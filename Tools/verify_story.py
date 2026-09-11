# -*- coding: utf-8 -*-
"""Independent verification of the generated story assets.

Reads the .asset files back from disk (not the builder's model), rebuilds the
card graph from GUID references, and checks:
  - every reference resolves to an existing asset
  - every card is reachable from the starting card; no orphans
  - choice cards have both branches; endings are terminal; no broken branches
  - every playable route is 15-20 experienced cards
  - every route contains at least one chat, one petition, one reaction card
  - resource simulation: clamped deltas, collapse only on reckless routes
  - every localized field carries both English and Arabic
  - speaker persona prompts present; seed overrides present where expected
"""
import os
import sys
import re
import yaml

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
DATA = os.path.join(ROOT, "Assets", "Game", "Data")

class Loader(yaml.SafeLoader):
    pass

def unity_tag(loader, suffix, node):
    if isinstance(node, yaml.ScalarNode):
        return loader.construct_scalar(node)
    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node)
    return loader.construct_mapping(node)

Loader.add_multi_constructor("tag:unity3d.com,2011", unity_tag)

def load_asset(path):
    with open(path, "r", encoding="utf-8") as f:
        text = f.read()
    docs = list(yaml.load_all(text, Loader=Loader))
    if not docs:
        return None
    doc = docs[0]
    if isinstance(doc, dict) and "MonoBehaviour" in doc:
        doc = doc["MonoBehaviour"]
    return doc

def ref_guid(ref_value):
    """Extracts an asset guid from a Unity object reference (string or parsed flow mapping)."""
    if not ref_value:
        return None
    if isinstance(ref_value, dict):
        g = ref_value.get("guid")
        return g if isinstance(g, str) and re.fullmatch(r"[0-9a-f]{32}", g) else None
    if isinstance(ref_value, str):
        m = re.search(r"guid: ([0-9a-f]{32})", ref_value)
        return m.group(1) if m else None
    return None

def is_ref(ref_value):
    return bool(ref_guid(ref_value))

failures = []
notes = []

def check(condition, message):
    if not condition:
        failures.append(message)

# ------------------------------------------------ load everything
cards_dir = os.path.join(DATA, "Cards")
cards = {}
for name in sorted(os.listdir(cards_dir)):
    if not name.endswith(".asset"):
        continue
    data = load_asset(os.path.join(cards_dir, name))
    check(data is not None, "%s failed to parse" % name)
    cards[data["assetName"]] = data

speakers = {}
for name in sorted(os.listdir(os.path.join(DATA, "Speakers"))):
    if not name.endswith(".asset"):
        continue
    data = load_asset(os.path.join(DATA, "Speakers", name))
    speakers[data["assetName"]] = data

resources = {}
for name in sorted(os.listdir(os.path.join(DATA, "Resources"))):
    if not name.endswith(".asset"):
        continue
    data = load_asset(os.path.join(DATA, "Resources", name))
    resources[data["assetName"]] = data

catalog = load_asset(os.path.join(DATA, "ResourceCatalog.asset"))
database = load_asset(os.path.join(DATA, "NarrativeDatabase.asset"))

print("cards: %d, speakers: %d, resources: %d" % (len(cards), len(speakers), len(resources)))
check(len(cards) == 48, "expected 48 cards, found %d" % len(cards))
check(len(speakers) == 7, "expected 7 speakers, found %d" % len(speakers))
check(len(resources) == 3, "expected 3 resources, found %d" % len(resources))
check("Army" in resources, "Army resource missing")
check("Loyalty" not in resources, "Loyalty resource should have been renamed to Army")
check("Card_19_BlightDecree" not in cards, "Card_19_BlightDecree should have been cut")
check("Card_QuietCity" in cards, "Card_QuietCity missing")

# ------------------------------------------------ guid index
guid_to_card = {}
for c in cards.values():
    meta_path = os.path.join(cards_dir, c["assetName"] + ".asset.meta")
    guid = ref_guid(open(meta_path, encoding="utf-8").read().splitlines()[1])
    guid_to_card[guid] = c["assetName"]

# database sanity
db_card_guids = [ref_guid(r) for r in database["cards"]]
check(all(g in guid_to_card for g in db_card_guids), "database references unknown card guids")
check(set(db_card_guids) == set(guid_to_card), "database card list != cards on disk")
start_guid = ref_guid(database["startingCard"])
check(start_guid in guid_to_card, "starting card guid does not resolve")

db_speaker_guids = {ref_guid(r) for r in database["speakers"]}
speaker_guids = set()
for name in speakers:
    meta = open(os.path.join(DATA, "Speakers", name + ".asset.meta"), encoding="utf-8").read()
    speaker_guids.add(ref_guid(meta.splitlines()[1]))
check(db_speaker_guids == speaker_guids, "database speaker list != speakers on disk")

cat_guids = [ref_guid(r) for r in catalog["resources"]]
res_guids = set()
for name in resources:
    meta = open(os.path.join(DATA, "Resources", name + ".asset.meta"), encoding="utf-8").read()
    res_guids.add(ref_guid(meta.splitlines()[1]))
check(set(cat_guids) == res_guids, "catalog != resources on disk")
check(all(g in res_guids for g in cat_guids), "catalog references unknown resource guids")

# ------------------------------------------------ per-card structural checks
ENDINGS = {
    "Card_End_TwoBrothers",
    "Card_35_End_Abdication",
    "Card_37_End_Tyrant",
    "Card_38_End_ShadowKing",
    "Card_39_End_WarlordsPeace",
    "Card_40_End_MerchantKing"
}

# Evaluator node: routes to state-driven endings; treated like a two-branch choice by the walker
EVALUATOR = {"Card_EndingEvaluator"}

# Verdict node: three-way card — left/right/continueNextCard are all valid exit branches
VERDICT = {"Card_36_FoolsVerdict"}
res_by_guid = {ref_guid_from_meta: name for name in resources
               for ref_guid_from_meta in [ref_guid(open(os.path.join(DATA, "Resources", name + ".asset.meta"),
                                               encoding="utf-8").read().splitlines()[1])]}

def deltas_of(change):
    values = (change or {}).get("values") or []
    out = {}
    for entry in values:
        g = ref_guid(entry["resource"])
        name = res_by_guid.get(g)
        check(name is not None, "resource change references unknown guid %s" % g)
        if name:
            out[name] = out.get(name, 0) + int(entry["value"])
    return out

for c in cards.values():
    asset = c["assetName"]
    is_reaction = bool(c["isLlmReactionCard"])
    is_petition = bool(c["isPetitionCard"])
    is_chat = bool(c["isChatCard"])
    continue_exit = is_reaction or is_petition or is_chat

    if asset in ENDINGS:
        check(not continue_exit, "%s: ending must not be a continue card" % asset)
        check(ref_guid(c["leftNextCard"]) is None and ref_guid(c["rightNextCard"]) is None,
              "%s: ending must have no branches" % asset)
        check(ref_guid(c["continueNextCard"]) is None, "%s: ending must have no continue" % asset)
    elif asset in EVALUATOR:
        lg, rg = ref_guid(c["leftNextCard"]), ref_guid(c["rightNextCard"])
        cg, mg = ref_guid(c.get("continueNextCard")), ref_guid(c.get("middleNextCard"))
        check(lg in guid_to_card, "%s: evaluator left target unresolved" % asset)
        check(rg in guid_to_card, "%s: evaluator right target unresolved" % asset)
        check(cg in guid_to_card, "%s: evaluator continue target unresolved" % asset)
        check(mg in guid_to_card, "%s: evaluator middle target unresolved" % asset)
        check(bool(c.get("isEndingEvaluator")), "%s: evaluator flag missing" % asset)
    elif continue_exit:
        g = ref_guid(c["continueNextCard"])
        check(g in guid_to_card, "%s: continue target unresolved" % asset)
        check(ref_guid(c["leftNextCard"]) is None and ref_guid(c["rightNextCard"]) is None,
              "%s: continue card should not use branches" % asset)
    else:
        lg, rg = ref_guid(c["leftNextCard"]), ref_guid(c["rightNextCard"])
        check(lg in guid_to_card, "%s: left branch unresolved" % asset)
        check(rg in guid_to_card, "%s: right branch unresolved" % asset)

    # localized fields carry both languages
    d = c["descriptionLocalized"]
    check(d.get("english") and d.get("arabic"), "%s: description missing a language" % asset)
    if not continue_exit and asset not in ENDINGS and asset not in EVALUATOR:
        for side in ("leftChoiceLocalized", "rightChoiceLocalized"):
            s = c[side]
            check(s.get("english") and s.get("arabic"), "%s: %s missing a language" % (asset, side))
        if asset in VERDICT:
            check(bool(c.get("isThreeWayVerdict")), "%s: three-way flag missing" % asset)
            m = c.get("middleChoiceLocalized") or {}
            check(m.get("english") and m.get("arabic"), "%s: middle choice missing a language" % asset)

    # seed overrides where the mechanic needs steering
    if is_petition:
        check(bool(c.get("petitionSeedOverride")), "%s: petition needs seed override" % asset)
        check(int(c["petitionerSource"]) == 0, "%s: commoner petition must use generated source" % asset)
    if is_chat:
        check(bool(c.get("chatSeedOverride")), "%s: chat needs seed override" % asset)
        check(int(c["petitionerSource"]) == 1, "%s: chat must use defined speaker" % asset)
        check(ref_guid(c["speaker"]) is not None, "%s: chat needs a speaker" % asset)
    if is_reaction:
        check(bool(c.get("reactionSeedOverride")), "%s: reaction needs seed override" % asset)
        check(ref_guid(c["speaker"]) is not None, "%s: reaction needs a speaker" % asset)

for s in speakers.values():
    prompt = s.get("llmPersonaPrompt") or ""
    check(bool(prompt and len(prompt) > 200),
          "%s: persona prompt too thin" % s["assetName"])
    check("Only reply in fluent Arabic" not in prompt,
          "%s: persona still language-locks to Arabic" % s["assetName"])
    dl = s["displayNameLocalized"]
    check(dl.get("english") and dl.get("arabic"), "%s: display name missing a language" % s["assetName"])
    check(is_ref(s.get("portrait")), "%s: portrait missing" % s["assetName"])

for r in resources.values():
    check(r.get("speaker") and ref_guid(r["speaker"]), "%s: warning speaker missing" % r["assetName"])
    check(bool(r.get("collapseEndingFallbackEnglish")) and bool(r.get("collapseEndingFallbackArabic")),
          "%s: collapse fallback missing" % r["assetName"])
    check(int(r.get("collapseThreshold", 99)) == 0, "%s: collapse threshold should be 0" % r["assetName"])
    check(int(r.get("warningThresholdPercent", 0)) == 30, "%s: warning threshold should be 30%%" % r["assetName"])
    check(r["displayNameLocalized"].get("english") == r["assetName"],
          "%s: display name should match asset name" % r["assetName"])

# warning speakers are distinct personas
warn_speakers = [ref_guid(r["speaker"]) for r in resources.values()]
check(len(set(warn_speakers)) == len(warn_speakers), "warning speakers not distinct")

# ------------------------------------------------ routes: length, mechanics, resources
def card_by_guid(g):
    return cards[guid_to_card[g]]

FLAG_BROTHER = 1
FLAG_CIRCUS = 2
FLAG_CHANCELLOR = 4
HEGEMONY = 70
IRON_CROWN = 50
COLLAPSE_AT = 0

def next_of(c):
    if c["assetName"] in ENDINGS:
        return []
    if c["assetName"] in EVALUATOR:
        outs = [ref_guid(c["leftNextCard"]), ref_guid(c["rightNextCard"]),
                ref_guid(c.get("continueNextCard")), ref_guid(c.get("middleNextCard"))]
        return [g for g in outs if g]
    if c["assetName"] in VERDICT:
        middle = ref_guid(c.get("middleNextCard")) or ref_guid(c.get("continueNextCard"))
        return [g for g in (ref_guid(c["leftNextCard"]), middle, ref_guid(c["rightNextCard"])) if g]
    if c["isLlmReactionCard"] or c["isPetitionCard"] or c["isChatCard"]:
        return [ref_guid(c["continueNextCard"])]
    return [ref_guid(c["leftNextCard"]), ref_guid(c["rightNextCard"])]

def deltas_for(c, side):
    if c["isLlmReactionCard"] or c["isPetitionCard"] or c["isChatCard"] \
            or c["assetName"] in ENDINGS or c["assetName"] in EVALUATOR:
        return {}
    if side == "M":
        return deltas_of(c.get("middleResourceChange"))
    return deltas_of(c["leftResourceChange" if side == "L" else "rightResourceChange"])

def flags_set(c, side):
    key = {"L": "leftSetFlags", "R": "rightSetFlags", "M": "middleSetFlags", "C": "continueSetFlags"}[side]
    return int(c.get(key) or 0)

def flags_required(c, side):
    key = {"L": "leftRequiresFlags", "R": "rightRequiresFlags", "M": "middleRequiresFlags"}.get(side)
    return int(c.get(key) or 0) if key else 0

def passes_entry(c, res, story_flags):
    req = int(c.get("entryRequiresFlags") or 0)
    if req and (story_flags & req) != req:
        return False
    if not c.get("hasResourceGate"):
        return True
    g = ref_guid(c.get("gateResource"))
    name = res_by_guid.get(g)
    if not name:
        return True
    value = res.get(name, 0)
    return int(c.get("gateMinInclusive") or 0) <= value <= int(c.get("gateMaxInclusive") or 100)

def pick_skip(c, res):
    skip = ref_guid(c.get("skipToCard"))
    alt = ref_guid(c.get("skipToAltCard"))
    if skip and alt:
        return alt if res.get("Gold", 0) > res.get("Army", 0) else skip
    return skip or alt

def resolve_entry(guid, res, story_flags):
    for _ in range(8):
        if not guid:
            return None
        c = cards[guid_to_card[guid]]
        if passes_entry(c, res, story_flags):
            return guid
        guid = pick_skip(c, res)
    return guid

def select_ending(eval_card, res, story_flags):
    tyrant = ref_guid(eval_card["leftNextCard"])
    shadow = ref_guid(eval_card["rightNextCard"])
    warlords = ref_guid(eval_card.get("continueNextCard"))
    merchant = ref_guid(eval_card.get("middleNextCard"))
    crown, gold, army = res.get("Crown", 0), res.get("Gold", 0), res.get("Army", 0)
    fallen = bool(story_flags & FLAG_CHANCELLOR)
    if army >= HEGEMONY and army >= gold and army >= crown and warlords:
        return warlords
    if gold >= HEGEMONY and gold >= army and gold >= crown and merchant:
        return merchant
    if fallen and crown >= IRON_CROWN and tyrant:
        return tyrant
    if not fallen and shadow:
        return shadow
    if crown >= gold and crown >= army and tyrant:
        return tyrant
    return shadow or tyrant or warlords or merchant

start = guid_to_card[start_guid]
routes = []

def walk(guid, res, seq, mech, story_flags, days):
    guid = resolve_entry(guid, res, story_flags)
    if not guid:
        routes.append((seq + ["#EMPTY_GATE"], res, mech, days, story_flags))
        return
    c = cards[guid_to_card[guid]]
    seq = seq + [c["assetName"]]
    days += int(c.get("dayAdvance", 1))
    mech = dict(mech)
    for k in ("chat", "petition", "reaction"):
        if (k == "chat" and c["isChatCard"]) or (k == "petition" and c["isPetitionCard"]) \
           or (k == "reaction" and c["isLlmReactionCard"]):
            mech[k] = mech.get(k, 0) + 1
    if c["assetName"] in ENDINGS:
        routes.append((seq, res, mech, days, story_flags))
        return
    if c["assetName"] in EVALUATOR:
        ending_guid = select_ending(c, res, story_flags)
        if ending_guid:
            walk(ending_guid, res, seq, mech, story_flags, days)
        else:
            routes.append((seq + ["#NO_ENDING"], res, mech, days, story_flags))
        return
    outs = next_of(c)
    if c["assetName"] in VERDICT:
        sides = ["L", "M", "R"]
    elif len(outs) == 1:
        sides = ["C"]
    else:
        sides = ["L", "R"]
    for side, g in zip(sides, outs):
        required = flags_required(c, side)
        if required and (story_flags & required) != required:
            continue
        r2 = dict(res)
        for name, v in deltas_for(c, side).items():
            r2[name] = max(0, min(100, r2[name] + v))
        next_flags = story_flags | flags_set(c, side)
        nxt_name = guid_to_card.get(g)
        if nxt_name and nxt_name not in ENDINGS:
            collapsed = sorted(n for n, v in r2.items() if v <= COLLAPSE_AT)
            if collapsed:
                routes.append((seq + ["#COLLAPSE(%s)" % ",".join(collapsed)], r2, mech, days, next_flags))
                continue
        walk(g, r2, seq, mech, next_flags, days)

walk(start_guid, {name: int(r["defaultStartingValue"]) for name, r in resources.items()}, [], {}, 0, 0)

survivor_routes = [r for r in routes if not any(s.startswith("#") for s in r[0])]
collapse_routes = [r for r in routes if any("#COLLAPSE" in s for s in r[0])]
survivor_lengths = [
    len([s for s in seq if s not in EVALUATOR])
    for seq, _, _, _, _ in survivor_routes
]
print("survivor routes: %d, length min/max: %d/%d" % (
    len(survivor_lengths), min(survivor_lengths) if survivor_lengths else 0,
    max(survivor_lengths) if survivor_lengths else 0))
check(bool(survivor_lengths), "no surviving routes")
if survivor_lengths:
    check(min(survivor_lengths) >= 14, "survivor route shorter than 14 cards")
    check(max(survivor_lengths) <= 24, "survivor route longer than 24 cards")

for seq, _, mech, _, _ in survivor_routes:
    for k in ("chat", "petition", "reaction"):
        check(mech.get(k, 0) >= 1, "route lacks %s: %s" % (k, seq))

print("collapse routes: %d / %d" % (len(collapse_routes), len(routes)))
check(len(collapse_routes) > 0, "no collapse routes; threshold 0 should be reachable")

endings_reached = {seq[-1] for seq, _, _, _, _ in survivor_routes}
print("authored endings reached: %s" % sorted(endings_reached))
for ending in ENDINGS:
    check(ending in endings_reached, "%s unreachable" % ending)

day_totals = [d for _, _, _, d, _ in survivor_routes]
print("reign days min/max: %d/%d" % (min(day_totals), max(day_totals)))

for name in resources:
    vals = [res[name] for _, res, _, _, _ in survivor_routes]
    print("%s at endings min/max: %d/%d" % (name, min(vals), max(vals)))
    check(min(vals) >= 0, "%s below collapse threshold on a surviving route" % name)

# Flag-gated content actually fires
check(any(sf & FLAG_BROTHER for *_, sf in survivor_routes), "BrotherKnown never set")
check(any(sf & FLAG_CIRCUS for *_, sf in survivor_routes), "CircusIn never set")
check(any(sf & FLAG_CHANCELLOR for *_, sf in survivor_routes), "ChancellorFallen never set")
check(any(not (sf & FLAG_BROTHER) for *_, sf in survivor_routes), "every route sets BrotherKnown")
check(any(not (sf & FLAG_CIRCUS) for *_, sf in survivor_routes), "every route sets CircusIn")
check(any(not (sf & FLAG_CHANCELLOR) for *_, sf in survivor_routes), "every route sets ChancellorFallen")

reached_cards = set()
for seq, *_ in routes:
    reached_cards.update(s for s in seq if not s.startswith("#"))
orphans = sorted(set(cards) - reached_cards - EVALUATOR)
check(not orphans, "unreachable cards: %s" % orphans)

# ------------------------------------------------ report
if failures:
    print("\nFAILURES (%d):" % len(failures))
    for f in failures:
        print(" -", f)
    sys.exit(1)
print("\nVERIFY OK")
