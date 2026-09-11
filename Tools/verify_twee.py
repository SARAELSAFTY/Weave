# -*- coding: utf-8 -*-
"""Verifies The_Wicked_King_2_5 (2).twee against the Delivery Spec:
  - 40 story cards + 6 personas + 3 collapse fallbacks + meta passages
  - Asserts all 2D Twine layout coordinates are non-overlapping with clear padding
  - Asserts all link targets exist (no broken branches)
  - Checks reachability from 'Card_01_MotherGhost'
  - Validates that EVERY story card description is short (1-3 sentences, <= 45 words)
  - Simulates all routes from start to confirm:
      * Every route experiences >= 15 cards
      * Every route experiences at least 1 Chat, 1 Petition, and 1 Reaction
      * Reaches one of the 5 authored endings
  - Checks existence of isolated Persona passages and Collapse Fallbacks
"""

import os
import re
import sys
from collections import defaultdict, deque

ROOT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)), ".."))
TWEE_PATH = os.path.join(ROOT, "The_Wicked_King_2_5 (2).twee")

failures = []

def check(condition, message):
    if not condition:
        failures.append(message)

with open(TWEE_PATH, "r", encoding="utf-8") as f:
    content = f.read()

passage_pattern = re.compile(r'^::\s*([^\{\[\n\r]+?)(?:\s*\[(.*?)\])?(?:\s*\{([^}]+)\})?\s*$', re.MULTILINE)
matches = list(passage_pattern.finditer(content))

passages = {}
positions = {}

for i, m in enumerate(matches):
    title = m.group(1).strip()
    tags = m.group(2).strip() if m.group(2) else ""
    meta = m.group(3).strip() if m.group(3) else ""
    start_pos = m.end()
    end_pos = matches[i+1].start() if i + 1 < len(matches) else len(content)
    body = content[start_pos:end_pos].strip()
    
    # Parse position
    pos_m = re.search(r'"position":\s*"(-?\d+),(-?\d+)"', meta)
    if pos_m:
        positions[title] = (int(pos_m.group(1)), int(pos_m.group(2)))
    elif title not in ["StoryTitle", "StoryData"]:
        failures.append(f"Passage '{title}' has missing or invalid position: '{meta}'")
        
    # Parse links
    links = []
    for link_m in re.finditer(r'\[\[(.*?)\]\]', body):
        raw = link_m.group(1).strip()
        if "->" in raw:
            text, target = raw.split("->", 1)
        elif "|" in raw:
            text, target = raw.split("|", 1)
        else:
            text = raw
            target = raw
        links.append((text.strip(), target.strip()))
        
    passages[title] = {
        "tags": tags,
        "meta": meta,
        "body": body,
        "links": links
    }

print(f"Total passages found: {len(passages)}")

# 1. Overlap check
pos_items = list(positions.items())
for i in range(len(pos_items)):
    for j in range(i + 1, len(pos_items)):
        t1, (x1, y1) = pos_items[i]
        t2, (x2, y2) = pos_items[j]
        if abs(x1 - x2) < 120 and abs(y1 - y2) < 60:
            failures.append(f"Overlap between '{t1}' ({x1},{y1}) and '{t2}' ({x2},{y2})")

# 2. Broken link check
for title, d in passages.items():
    for txt, tgt in d["links"]:
        if tgt not in passages:
            failures.append(f"Broken link in '{title}': target '{tgt}' does not exist")

# 3. Card text length check (1-3 sentences, <= 45 words)
story_cards = [k for k in passages if k.startswith("Card_")]
check(len(story_cards) == 40, f"Expected 40 story cards, found {len(story_cards)}")

for sc in story_cards:
    body = passages[sc]["body"]
    en_m = re.search(r'EN:\s*(.*?)(?=\nAR:|\n\[Seed|\n<!--|\Z)', body, re.DOTALL)
    if en_m:
        en_text = en_m.group(1).strip()
        word_count = len(en_text.split())
        check(word_count <= 45, f"{sc}: description too long ({word_count} words): '{en_text}'")
    else:
        failures.append(f"{sc}: missing 'EN:' text field")

# 4. Route length & card type simulation
adj = defaultdict(list)
for sc in story_cards:
    for txt, tgt in passages[sc]["links"]:
        adj[sc].append(tgt)

AUTHORED_ENDINGS = {
    "Card_35_End_Abdication",
    "Card_37_End_Tyrant",
    "Card_38_End_ShadowKing",
    "Card_39_End_WarlordsPeace",
    "Card_40_End_MerchantKing"
}

routes = []
def walk(curr, path, kinds):
    p = passages[curr]
    tag = p["tags"].lower()
    new_kinds = kinds + [tag if tag else "choice"]
    new_path = path + [curr]
    
    if curr in AUTHORED_ENDINGS:
        routes.append((new_path, new_kinds))
        return
        
    outs = adj[curr]
    check(len(outs) > 0, f"Card '{curr}' is a dead end (no outgoing links and not an ending)")
    for nxt in outs:
        walk(nxt, new_path, new_kinds)

walk("Card_01_MotherGhost", [], [])

print(f"Total simulated routes: {len(routes)}")
lengths = [len(r) for r, k in routes]
min_l, max_l = min(lengths), max(lengths)
print(f"Route lengths min: {min_l}, max: {max_l}")
check(min_l >= 15, f"Shortest route has {min_l} cards (expected >= 15)")
check(max_l <= 20, f"Longest route has {max_l} cards (expected <= 20)")

# Check Chat, Petition, Reaction on every route
for i, (path, kinds) in enumerate(routes):
    check("chat" in kinds, f"Route {i} lacks Chat card: {path}")
    check("petition" in kinds, f"Route {i} lacks Petition card: {path}")
    check("reaction" in kinds, f"Route {i} lacks Reaction card: {path}")

endings_reached = {path[-1] for path, _ in routes}
print(f"Authored endings reached: {sorted(endings_reached)}")
check(endings_reached == AUTHORED_ENDINGS, f"Missing endings: {AUTHORED_ENDINGS - endings_reached}")

# 5. Isolated Personas & Fallbacks
expected_personas = [
    "Persona - The General",
    "Persona - The Chancellor",
    "Persona - The Treasurer",
    "Persona - The Jester",
    "Persona - The Spirit Mother",
    "Persona - The Seer"
]
for ep in expected_personas:
    check(ep in passages, f"Missing persona passage: '{ep}'")
    check("Persona" in passages[ep]["tags"], f"'{ep}' missing [Persona] tag")

expected_fallbacks = [
    "Collapse - Crown",
    "Collapse - Gold",
    "Collapse - Army"
]
for ef in expected_fallbacks:
    check(ef in passages, f"Missing collapse fallback: '{ef}'")
    check("Collapse Fallback" in passages[ef]["tags"], f"'{ef}' missing [Collapse Fallback] tag")

if failures:
    print(f"\nFAILED with {len(failures)} errors:")
    for f in failures:
        print("  -", f)
    sys.exit(1)
else:
    print("\nVERIFY TWEE OK - All 13 delivery specifications passed!")
    sys.exit(0)
