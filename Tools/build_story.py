#!/usr/bin/env python3
"""
build_story.py — Compiles The Wicked King narrative graph.
Generates:
  - Assets/Game/Data/Cards/*.asset (+ .meta)
  - Assets/Game/Data/Speakers/*.asset (+ .meta)
  - Assets/Game/Data/Resources/*.asset (+ .meta)
  - Assets/Game/Data/ResourceCatalog.asset
  - Assets/Game/Data/NarrativeDatabase.asset
  - The_Wicked_King_2_5 (2).twee
"""

import os, sys, re, yaml, hashlib

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARDS_DIR = os.path.join(ROOT, "Assets", "Game", "Data", "Cards")
SPEAKERS_DIR = os.path.join(ROOT, "Assets", "Game", "Data", "Speakers")
RESOURCES_DIR = os.path.join(ROOT, "Assets", "Game", "Data", "Resources")
CATALOG_PATH = os.path.join(ROOT, "Assets", "Game", "Data", "ResourceCatalog.asset")
DATABASE_PATH = os.path.join(ROOT, "Assets", "Game", "Data", "NarrativeDatabase.asset")
TWEE_PATH = os.path.join(ROOT, "The_Wicked_King_2_5 (2).twee")

# Stable Canonical GUIDs
DATABASE_GUID = "6e18ec970705145408f300ec375fe9be"
PROMPT_TEMPLATES_GUID = "4ff166bac16d6c74cb86389f74611cd4"

SPEAKERS = {
    "Spk_Chancellor": {
        "guid": "220c64b1d598ddf41bc3f43bc6849160",
        "en": "The Chancellor", "ar": "المستشار",
        "portrait": "{fileID: 21300000, guid: 09f5413882d741ed938f1ac53dcab077, type: 3}",
        "prompt": "You are Chancellor Aldric, the silk-voiced chief diplomat and minister. You make ruthless realpolitik sound like noble statesmanship. You cite precedents, numbers, and state stability. Never raise your voice; speak with calm, polished, dangerous elegance. Keep replies under 30 words."
    },
    "Spk_General": {
        "guid": "968d4d0248269710a22f4c7dabc6a9cd",
        "en": "The General", "ar": "الجنرال",
        "portrait": "{fileID: 21300000, guid: 46bd00415f48411fadf2b6e8af1c8dfa, type: 3}",
        "prompt": "You are the General, a veteran 30-year army commander. You frame every problem in terms of soldiers, steel, supplies, and tactical terrain. You distrust courtiers and diplomats. Speak with stern military brevity and unyielding loyalty. Calls king Sire. Keep replies under 30 words."
    },
    "Spk_Treasurer": {
        "guid": "bd422fef7c7a0976bcb3c39364963115",
        "en": "The Treasurer", "ar": "أمين الخزينة",
        "portrait": "{fileID: 21300000, guid: 09f5413882d741ed938f1ac53dcab077, type: 3}",
        "prompt": "You are the Treasurer, master of the royal vaults. You view every war, treaty, and crisis through gold and taxation. You quietly skim an administrative fee whenever possible. Speak with oily politeness and numerical precision. Calls king Sire. Keep replies under 30 words."
    },
    "Spk_Jester": {
        "guid": "58e0d26dc32b25229dc4aef014036958",
        "en": "The Jester", "ar": "بهلوان البلاط",
        "portrait": "{fileID: 21300000, guid: 9991a13208bb4eda8e373c0a3e8b5786, type: 3}",
        "prompt": "You are the Jester, King Percy's elder bastard half-brother. You pose as a fool in bells and ribbons to protect him and speak unvarnished truth to power. You drop profound wisdom inside riddles and jokes. Calls the king Percy. Keep replies under 30 words."
    },
    "Spk_SpiritMother": {
        "guid": "955c23e0b4b52b58398e46bb89677072",
        "en": "The Spirit Mother", "ar": "روح الملكة الأم",
        "portrait": "{fileID: 21300000, guid: 418c88c1451d4d39bf920be20f82533f, type: 3}",
        "prompt": "You are the ghost of the late queen mother. You haunt King Percy with sharp, unsparing maternal critique, balancing severe standards with deep protective instinct. Remind him of blood, duty, and truth. Calls him Percy. Keep replies under 30 words."
    },
    "Spk_Seer": {
        "guid": "b03696b9fe28e4290e0808855683bfe3",
        "en": "The Seer", "ar": "العرّافة",
        "portrait": "{fileID: 21300000, guid: d737a477c6e94ab28bec29563bc1adcd, type: 3}",
        "prompt": "You are the blind Seer of the traveling circus. You speak in chilling, rhythmic omens drawn from domestic metaphors like salt, thread, and smoke. You demand coin or sacrifice for insight. Addresses ruler as little king. Keep replies under 30 words."
    },
    "Spk_Merchant": {
        "guid": "c5e9af25f58fc364fa7f8feda2237cb4",
        "en": "The Merchant", "ar": "كبير التجار",
        "portrait": "{fileID: 21300000, guid: 4a7945b246c54ab5be0cf41a50bdb5ca, type: 3}",
        "prompt": "You are Master Silvio, head of the merchant guild. You manage grain shipments, harbor trade, and market supply. Pragmatic, persuasive, and concerned with keeping bread affordable and trade routes open. Keep replies under 30 words."
    }
}

RESOURCES = {
    "Crown": {
        "guid": "b891174f857049f44be00b474746951e",
        "en": "Crown", "ar": "التاج",
        "icon": "{fileID: -1512851089295447720, guid: df8ab9ec8126d8148a4242e7d8b17249, type: 3}",
        "speaker": "{fileID: 11400000, guid: 220c64b1d598ddf41bc3f43bc6849160, type: 2}", # Spk_Chancellor
        "collapse_en": "The gates give way at dusk. The mob carries your crown through the streets. A dynasty is a story the realm agrees to tell, and yours has ended mid-sentence.",
        "collapse_ar": "سقطت بوابات القصر عند الغسق، وحملت الجموع تاجك عبر الشوارع. الملك هيبة يرتضيها الناس، وهيبتك تبددت للأبد."
    },
    "Gold": {
        "guid": "1bd4e23f03990bc4d9ab64de0475c474",
        "en": "Gold", "ar": "الذهب",
        "icon": "{fileID: -4290352727915614949, guid: 3dd2af48656141c43a45dd30c37c4daf, type: 3}",
        "speaker": "{fileID: 11400000, guid: c5e9af25f58fc364fa7f8feda2237cb4, type: 2}", # Spk_Merchant
        "collapse_en": "The vaults stand open and echoing, swept clean. A king without coin is a beggar with better posture.",
        "collapse_ar": "تردد صدى الصمت في خزائنك الخاوية بعد نفاد آخر دينار. الملك بلا مال كالمتسول في ثياب فاخرة."
    },
    "Army": {
        "guid": "10d39ca6ec17c4442b381bbfe0578b71",
        "en": "Army", "ar": "الجيش",
        "icon": "{fileID: -509447706214915325, guid: 007a64651e7261241b30c259e7fd2957, type: 3}",
        "speaker": "{fileID: 11400000, guid: 968d4d0248269710a22f4c7dabc6a9cd, type: 2}", # Spk_General
        "collapse_en": "The garrison stacks pikes in the yard and goes home. The General salutes the empty throne out of habit before leaving.",
        "collapse_ar": "ألقى الجنود أسلحتهم وانصرفوا إلى بيوتهم؛ فحاكم بلا ولاء حقيقي لا يملك سوى خارطة بلا جنود."
    }
}

# StoryFlags enum in CardData: BrotherKnown=1, CircusIn=2, ChancellorFallen=4
FLAG_BROTHER = 1
FLAG_CIRCUS = 2
FLAG_CHANCELLOR = 4

# The 48 Cards of the Weave Narrative Graph
CARDS = [
    # ─── PROLOGUE ─────────────────────────────────────────────────────────────
    {
        "asset": "Card_01_MotherGhost", "speaker": "Spk_SpiritMother",
        "desc": {
            "en": "Cold mist gathers around your canopy bed. Your mother's ghost leans close: 'Percy, waking up with a crown does not make you a ruler. The Eastern border burns. Will you rule with iron or ink?'",
            "ar": "يتصاعد ضباب بارد حول سريرك الملكي، ويدنو طيف والدتك هامساً: «يا بيرسي، ليس كل من استيقظ وعلى رأسه تاج صار ملكاً حقاً! حدود الشرق تشتعل، فهل ستحكم بالحديد أم بالحبر؟»"
        },
        "left": {"en": "Gather the war council; march on the East.", "ar": "اجمع مجلس الحرب؛ سنزحف نحو الشرق فوراً!"},
        "left_res": {"Crown": 5, "Gold": -10, "Army": 10}, "left_next": "Card_02_EasternRaids",
        "right": {"en": "Summon diplomats; offer alliance and trade.", "ar": "استدعِ السفراء؛ اعرض التحالف والمصالح المشتركة."},
        "right_res": {"Crown": -5, "Gold": 10, "Army": -5}, "right_next": "Card_09_Alliance_Relief"
    },

    # ─── ACT I: EASTERN CONQUEST ROUTE ────────────────────────────────────────
    {
        "asset": "Card_02_EasternRaids", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'Sire, Easternia's border raiders burned three of our grain mills. We must strike their garrisons before dawn!'",
            "ar": "الجنرال: «يا مولاي، أحرقت غارات مملكة الشرق ثلاثة من طواحين قمحنا! لا بد من مباغتة ثكناتهم قبل بزوغ الفجر!»"
        },
        "left": {"en": "Mobilize the vanguard; burn their outposts.", "ar": "حرّك طليعة الجيش، وأشعل النار في حصونهم."},
        "left_res": {"Crown": 5, "Gold": -10, "Army": 10}, "left_next": "Card_03_Conquest_March",
        "right": {"en": "Hold defensive lines; do not escalate.", "ar": "الزموا الخطوط الدفاعية؛ لا أريد تصعيداً غير محسوب."},
        "right_res": {"Crown": -5, "Gold": 5, "Army": -5}, "right_next": "Card_08_EasternResistance"
    },
    {
        "asset": "Card_03_Conquest_March", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'Our cavalry broke their outer perimeter! Their frontier village flies our banner, but the prisoners stare with bitter hatred.'",
            "ar": "الجنرال: «حطمت خيالتنا خطوطهم الأمامية! قريتهم الحدودية ترفع رايتنا الآن، لكن الأسرى يرمقوننا بنظرات الحقد الدفين.»"
        },
        "left": {"en": "Enforce strict martial law; seize weapons.", "ar": "فرض الأحكام العرفية وتجريدهم من السلاح."},
        "left_res": {"Crown": 5, "Gold": 5, "Army": 5}, "left_next": "Card_04_Conquest_IronGrip",
        "right": {"en": "Distribute rations; win their hearts.", "ar": "وزّعوا المؤن على الأهالي لكسب ولائهم."},
        "right_res": {"Crown": -5, "Gold": -10, "Army": 10}, "right_next": "Card_06_Conquest_Mercy"
    },
    {
        "asset": "Card_04_Conquest_IronGrip", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Treasurer: 'Sire, pacifying this conquered territory is bleeding our coffers dry. We need a war levy on their local merchants immediately.'",
            "ar": "أمين الخزينة: «يا مولاي، إحكام السيطرة على هذه الأراضي يستنزف خزائننا! يجب فرض ضريبة حرب على تجارهم فوراً.»"
        },
        "left": {"en": "Tax them mercilessly; war must pay for war.", "ar": "افرض الضرائب بلا رحمة؛ فالحرب تطعم نفسها."},
        "left_res": {"Crown": 5, "Gold": 15, "Army": -10}, "left_next": "Card_05_Conquest_Taxation",
        "right": {"en": "Spare their commerce; establish fair border tolls.", "ar": "اعفُ تجارتهم؛ واكتفِ برسوم جمركية عادلة."},
        "right_res": {"Crown": -5, "Gold": -5, "Army": 10}, "right_next": "Card_07_Conquest_GarrisonCost"
    },
    {
        "asset": "Card_05_Conquest_Taxation", "speaker": "Spk_Chancellor",
        "desc": {
            "en": "Chancellor: 'The high levies brought heavy coin, Sire, but rebel partisans are organizing in the mountains. A rebellion brews.'",
            "ar": "المستشار: «جلبت الضرائب الباهظة ذهباً وفيراً، لكن المقاومين يحشدون في الجبال. نيران التمرد توشك أن تندلع!»"
        },
        "left": {"en": "Send inquisitors; purge the mountain dens.", "ar": "أرسل العيون والفرسان لتطهير مخابئ الجبال."},
        "left_res": {"Crown": 5, "Gold": -10, "Army": 5}, "left_next": "Card_15_CircusArrives",
        "right": {"en": "Offer amnesty to any rebel who lays down arms.", "ar": "اعرض العفو العام على كل من يلقي السلاح."},
        "right_res": {"Crown": -5, "Gold": -5, "Army": 10}, "right_next": "Card_15_CircusArrives"
    },
    {
        "asset": "Card_06_Conquest_Mercy", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'Your mercy impressed the town elders, Sire. They surrendered their weapon caches, but our soldiers grumble about missed plunder.'",
            "ar": "الجنرال: «أثرت رحمتك في كبار قريتهم، فسلّموا مخازن سلاحهم طوعاً؛ غير أن جندنا يتذمرون لحرمانهم من الغنائم.»"
        },
        "left": {"en": "Pay the troops a modest royal bonus.", "ar": "اصرف مكافأة ترضية للجند من الخزينة الخاصة."},
        "left_res": {"Crown": 5, "Gold": -15, "Army": 15}, "left_next": "Card_07_Conquest_GarrisonCost",
        "right": {"en": "Remind them of military discipline; no bonuses.", "ar": "ذكّرهم بالانضباط العسكري؛ لا غنائم على حساب الشرف."},
        "right_res": {"Crown": 10, "Gold": 0, "Army": -10}, "right_next": "Card_08_EasternResistance"
    },
    {
        "asset": "Card_07_Conquest_GarrisonCost", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Treasurer: 'Maintaining three forward fortresses on the eastern ridge costs fifty wagonloads of silver each month. Shall we scale back?'",
            "ar": "أمين الخزينة: «صيانة ثلاثة حصون أمامية في الشرق تكلفنا خمسين حمولة فضة شهرياً! فهل نخفض أعداد الحاميات؟»"
        },
        "left": {"en": "Reinforce the ridge; security comes first.", "ar": "عزّز الحصون؛ أمن المملكة لا يقبل المساومة."},
        "left_res": {"Crown": 5, "Gold": -15, "Army": 10}, "left_next": "Card_15_CircusArrives",
        "right": {"en": "Withdraw back to the natural river border.", "ar": "انسحب إلى خط النهر الطبيعي لترشيد النفقات."},
        "right_res": {"Crown": -5, "Gold": 10, "Army": -5}, "right_next": "Card_15_CircusArrives"
    },
    {
        "asset": "Card_08_EasternResistance", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'Eastern partisans launched hit-and-run ambushes along our supply routes. We must crush their mountain hideouts or withdraw.'",
            "ar": "الجنرال: «شنّ مقاتلو الشرق كمائن مباغتة على قوافل إمدادنا! إما أن نمشّط الجبال بحملة كبرى، أو ننسحب.»"
        },
        "left": {"en": "Deploy shock cavalry and seal off the passes.", "ar": "انشر الفرسان وأغلق الممرات الجبلية بإحكام."},
        "left_res": {"Crown": 10, "Gold": -10, "Army": 5}, "left_next": "Card_15_CircusArrives",
        "right": {"en": "Fortify our supply depots and avoid open battle.", "ar": "حصّن مستودعات المؤن وتجنب المعارك المفتوحة."},
        "right_res": {"Crown": -5, "Gold": 5, "Army": -5}, "right_next": "Card_15_CircusArrives"
    },

    # ─── ACT I: EASTERN ALLIANCE ROUTE ────────────────────────────────────────
    {
        "asset": "Card_09_Alliance_Relief", "speaker": "Spk_Chancellor",
        "desc": {
            "en": "Chancellor: 'The Eastern Kingdom welcomes an envoy, Sire! But their frontier provinces suffer from drought. Sending grain would seal their goodwill.'",
            "ar": "المستشار: «رحبت مملكة الشرق بسفيرنا يا مولاي! غير أن أقاليمهم الحدودية تعاني الجفاف. إرسال معونات قمح سيضمن صداقتهم.»"
        },
        "left": {"en": "Dispatch fifty wagons of royal grain.", "ar": "أرسل خمسين عربة قمح من مستودعاتنا فوراً."},
        "left_res": {"Crown": 5, "Gold": -10, "Army": 10}, "left_next": "Card_10_Alliance_Pact",
        "right": {"en": "Send words of sympathy and modest gifts.", "ar": "أرسل رسائل مواساة وهدايا رمزية فقط."},
        "right_res": {"Crown": -5, "Gold": 5, "Army": -5}, "right_next": "Card_12_Alliance_Gifts"
    },
    {
        "asset": "Card_10_Alliance_Pact", "speaker": "Spk_Chancellor",
        "desc": {
            "en": "Chancellor: 'The Eastern King has signed the non-aggression pact! He proposes joint border customs to deter banditry.'",
            "ar": "المستشار: «وقّع ملك الشرق ميثاق عدم الاعتداء! ويقترح إنشاء نقاط جمركية مشتركة لردع قُطّاع الطرق.»"
        },
        "left": {"en": "Ratify the treaty and open the trade road.", "ar": "صادق على المعاهدة وافتح طريق التجارة."},
        "left_res": {"Crown": 5, "Gold": 10, "Army": -5}, "left_next": "Card_11_Alliance_Tolls",
        "right": {"en": "Demand extra security concessions first.", "ar": "اشترط ضمانات أمنية إضافية لصالحنا أولاً."},
        "right_res": {"Crown": 5, "Gold": -5, "Army": 5}, "right_next": "Card_13_Alliance_Sovereignty"
    },
    {
        "asset": "Card_11_Alliance_Tolls", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Treasurer: 'Merchandise flows smoothly across the border, Sire! We could double the transit tariffs on their silk and spice wagons.'",
            "ar": "أمين الخزينة: «البضائع تتدفق عبر الحدود بسلاسة يا مولاي! يسعنا مضاعفة الرسوم على قوافل الحرير والتوابل.»"
        },
        "left": {"en": "Increase the tariffs; fill the royal treasury.", "ar": "ارفع الرسوم الجمركية؛ لنملأ خزائن التاج بالذهب."},
        "left_res": {"Crown": -5, "Gold": 15, "Army": -5}, "left_next": "Card_14_Alliance_BorderPost",
        "right": {"en": "Keep tariffs low to encourage trade volume.", "ar": "أبقِ الرسوم منخفضة لتشجيع رواج التجارة."},
        "right_res": {"Crown": 5, "Gold": 5, "Army": 10}, "right_next": "Card_14_Alliance_BorderPost"
    },
    {
        "asset": "Card_12_Alliance_Gifts", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'The Eastern court felt slighted by our modest gifts. They have doubled their border patrols. We look weak!'",
            "ar": "الجنرال: «استاء بلاط الشرق من هدايانا الهزيلة، وضاعفوا دورياتهم! يبدو تراجعنا ضعفاً في أعينهم!»"
        },
        "left": {"en": "Stage a grand military parade at the frontier.", "ar": "أقم استعراضاً عسكرياً مهيباً على خط الحدود."},
        "left_res": {"Crown": 10, "Gold": -5, "Army": 5}, "left_next": "Card_13_Alliance_Sovereignty",
        "right": {"en": "Send a polished apology with fine royal wines.", "ar": "أرسل اعتذاراً دبلوماسياً رقيقاً مع خمور فاخرة."},
        "right_res": {"Crown": -5, "Gold": -5, "Army": 5}, "right_next": "Card_14_Alliance_BorderPost"
    },
    {
        "asset": "Card_13_Alliance_Sovereignty", "speaker": "Spk_Chancellor",
        "desc": {
            "en": "Chancellor: 'The Eastern Kingdom formally recognizes our sovereignty over the border valley. The frontier is at peace, Sire.'",
            "ar": "المستشار: «اعترفت مملكة الشرق رسمياً بسيادتنا على وادي الحدود. حلّ السلام على الثغور يا مولاي.»"
        },
        "left": {"en": "Celebrate the diplomatic victory with a feast.", "ar": "أقم مأدبة ملكية كبرى احتفاءً بنصر الدبلوماسية."},
        "left_res": {"Crown": 10, "Gold": -10, "Army": 10}, "left_next": "Card_15_CircusArrives",
        "right": {"en": "Reallocate border garrisons to the capital.", "ar": "أعد توزيع حاميات الحدود لحماية العاصمة."},
        "right_res": {"Crown": -5, "Gold": 5, "Army": -5}, "right_next": "Card_15_CircusArrives"
    },
    {
        "asset": "Card_14_Alliance_BorderPost", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Treasurer: 'Our joint border post generates a steady thirty gold per week. The eastern merchants are satisfied and singing your praise.'",
            "ar": "أمين الخزينة: «نقطة العبور المشتركة تدر ثلاثين ديناراً أسبوعياً! تجار الشرق راضون ويثنون على عدالتكم.»"
        },
        "left": {"en": "Invest the revenue in royal road repairs.", "ar": "استثمر العائدات في تمهيد الطرق وحماية القوافل."},
        "left_res": {"Crown": 5, "Gold": -5, "Army": 15}, "left_next": "Card_15_CircusArrives",
        "right": {"en": "Lock the profits away in the high vault.", "ar": "أودع الأرباح في خزائن القصر كاحتياطي استراتيجي."},
        "right_res": {"Crown": 0, "Gold": 15, "Army": -5}, "right_next": "Card_15_CircusArrives"
    },

    # ─── ACT II: DOMESTIC COURT, REBELLION, TREASON & BLOODLINES ──────────────
    {
        "asset": "Card_15_CircusArrives", "speaker": "Spk_Jester",
        "desc": {
            "en": "The Jester bounds in, bells jingling: 'A grand traveling circus has pitched tents outside the city gates, Percy! Acrobats, fire-eaters, and a blind Seer who claims she can see tomorrow. Shall we let them perform?'",
            "ar": "يقفز البهلوان وتدق أجراسه: «سيرك عظيم نصب خيامه قرب أسوار المدينة يا بيرسي! بهلوانيون، نافثو لهب، وعرّافة عمياء تزعم أنها ترى الغد! أنأذن لهم بالعرض؟»"
        },
        "left": {"en": "Welcome the circus! The people need joy.", "ar": "مرحباً بالسيرك! يحتاج الشعب إلى البهجة والترويح."},
        "left_res": {"Crown": 5, "Gold": -5, "Army": 10}, "left_next": "Card_16_CircusTents",
        "left_set_flags": FLAG_CIRCUS,
        "right": {"en": "Turn them away! Wandering troupes harbor spies.", "ar": "اطردوهم! فخلف أقنعة المهرجين يندس الجواسيس."},
        "right_res": {"Crown": -5, "Gold": 5, "Army": -5}, "right_next": "Card_QuietCity"
    },
    {
        "asset": "Card_QuietCity", "speaker": "Spk_Jester",
        "desc": {
            "en": "The square beyond the gates is packed earth and silence. The Jester jingles once, unsmiling: 'No tents, no Seer, no songs. The city will remember you turned the joy away, Percy.'",
            "ar": "الساحة خلف البوابات تراب صامت بلا خيام. يدق البهلوان جرساً واحداً دون ابتسام: «لا سيرك، ولا عرّافة، ولا أغاني. سيتذكر أهل المدينة أنك طردت الفرح يا بيرسي.»"
        },
        "left": {"en": "Post a notice that the troupe was a nest of spies.", "ar": "علّقوا بياناً أن الفرقة وكر جواسيس."},
        "left_res": {"Crown": 5, "Gold": 0, "Army": -5}, "left_next": "Card_18_BlightPetition",
        "right": {"en": "Send a modest feast to the taverns to soothe the city.", "ar": "أرسلوا مأدبة متواضعة إلى الحانات تطييباً للخاطر."},
        "right_res": {"Crown": -5, "Gold": -10, "Army": 10}, "right_next": "Card_18_BlightPetition"
    },

    # Thread 1: Jester Bloodline & Ruined Castle
    {
        "asset": "Card_16_CircusTents", "speaker": "Spk_Jester",
        "desc": {
            "en": "Between fiery acrobatic flips, the Jester leans over your throne and whispers without smiling: 'Percy... look at our hands. Have you ever wondered why your late father kept an artist locked away in the Ruined Castle across the marshes?'",
            "ar": "بين عروض الأكروبات، يدنو البهلوان من عرشك ويهمس دون ابتسام: «بيرسي... انظر إلى أيدينا. ألم تتساءل يوماً لماذا حبس والدك الراحل رساماً في القلعة المهدمة خلف المستنقعات؟»"
        },
        "left": {"en": "Slip out tonight and investigate the Ruined Castle.", "ar": "تسلل الليلة وتحقق من أسرار القلعة المهدمة."},
        "left_res": {"Crown": -5, "Gold": -5, "Army": 10}, "left_next": "Card_RuinedCastle",
        "right": {"en": "Silence, fool! Speak no insolence of my father.", "ar": "اصمت يا أحمق! لا تتفوه بوقاحة عن والدي الملك."},
        "right_res": {"Crown": 10, "Gold": 0, "Army": -5}, "right_next": "Card_18_BlightPetition"
    },
    {
        "asset": "Card_17_GallerySilence", "kind": "reaction", "speaker": "Spk_SpiritMother",
        "seed": "The ghost warns Percy of bloodline secrets and false certainty before the final audience.",
        "desc": {
            "en": "In the cold portrait gallery, your mother's ghost manifests, her eyes sorrowful: 'The truth is an uninvited guest, Percy. If you do not seek it willingly, it will kick down your palace doors.'",
            "ar": "في بهو اللوحات البارد، يتجسد طيف والدتك بعينين حزينتين: «الحقيقة ضيف لا يستأذن يا بيرسي. إن لم تفتح لها بابك راضياً، فستكسر أبواب قصرك عنوة!»"
        },
        "continue_next": "Card_34_JestersAudience"
    },
    {
        "asset": "Card_RuinedCastle", "speaker": "Spk_Jester",
        "desc": {
            "en": "In the mossy crypt of the Ruined Castle, you uncover a locked alcove. Inside rests a sealed portrait: your late father holding an infant boy beside a woman in motley—the Jester's true mother. He is your elder brother!",
            "ar": "في سرداب القلعة المهدمة، تعثر على حجرة سرية تخفي لوحة زيتية: والدك الراحل يحمل طفلاً رضيعاً إلى جانب امرأة بلباس البهلوانيين... إنها أم المهرج! البهلوان هو أخوك الأكبر الحقيقي!"
        },
        "left": {"en": "Burn the portrait to ash; keep the throne secure!", "ar": "احرق اللوحة حتى تصير رماداً؛ العرش لي وحدي!"},
        "left_res": {"Crown": 15, "Gold": 0, "Army": -15}, "left_next": "Card_BurnPortrait",
        "right": {"en": "Embrace him as brother and name him co-ruler!", "ar": "اعترف به شقيقاً وشاركه حكم المملكة بالعدل!"},
        "right_res": {"Crown": -10, "Gold": -5, "Army": 25}, "right_next": "Card_BrotherCoRuler",
        "right_set_flags": FLAG_BROTHER
    },
    {
        "asset": "Card_BurnPortrait", "speaker": "Spk_Chancellor",
        "desc": {
            "en": "Chancellor: 'Wisely done, Sire. The cinders are scattered to the winds. The sovereign right to rule must never be divided, lest the kingdom tear itself apart.'",
            "ar": "المستشار: «فعلت عين الصواب يا مولاي! تذرو الرياح رمادها، فحكم المملكة لا يحتمل شريكين، وإلا تمزق التاج إرباً.»"
        },
        "left": {"en": "Keep the Jester closely watched under guard.", "ar": "ضع البهلوان تحت مراقبة أمنية مشددة."},
        "left_res": {"Crown": 5, "Gold": -5, "Army": -5}, "left_next": "Card_18_BlightPetition",
        "right": {"en": "Grant him a generous estate far from court.", "ar": "امنحه ضيعة ريفية هادئة بعيداً عن البلاط."},
        "right_res": {"Crown": -5, "Gold": -10, "Army": 10}, "right_next": "Card_18_BlightPetition"
    },
    {
        "asset": "Card_BrotherCoRuler", "speaker": "Spk_Jester",
        "desc": {
            "en": "The Jester casts aside his jester's cap, tears in his eyes: 'Percy... I wanted neither your gold nor your crown, only the truth. Together, we can govern this land with both sword and compassion.'",
            "ar": "يخلع البهلوان قبعته وعيناه تفيضان بالدموع: «يا بيرسي... لم أرد تاجك ولا ذهبك، بل أردت الحقيقة فقط. معاً، سنقود هذه المملكة بالسيف والحكمة معاً.»"
        },
        "left": {"en": "Affirm our secret pact; prepare to face the realm.", "ar": "ثبّت عهد الأخوة؛ واستعد لمواجهة أزمات المملكة معاً."},
        "left_res": {"Crown": 10, "Gold": 5, "Army": 15}, "left_next": "Card_18_BlightPetition",
        "left_set_flags": FLAG_BROTHER,
        "right": {"en": "Prepare the royal decree of joint succession.", "ar": "أعد مرسوماً ملكياً بوراثة مشتركة للعرش."},
        "right_res": {"Crown": 5, "Gold": -5, "Army": 20}, "right_next": "Card_18_BlightPetition",
        "right_set_flags": FLAG_BROTHER
    },

    # Thread 3: Crop Blight & Peasant Rebellion Leader
    {
        "asset": "Card_18_BlightPetition", "kind": "petition", "speaker": "Spk_Merchant",
        "seed": "A commoner petitions the king for grain relief after crop blight destroys the harvest.",
        "desc": {
            "en": "A weather-beaten farmer's representative kneels before you: 'Sire, black rot has consumed half our grain fields. If you demand full taxes, our children will starve before winter sets in!'",
            "ar": "يركع فلاح أجهدته السنون بين يديك: «يا مولاي، التهم العفن الأسود نصف حقول القمح! إن ألزمتمونا بكامل الخراج، فستهلك عائلاتنا جوعاً قبل قدوم الشتاء!»"
        },
        "continue_next": "Card_PeasantLeader"
    },
    {
        "asset": "Card_PeasantLeader", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'Sire! Five thousand furious peasants have marched upon the palace gate, demanding bread! We arrested their ringleader in chains. What is his sentence?'",
            "ar": "الجنرال: «يا مولاي! خمسة آلاف فلاح غاضب يحاصرون بوابات القصر مطالبين بالخبز! اعتقلنا زعيم تمردهم بالأصفاد. فما حكمك عليه؟»"
        },
        "gate": {"resource": "Army", "min": 0, "max": 55},
        "skip_to": "Card_20_HoodedBandit",
        "left": {"en": "Execute him publicly; quell this treason with iron!", "ar": "أعدمه علناً في الميدان لردع الرعاع عن العصيان!"},
        "left_res": {"Crown": 10, "Gold": 0, "Army": -20}, "left_next": "Card_ExecuteRebel",
        "right": {"en": "Open royal granaries! Feed them and grant a pardon.", "ar": "افتحوا صوامع الغلال الملكية! أطعموهم واصفحوا عنهم."},
        "right_res": {"Crown": -5, "Gold": -15, "Army": 25}, "right_next": "Card_OpenGranaries"
    },
    {
        "asset": "Card_ExecuteRebel", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'The rebel swung from the gallows at noon. The crowds backed away in sullen dread, but their eyes burn with suppressed fury. We must stay on high alert.'",
            "ar": "الجنرال: «نُفذ الإعدام شنقاً عند الظهيرة. تفرقت الحشود في وجوم وخوف، لكن نيران الغضب تتأجج في صدورهم. الحيطة واجبة.»"
        },
        "left": {"en": "Double the palace night watches.", "ar": "ضاعف حراسات القصر الليلية تحسباً للغدر."},
        "left_res": {"Crown": 5, "Gold": -5, "Army": -5}, "left_next": "Card_20_HoodedBandit",
        "right": {"en": "Offer a minor bread dole to calm the widows.", "ar": "وزّع صدقات خبز خفيفة لتهدئة الأرامل والمحتاجين."},
        "right_res": {"Crown": 0, "Gold": -5, "Army": 10}, "right_next": "Card_20_HoodedBandit"
    },
    {
        "asset": "Card_OpenGranaries", "speaker": "Spk_Merchant",
        "desc": {
            "en": "Merchant: 'Sire, grain wagons roll through every district. The people are weeping with joy, shouting blessings on your name! The riot has turned into a celebration.'",
            "ar": "كبير التجار: «عربات الحبوب تجوب الأسواق يا مولاي! الناس يبكون فرحاً ويدعون لكم بطول البقاء، وانقلب الشغب إلى احتفال مهيب.»"
        },
        "left": {"en": "Commission local guilds to organize autumn planting.", "ar": "كلّف النقابات بتنظيم مواسم البذر القادمة."},
        "left_res": {"Crown": 5, "Gold": -5, "Army": 10}, "left_next": "Card_20_HoodedBandit",
        "right": {"en": "Enlist grateful young farmers into the royal militia.", "ar": "جنّد شبابهم الراغبين في الدفاع عن العرش."},
        "right_res": {"Crown": 5, "Gold": -5, "Army": 15}, "right_next": "Card_20_HoodedBandit"
    },

    # Thread 2: Hooded Bandit & Chancellor Conspiracy
    {
        "asset": "Card_20_HoodedBandit", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'Sire! We raided the outlaw stronghold and dragged the Hooded Bandit to your throne. Searching his chest revealed sealed letters and coin purses stamped with the Chancellor's private sigil!'",
            "ar": "الجنرال: «يا مولاي! داهمنا وكر قطاع الطرق وجلبنا زعيمهم المقنع مقيداً! عثرنا في حوزته على رسائل سرية وصرر ذهب ممهورة بختم المستشار الخاص!»"
        },
        "left": {"en": "Raid the Chancellor's estate immediately!", "ar": "داهموا قصر المستشار فوراً واعتقلوه بتهمة الخيانة!"},
        "left_res": {"Crown": 10, "Gold": 15, "Army": 10}, "left_next": "Card_RaidChancellor",
        "right": {"en": "Turn the Bandit into a double agent to bait him!", "ar": "استدرج المستشار واجعل قاطع الطريق عميلاً مزدوجاً!"},
        "right_res": {"Crown": 5, "Gold": 5, "Army": 15}, "right_next": "Card_BanditDoubleAgent",
        "gate": {"resource": "Crown", "min": 50, "max": 100},
        "skip_to": "Card_23_SeersOmen"
    },
    {
        "asset": "Card_RaidChancellor", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'Our guards breached the Chancellor's residence! We seized his secret ledgers and intercepted his sleeper couriers. The conspirator is chained in the dungeon, and his stolen loot is back in your vault!'",
            "ar": "الجنرال: «اقتحم حرسنا دار المستشار! ضبطنا دفاتره السرية ومراسلاته المشبوهة، وأودعناه في أعمق الزنازين، وعاد الذهب المنهوب إلى خزينتك!»"
        },
        "left": {"en": "Condemn him to solitary confinement for life.", "ar": "احكم عليه بالسجن المؤبد في زنزانة انفرادية."},
        "left_res": {"Crown": 10, "Gold": 5, "Army": 5}, "left_next": "Card_23_SeersOmen",
        "left_set_flags": FLAG_CHANCELLOR,
        "right": {"en": "Confiscate all his properties to fund our army.", "ar": "صادر كامل أملاكه لصالح تعزيز دفاعات المملكة."},
        "right_res": {"Crown": 5, "Gold": 15, "Army": 10}, "right_next": "Card_23_SeersOmen",
        "right_set_flags": FLAG_CHANCELLOR
    },
    {
        "asset": "Card_BanditDoubleAgent", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'The trap worked flawlessly, Sire! The Chancellor arrived at the abandoned mill with payment for another raid—only to be surrounded by our loyal crossbowmen. His corrupt network is shattered.'",
            "ar": "الجنرال: «نجح الفخ بإحكام يا مولاي! حضر المستشار إلى الطاحونة المهجورة لتسليم ثمن غارة جديدة، ليجد نفسه محاصراً برماة سهامنا المخلصين!»"
        },
        "left": {"en": "Strip him of rank and grant the Bandit a pardon.", "ar": "جرّده من ألقابه، وامنح قاطع الطريق عفواً لتعاونه."},
        "left_res": {"Crown": 5, "Gold": 10, "Army": 10}, "left_next": "Card_23_SeersOmen",
        "left_set_flags": FLAG_CHANCELLOR,
        "right": {"en": "Recruit the Bandit's men as an elite scout vanguard.", "ar": "ضم رجال قاطع الطريق إلى طليعة الاستطلاع الملكي."},
        "right_res": {"Crown": 5, "Gold": -5, "Army": 15}, "right_next": "Card_23_SeersOmen",
        "right_set_flags": FLAG_CHANCELLOR
    },
    {
        "asset": "Card_23_SeersOmen", "speaker": "Spk_Seer",
        "desc": {
            "en": "The blind Seer raises her withered hands through aromatic incense: 'Little king... the western sea churns with warships. War or coin, blood or bread—the tide comes for your throne. How will you answer?'",
            "ar": "ترفع العرّافة العمياء كفيها وسط دخان البخور العبق: «أيها الملك الصغير... بحار الغرب تموج بالسفن الحربية! حربٌ أم ذهب، دمٌ أم خبز—المد القادم يهدد عرشك، فكيف ستجيب؟»"
        },
        "alt_desc_flags": FLAG_BROTHER,
        "alt_desc": {
            "en": "The blind Seer smiles toward the bells at your side: 'Two faces on one coin, little king. The western sea churns with warships. War or coin—the tide comes for both your thrones. How will you answer?'",
            "ar": "تبتسم العرّافة العمياء نحو الأجراس إلى جوارك: «وجهان لعملة واحدة أيها الملك الصغير. بحار الغرب تموج بالسفن. حربٌ أم ذهب—المدّ قادم نحو عرشيْكما. فكيف ستجيب؟»"
        },
        "entry_flags": FLAG_CIRCUS,
        "skip_to": "Card_24_WesternThreat",
        "skip_to_alt": "Card_29_WesternToll",
        "left": {"en": "Prepare war galleys and burn their fleet first!", "ar": "جهّز السفن الحربية وأشعل النيران في أسطولهم مباغتة!"},
        "left_res": {"Crown": 5, "Gold": -10, "Army": 10}, "left_next": "Card_24_WesternThreat",
        "right": {"en": "Strengthen our harbors and propose trade tolls.", "ar": "حصّن موانئنا واعرض عليهم اتفاقاً لتنظيم رسوم الملاحة."},
        "right_res": {"Crown": -5, "Gold": 10, "Army": -5}, "right_next": "Card_29_WesternToll"
    },

    # ─── ACT III: WESTERN KINGDOM CRISIS (مملكة الغرب) ────────────────────────
    {
        "asset": "Card_24_WesternThreat", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'Sire! The Western Kingdom has mobilized its grand armada. Their warships patrol our maritime borders. War is at our threshold!'",
            "ar": "الجنرال: «يا مولاي! حشدت مملكة الغرب أسطولها البحري العظيم، وسفنهم تجوب سواحلنا بتحدٍّ سافر. الحرب على الأبواب!»"
        },
        "alt_desc_flags": FLAG_CHANCELLOR,
        "alt_desc": {
            "en": "General: 'Sire! The Western Kingdom has mobilized its grand armada. With the Chancellor in chains we have no silk-tongue to send. Their warships patrol our maritime borders. War is at our threshold!'",
            "ar": "الجنرال: «يا مولاي! حشدت مملكة الغرب أسطولها. والمستشار في الأصفاد، فلا لسان حرير نرسله. سفنهم تجوب سواحلنا. الحرب على الأبواب!»"
        },
        "gate": {"resource": "Army", "min": 40, "max": 100},
        "skip_to": "Card_29_WesternToll",
        "left": {"en": "Launch a nighttime fire-ship raid against their fleet!", "ar": "أطلق سفناً محملة بالنيران ليلاً لإحراق سفنهم الراسية!"},
        "left_res": {"Crown": 10, "Gold": -15, "Army": 15}, "left_next": "Card_25_FirstStrike_FleetBurns",
        "right": {"en": "Levy an emergency defense fund from coastal merchants.", "ar": "افرض ضريبة دفاع طارئة على تجار السواحل."},
        "right_res": {"Crown": -5, "Gold": 15, "Army": -10}, "right_next": "Card_26_FirstStrike_Levy"
    },
    {
        "asset": "Card_25_FirstStrike_FleetBurns", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'A triumph, Sire! The western flagship burns brightly against the night sky! Their surviving captains are suing for an armistice.'",
            "ar": "الجنرال: «نصر مؤزر يا مولاي! سفينتهم القيادية تحترق في كبد الليل، وبقية قادتهم يرفعون رايات طلب الهدنة!»"
        },
        "left": {"en": "Demand reparations and establish a naval treaty.", "ar": "افرض شروط هدنة صارمة تلزمهم بتعويضات بحرية."},
        "left_res": {"Crown": 10, "Gold": 10, "Army": 10}, "left_next": "Card_28_FirstStrike_Truce",
        "right": {"en": "Hunt down their fleeing vessels and claim complete victory.", "ar": "طارد فلول سفنهم وحقق نصراً بحرياً حاسماً."},
        "right_res": {"Crown": 15, "Gold": -10, "Army": 15}, "right_next": "Card_27_WarMother"
    },
    {
        "asset": "Card_26_FirstStrike_Levy", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Treasurer: 'The coastal merchants grumble, but the funds arrived in time to hire seasoned mercenary frigates. Our harbor is secure.'",
            "ar": "أمين الخزينة: «تذمر تجار السواحل، لكن الأموال وصلت في موعدها لاستئجار فرقاطات مرتزقة حصّنت موانئنا.»"
        },
        "left": {"en": "Send the frigates to patrol Western trade routes.", "ar": "أرسل الفرقاطات لاعتراض خطوط التجارة الغربية."},
        "left_res": {"Crown": 5, "Gold": 10, "Army": 5}, "left_next": "Card_28_FirstStrike_Truce",
        "right": {"en": "Reinforce coastal forts and hold defensive posture.", "ar": "ادعم الحصون الشاطئية والتزم بالدفاع المدروس."},
        "right_res": {"Crown": 0, "Gold": -5, "Army": 10}, "right_next": "Card_28_FirstStrike_Truce"
    },
    {
        "asset": "Card_27_WarMother", "kind": "reaction", "speaker": "Spk_SpiritMother",
        "seed": "The ghost reflects on the heavy cost of triumph and pride.",
        "desc": {
            "en": "Your mother's ghost stands before the battle-smoke: 'You won the sea, Percy. But pride built upon salt and blood can crumble in a single heartbeat. Remember who holds your realm together.'",
            "ar": "يقف طيف والدتك وسط سحب الدخان المتلاشية: «ربحت البحر يا بيرسي... لكن العزة المبنية على الملح والدم قد تنهار في رمشة عين! تذكّر من يحفظ مملكتك حقاً.»"
        },
        "continue_next": "Card_17_GallerySilence"
    },
    {
        "asset": "Card_28_FirstStrike_Truce", "speaker": "Spk_General",
        "desc": {
            "en": "General: 'The Western armada has withdrawn beyond the horizon. Our borders are calm once more, but rumors swirl in the capital.'",
            "ar": "الجنرال: «انسحب الأسطول الغربي إلى ما وراء الأفق. هدأت الحدود مجدداً، لكن الشائعات بدأت تروج في أزقة العاصمة.»"
        },
        "left": {"en": "Reward the soldiers with victory banners and rest.", "ar": "كافئ الجنود بأوسمة النصر وأيام راحة."},
        "left_res": {"Crown": 5, "Gold": -5, "Army": 15}, "left_next": "Card_17_GallerySilence",
        "right": {"en": "Inspect the capital garrison and fortify palace walls.", "ar": "تفقّد حاميات العاصمة وحصّن أسوار القصر الملكي."},
        "right_res": {"Crown": 10, "Gold": -5, "Army": 5}, "right_next": "Card_17_GallerySilence"
    },
    {
        "asset": "Card_29_WesternToll", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Treasurer: 'Sire, Western merchants offer a fat annual tribute in exchange for exempting their wine and grain fleet from our harbor tolls.'",
            "ar": "أمين الخزينة: «يا مولاي، يعرض تجار الغرب جزية سنوية سخية مقابل إعفاء سفنهم من رسوم الموانئ البحرية.»"
        },
        "left": {"en": "Accept their gold; coin speaks louder than steel.", "ar": "اقبل ذهبهم؛ فالمال أبلغ صوتاً من قرع السيوف."},
        "left_res": {"Crown": -5, "Gold": 20, "Army": -10}, "left_next": "Card_30_TributeSkim",
        "right": {"en": "Refuse; foreign merchants must respect royal sovereignty.", "ar": "ارفض العرض؛ فلا مساومة على سيادة موانئنا."},
        "right_res": {"Crown": 10, "Gold": -5, "Army": 10}, "right_next": "Card_32_GrainWar"
    },
    {
        "asset": "Card_30_TributeSkim", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Treasurer: 'A glorious harvest of gold, Sire! I took only a modest administrative fee for my ledger work. Our vault shines like the sun!'",
            "ar": "أمين الخزينة: «موسم ذهبي يا مولاي! لم أقتطع سوى نسبة إدارية يسيرة مقابل جهدي الحسابي، وخزائننا تشرق كالشمس!»"
        },
        "left": {"en": "Overlook his fee; prosperity justifies rewards.", "ar": "تغاضَ عن عمولته؛ فالرخاء يبرر المكافأة."},
        "left_res": {"Crown": -5, "Gold": 15, "Army": -5}, "left_next": "Card_31_TreasuryDepleted",
        "right": {"en": "Audit his ledgers and recover every stolen ounce.", "ar": "راجع سجلاته بدقة واسترد كل مثقال ذهب."},
        "right_res": {"Crown": 10, "Gold": 10, "Army": 10}, "right_next": "Card_33_SmugglerBargain"
    },
    {
        "asset": "Card_31_TreasuryDepleted", "speaker": "Spk_Merchant",
        "desc": {
            "en": "Merchant: 'Sire, foreign merchants bought up our domestic reserves! Prices in the market are skyrocketing, and families demand bread.'",
            "ar": "كبير التجار: «يا مولاي، اشترى تجار الغرب مخزوناتنا المحلية! أسعار الأسواق تلتهب، والأهالي يستغيثون من شح الخبز.»"
        },
        "left": {"en": "Subsidize bakeries from the royal reserves.", "ar": "ادعم المخابز بالدقيق فوراً من الاحتياطي الملكي."},
        "left_res": {"Crown": 5, "Gold": -15, "Army": 15}, "left_next": "Card_17_GallerySilence",
        "right": {"en": "Ban the export of all edible grains immediately.", "ar": "امنع تصدير الحبوب والقمح إلى الخارج كلياً."},
        "right_res": {"Crown": 5, "Gold": -5, "Army": 10}, "right_next": "Card_17_GallerySilence"
    },
    {
        "asset": "Card_32_GrainWar", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Treasurer: 'In retaliation for the rejected deal, Westernia has halted all salt shipments to our kingdom. We must source salt elsewhere!'",
            "ar": "أمين الخزينة: «رداً على رفض الاتفاق، قطعت مملكة الغرب شحنات الملح عن أسواقنا! لا بد من تدبير بديل سريعاً.»"
        },
        "left": {"en": "Buy salt from the Eastern Kingdom at premium prices.", "ar": "اشترِ الملح من مملكة الشرق ولو بأسعار مضاعفة."},
        "left_res": {"Crown": 5, "Gold": -15, "Army": 10}, "left_next": "Card_33_SmugglerBargain",
        "right": {"en": "Ration domestic salt and commission sea salt pans.", "ar": "رشّد استهلاك الملح واستصلح ملاحات بحرية خاصة."},
        "right_res": {"Crown": -5, "Gold": 5, "Army": -5}, "right_next": "Card_33_SmugglerBargain"
    },
    {
        "asset": "Card_33_SmugglerBargain", "speaker": "Spk_Merchant",
        "desc": {
            "en": "Merchant: 'A guild of private smugglers offers to bring in foreign goods under Westernia's nose, provided we turn a blind eye to contraband.'",
            "ar": "كبير التجار: «تعرض نقابة من المهربين جلب البضائع تحت أنوف أساطيل الغرب، شريطة أن نتغاضى عن بعض شحناتهم غير المرخصة.»"
        },
        "left": {"en": "License the smugglers quietly; survival requires cunning.", "ar": "رخّص لهم سراً؛ فالبقاء في الأزمات يتطلب الدهاء."},
        "left_res": {"Crown": -5, "Gold": 10, "Army": 5}, "left_next": "Card_17_GallerySilence",
        "right": {"en": "Hang the smugglers; the realm will not deal in vice.", "ar": "اقبض على المهربين؛ فلن يدنس التاج شرفه بالخروج عن القانون."},
        "right_res": {"Crown": 10, "Gold": -5, "Army": 5}, "right_next": "Card_17_GallerySilence"
    },

    # ─── ACT IV: CLIMAX & MULTIPLE AUTHORED ENDINGS ───────────────────────────
    {
        "asset": "Card_34_JestersAudience", "kind": "chat", "speaker": "Spk_Jester",
        "seed": "The Jester conducts a final heart-to-heart with King Percy about the meaning of power.",
        "desc": {
            "en": "The Jester sits casually upon your throne steps, juggling three golden apples: 'Well, Percy. We fought wars, squeezed merchants, calmed rebellions, and stared into the dark. What sort of king do you want the songs to remember?'",
            "ar": "يجلس البهلوان على درجات العرش يتلاعب بثلاث تفاحات ذهبية: «حسناً يا بيرسي... خضنا الحروب، وسوّينا الأزمات، وخاطبنا الشعوب في الساحات. فأي ملك تريد أن تذكره أغاني الرواة في الغد؟»"
        },
        "continue_next": "Card_36_FoolsVerdict"
    },
    {
        "asset": "Card_36_FoolsVerdict", "kind": "verdict", "speaker": "Spk_Jester",
        "desc": {
            "en": "The Jester stands and bows deeply: 'The hour of your legacy has arrived, Sire. Will you seal the bond of blood, rule with absolute iron, or leave the golden cage behind?'",
            "ar": "يقف البهلوان وينحني بإجلال عميق: «دقت ساعة الحصاد يا مولاي. فهل نتوج عهد الأخوة المشتركة، أم تحكم بالحديد والنار، أم تغادر هذا القفص الذهبي؟»"
        },
        "left": {"en": "Crown my brother beside me; two brothers upon one throne!", "ar": "تتويج أخي شريكاً على العرش؛ أخوان على عرش واحد!"},
        "left_res": {"Crown": 10, "Gold": 0, "Army": 25}, "left_next": "Card_End_TwoBrothers",
        "left_requires_flags": FLAG_BROTHER,
        "middle": {"en": "Lay down the crown; I choose to be just Percy.", "ar": "أضع التاج على وسادته برفق وأغادر... أريد أن أكون بيرسي الإنسان فقط."},
        "middle_res": {"Crown": -20, "Gold": 0, "Army": 0}, "middle_next": "Card_35_End_Abdication",
        "right": {"en": "Assess the realm's balance and decree my final reign.", "ar": "استعرض حال المملكة وأصدر مرسوم الحكم النهائي."},
        "right_res": {"Crown": 5, "Gold": 5, "Army": 5}, "right_next": "Card_EndingEvaluator"
    },
    {
        "asset": "Card_EndingEvaluator", "kind": "evaluator",
        "desc": {
            "en": "The realm holds its breath. History will name you by the king you became.",
            "ar": "المملكة تحبس أنفاسها. سيمنحك التاريخ لقبه حسب الملك الذي كنته."
        },
        # Unity reads these four outgoing links and routes to the one matching the player's resource/flag state
        "endings": ["Card_37_End_Tyrant", "Card_38_End_ShadowKing", "Card_39_End_WarlordsPeace", "Card_40_End_MerchantKing"]
    },

    # Endings (Terminal)
    {
        "asset": "Card_End_TwoBrothers", "kind": "ending", "speaker": "Spk_Jester",
        "desc": {
            "en": "Two brothers upon one throne—one wearing the gilded crown, the other in bells and ribbons. Together, you dismantled corruption, united the people, and turned court intrigue into legendary harmony.",
            "ar": "أخوان على عرشٍ واحد؛ أحدهما يتوشح بالتاج الذهبي والصولجان، والآخر بالأجراس والدهاء. معاً، طهّرتما البلاط من الفساد، وأحللتما الوفاق والعدالة في أرجاء المملكة لتخلد ذكراكم في كتب التاريخ."
        }
    },
    {
        "asset": "Card_35_End_Abdication", "kind": "ending", "speaker": "Spk_SpiritMother",
        "desc": {
            "en": "You lay your crown upon the empty velvet cushion and walk out through the orchard gate at sunrise. The air is sweet, unburdened by decrees and blood. You are finally just Percy.",
            "ar": "وضعت تاجك بهدوء فوق الوسادة المخملية وخرجت عند شروق الشمس من بوابات البساتين. نسيم الصباح نقي خفيف بلا مراسيم ولا دماء... لقد عدت أخيراً بيرسي الإنسان."
        }
    },
    {
        "asset": "Card_37_End_Tyrant", "kind": "ending", "speaker": "Spk_General",
        "desc": {
            "en": "Your word is steel and your decree is final law. No minister dares whisper dissent; no neighbor tests your border. The realm is at absolute peace, silent as a graveyard.",
            "ar": "كلمتك صارت من فولاذ وأمرك قانون نافذ. لا يجرؤ مستشار على الهمس بمعارضة، ولا يجرؤ جار على اختبار حدودك. خيّم على المملكة سلام مطلق... صامت كالمقابر."
        }
    },
    {
        "asset": "Card_38_End_ShadowKing", "kind": "ending", "speaker": "Spk_Chancellor",
        "desc": {
            "en": "You sit beneath the heavy canopy while ministers nod and sign decrees in your name. The realm functions smoothly, but you realize with a cold chill: you rule nothing. The machine rules you.",
            "ar": "تجلس تحت المظلة الملكية الفخمة بينما يوقع الوزراء المراسيم باسمك. تسير الدولة بانتظام، لكنك تدرك برعدة باردة في قلبك: أنت لا تحكم شيئاً... بل الآلة البيروقراطية هي التي تحكمك."
        }
    },
    {
        "asset": "Card_39_End_WarlordsPeace", "kind": "ending", "speaker": "Spk_General",
        "desc": {
            "en": "With three victorious campaigns behind you, your borders stretch across mountain and sea. Foreign kings pay tribute; your generals stand like pillars of iron around your throne.",
            "ar": "بثلاث حملات ظافرة، امتدت حدود مملكتك عبر الجبال والبحار. يدفع الملوك الجزية طائعين، ويقف جنرالاتك كأعمدة من حديد حماية لعرشك العظيم."
        }
    },
    {
        "asset": "Card_40_End_MerchantKing", "kind": "ending", "speaker": "Spk_Treasurer",
        "desc": {
            "en": "Gold overflows from every vault into grand public fountains. Your merchant navy dominates the known seas. You bought peace where steel failed, and your coins bear your smiling face.",
            "ar": "فاض الذهب من الخزائن ليزيّن نوافير المدن العظمى، وسيطر أسطولك التجاري على بحار العالم المعروف. اشتريت السلام بالرخاء حيث عجزت السيوف، وصار وجهك المبتسم منقوشاً على كل دينار."
        }
    }
]

# Layout coordinates for Twine / Visual Graph
GRAPH_LAYOUT = {
    "Card_01_MotherGhost": (0, 3),
    "Card_02_EasternRaids": (1, 1),
    "Card_03_Conquest_March": (2, 0),
    "Card_04_Conquest_IronGrip": (3, 0),
    "Card_05_Conquest_Taxation": (4, 0),
    "Card_06_Conquest_Mercy": (3, 1),
    "Card_07_Conquest_GarrisonCost": (4, 1),
    "Card_08_EasternResistance": (2, 2),
    "Card_09_Alliance_Relief": (1, 5),
    "Card_10_Alliance_Pact": (2, 4),
    "Card_11_Alliance_Tolls": (3, 4),
    "Card_12_Alliance_Gifts": (2, 6),
    "Card_13_Alliance_Sovereignty": (3, 5),
    "Card_14_Alliance_BorderPost": (4, 5),
    # Act II
    "Card_15_CircusArrives": (5, 3),
    "Card_16_CircusTents": (6, 1),
    "Card_QuietCity": (6, 4),
    "Card_17_GallerySilence": (13, 3),
    "Card_RuinedCastle": (6, 0),
    "Card_BurnPortrait": (7, 0),
    "Card_BrotherCoRuler": (8, 0),
    "Card_18_BlightPetition": (6, 3),
    "Card_PeasantLeader": (8, 3),
    "Card_ExecuteRebel": (9, 2),
    "Card_OpenGranaries": (9, 4),
    "Card_20_HoodedBandit": (7, 5),
    "Card_RaidChancellor": (8, 5),
    "Card_BanditDoubleAgent": (8, 6),
    "Card_23_SeersOmen": (10, 3),
    # Act III
    "Card_24_WesternThreat": (11, 2),
    "Card_25_FirstStrike_FleetBurns": (12, 1),
    "Card_26_FirstStrike_Levy": (12, 2),
    "Card_27_WarMother": (13, 1),
    "Card_28_FirstStrike_Truce": (13, 2),
    "Card_29_WesternToll": (11, 5),
    "Card_30_TributeSkim": (12, 4),
    "Card_31_TreasuryDepleted": (13, 4),
    "Card_32_GrainWar": (12, 5),
    "Card_33_SmugglerBargain": (13, 5),
    # Act IV & Endings
    "Card_34_JestersAudience": (14, 3),
    "Card_36_FoolsVerdict": (15, 3),
    "Card_EndingEvaluator": (16, 3),
    "Card_End_TwoBrothers": (17, 0),
    "Card_35_End_Abdication": (17, 1),
    "Card_37_End_Tyrant": (17, 2),
    "Card_38_End_ShadowKing": (17, 3),
    "Card_39_End_WarlordsPeace": (17, 4),
    "Card_40_End_MerchantKing": (17, 5),
}


def make_yaml_str(s):
    if not s:
        return '""'
    escaped = s.replace('\\', '\\\\').replace('"', '\\"').replace('\n', '\\n')
    return f'"{escaped}"'


def write_meta(path, guid):
    content = f"fileFormatVersion: 2\nguid: {guid}\nNativeFormatImporter:\n  externalObjects: {{}}\n  mainObjectFileID: 11400000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n"
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(content)


def card_ref(card_guids, name):
    if not name:
        return "{fileID: 0}"
    return f"{{fileID: 11400000, guid: {card_guids[name]}, type: 2}}"


def resource_ref(name):
    if not name:
        return "{fileID: 0}"
    return f"{{fileID: 11400000, guid: {RESOURCES[name]['guid']}, type: 2}}"


def generate_card_asset(c, card_guids):
    name = c["asset"]
    guid = card_guids[name]
    kind = c.get("kind", "choice")
    speaker_guid = SPEAKERS[c["speaker"]]["guid"] if c.get("speaker") else "0"
    spk_ref = f"{{fileID: 11400000, guid: {speaker_guid}, type: 2}}" if speaker_guid != "0" else "{fileID: 0}"

    is_reaction = 1 if kind == "reaction" else 0
    is_petition = 1 if kind == "petition" else 0
    is_chat = 1 if kind == "chat" else 0
    is_three_way = 1 if kind == "verdict" else 0
    is_evaluator = 1 if kind == "evaluator" else 0
    seed = c.get("seed", "")

    middle_ref = "{fileID: 0}"
    if kind == "ending":
        left_ref = "{fileID: 0}"
        right_ref = "{fileID: 0}"
        cont_ref = "{fileID: 0}"
    elif kind == "evaluator":
        endings = c["endings"]
        left_ref = card_ref(card_guids, endings[0])
        right_ref = card_ref(card_guids, endings[1])
        cont_ref = card_ref(card_guids, endings[2])
        middle_ref = card_ref(card_guids, endings[3])
    elif kind in ("reaction", "petition", "chat"):
        left_ref = "{fileID: 0}"
        right_ref = "{fileID: 0}"
        cont_ref = card_ref(card_guids, c["continue_next"])
    elif kind == "verdict":
        left_ref = card_ref(card_guids, c["left_next"])
        right_ref = card_ref(card_guids, c["right_next"])
        middle_ref = card_ref(card_guids, c["middle_next"])
        cont_ref = middle_ref
    else:
        left_ref = card_ref(card_guids, c["left_next"])
        right_ref = card_ref(card_guids, c["right_next"])
        cont_ref = "{fileID: 0}"

    def build_res_yaml(res_dict):
        lines = ["    values:"]
        for r_name in ["Crown", "Gold", "Army"]:
            if r_name in res_dict:
                val = res_dict[r_name]
                r_guid = RESOURCES[r_name]["guid"]
                lines.append(f"    - resource: {{fileID: 11400000, guid: {r_guid}, type: 2}}")
                lines.append(f"      value: {val}")
        return "\n".join(lines)

    left_res_yaml = build_res_yaml(c.get("left_res", {}))
    right_res_yaml = build_res_yaml(c.get("right_res", {}))
    middle_res_yaml = build_res_yaml(c.get("middle_res", {}))

    left_en = make_yaml_str(c["left"]["en"]) if "left" in c else '""'
    left_ar = make_yaml_str(c["left"]["ar"]) if "left" in c else '""'
    right_en = make_yaml_str(c["right"]["en"]) if "right" in c else '""'
    right_ar = make_yaml_str(c["right"]["ar"]) if "right" in c else '""'
    middle_en = make_yaml_str(c["middle"]["en"]) if "middle" in c else '""'
    middle_ar = make_yaml_str(c["middle"]["ar"]) if "middle" in c else '""'

    desc_en = make_yaml_str(c["desc"]["en"])
    desc_ar = make_yaml_str(c["desc"]["ar"])
    alt = c.get("alt_desc") or {}
    alt_en = make_yaml_str(alt.get("en", ""))
    alt_ar = make_yaml_str(alt.get("ar", ""))
    alt_flags = int(c.get("alt_desc_flags", 0))
    alt_missing = 1 if c.get("alt_desc_if_missing") else 0

    seed_escaped = seed.replace('"', '\\"') if seed else ""
    petitioner_src = 1 if is_chat else 0

    gate = c.get("gate") or {}
    has_gate = 1 if gate else 0
    gate_res = resource_ref(gate.get("resource")) if gate else "{fileID: 0}"
    gate_min = int(gate.get("min", 0))
    gate_max = int(gate.get("max", 100))
    skip_ref = card_ref(card_guids, c["skip_to"]) if c.get("skip_to") else "{fileID: 0}"
    skip_alt_ref = card_ref(card_guids, c["skip_to_alt"]) if c.get("skip_to_alt") else "{fileID: 0}"

    asset_content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 3a58f704bd5e67641b99574144c55bc6, type: 3}}
  m_Name: {name}
  m_EditorClassIdentifier: 
  assetName: {name}
  displayNameLocalized:
    english: 
    arabic: 
  speaker: {spk_ref}
  descriptionLocalized:
    english: {desc_en}
    arabic: {desc_ar}
  dayAdvance: 1
  leftChoiceLocalized:
    english: {left_en}
    arabic: {left_ar}
  leftResourceChange:
{left_res_yaml}
  leftNextCard: {left_ref}
  rightChoiceLocalized:
    english: {right_en}
    arabic: {right_ar}
  rightResourceChange:
{right_res_yaml}
  rightNextCard: {right_ref}
  middleChoiceLocalized:
    english: {middle_en}
    arabic: {middle_ar}
  middleResourceChange:
{middle_res_yaml}
  middleNextCard: {middle_ref}
  isThreeWayVerdict: {is_three_way}
  isEndingEvaluator: {is_evaluator}
  leftSetFlags: {int(c.get("left_set_flags", 0))}
  rightSetFlags: {int(c.get("right_set_flags", 0))}
  middleSetFlags: {int(c.get("middle_set_flags", 0))}
  continueSetFlags: {int(c.get("continue_set_flags", 0))}
  leftRequiresFlags: {int(c.get("left_requires_flags", 0))}
  rightRequiresFlags: {int(c.get("right_requires_flags", 0))}
  middleRequiresFlags: {int(c.get("middle_requires_flags", 0))}
  alternateDescriptionIfFlags: {alt_flags}
  alternateDescriptionIfMissing: {alt_missing}
  alternateDescriptionLocalized:
    english: {alt_en}
    arabic: {alt_ar}
  entryRequiresFlags: {int(c.get("entry_flags", 0))}
  hasResourceGate: {has_gate}
  gateResource: {gate_res}
  gateMinInclusive: {gate_min}
  gateMaxInclusive: {gate_max}
  skipToCard: {skip_ref}
  skipToAltCard: {skip_alt_ref}
  isLlmReactionCard: {is_reaction}
  reactionSeedOverride: "{seed_escaped}"
  isPetitionCard: {is_petition}
  petitionSeedOverride: "{seed_escaped}"
  petitionerSource: {petitioner_src}
  isChatCard: {is_chat}
  chatSeedOverride: "{seed_escaped}"
  continueNextCard: {cont_ref}
  artMode: 0
  cardImage: {{fileID: 0}}
  visualTemplate: {{fileID: 0}}
"""
    asset_path = os.path.join(CARDS_DIR, f"{name}.asset")
    with open(asset_path, "w", encoding="utf-8", newline="\n") as f:
        f.write(asset_content)
    write_meta(f"{asset_path}.meta", guid)


def generate_speakers():
    for key, data in SPEAKERS.items():
        guid = data["guid"]
        path = os.path.join(SPEAKERS_DIR, f"{key}.asset")
        en_name = make_yaml_str(data["en"])
        ar_name = make_yaml_str(data["ar"])
        prompt_str = make_yaml_str(data["prompt"])
        portrait_str = data["portrait"]

        content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: e6ce3d4284c7ed74ca7fb22aa2548095, type: 3}}
  m_Name: {key}
  m_EditorClassIdentifier: 
  assetName: {key}
  displayNameLocalized:
    english: {en_name}
    arabic: {ar_name}
  portrait: {portrait_str}
  llmPersonaPrompt: {prompt_str}
"""
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(content)
        write_meta(f"{path}.meta", guid)


def generate_resources():
    for key, data in RESOURCES.items():
        guid = data["guid"]
        path = os.path.join(RESOURCES_DIR, f"{key}.asset")
        en_name = make_yaml_str(data["en"])
        ar_name = make_yaml_str(data["ar"])
        c_en = make_yaml_str(data["collapse_en"])
        c_ar = make_yaml_str(data["collapse_ar"])
        icon_str = data["icon"]
        speaker_str = data["speaker"]

        content = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: 0}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {{fileID: 11500000, guid: 51a3714b3e4c4278ac399d60c86fa534, type: 3}}
  m_Name: {key}
  m_EditorClassIdentifier: 
  assetName: {key}
  displayNameLocalized:
    english: {en_name}
    arabic: {ar_name}
  icon: {icon_str}
  defaultStartingValue: 50
  collapseThreshold: 0
  warningThresholdPercent: 30
  warningCooldownCards: 5
  speaker: {speaker_str}
  collapseEndingFallbackEnglish: {c_en}
  collapseEndingFallbackArabic: {c_ar}
"""
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write(content)
        write_meta(f"{path}.meta", guid)


def generate_catalog():
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        "  m_Script: {fileID: 11500000, guid: dff4a16108ee42109dcd0205c4184641, type: 3}",
        "  m_Name: ResourceCatalog",
        "  m_EditorClassIdentifier: ",
        "  assetName: ResourceCatalog",
        "  resources:"
    ]
    for r_name in ["Crown", "Gold", "Army"]:
        r_guid = RESOURCES[r_name]["guid"]
        lines.append(f"  - {{fileID: 11400000, guid: {r_guid}, type: 2}}")

    with open(CATALOG_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    write_meta(f"{CATALOG_PATH}.meta", "6b2923c43cf2ce847a3ced381a116f0b")


def generate_database(card_guids):
    first_card_guid = card_guids[CARDS[0]["asset"]]

    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        "  m_Script: {fileID: 11500000, guid: 0c9ad9e3b8ad4b6da6e0a01a6f2ce002, type: 3}",
        "  m_Name: NarrativeDatabase",
        "  m_EditorClassIdentifier: ",
        "  assetName: NarrativeDatabase",
        f"  startingCard: {{fileID: 11400000, guid: {first_card_guid}, type: 2}}",
        "  resourceCatalog: {fileID: 11400000, guid: 6b2923c43cf2ce847a3ced381a116f0b, type: 2}",
        f"  promptTemplates: {{fileID: 11400000, guid: {PROMPT_TEMPLATES_GUID}, type: 2}}",
        "  cards:"
    ]
    for c in CARDS:
        cg = card_guids[c["asset"]]
        lines.append(f"  - {{fileID: 11400000, guid: {cg}, type: 2}}")

    lines.append("  speakers:")
    for skey, sdata in SPEAKERS.items():
        lines.append(f"  - {{fileID: 11400000, guid: {sdata['guid']}, type: 2}}")

    lines.append("  editorGraphPositions:")
    for c in CARDS:
        cg = card_guids[c["asset"]]
        col, row = GRAPH_LAYOUT.get(c["asset"], (0, 0))
        px = 400 + col * 260
        py = 200 + row * 220
        lines.append(f"  - card: {{fileID: 11400000, guid: {cg}, type: 2}}")
        lines.append(f"    position: {{x: {px}, y: {py}}}")

    lines.append("  editorSpeakerPositions:")
    for i, (skey, sdata) in enumerate(SPEAKERS.items()):
        lines.append(f"  - speaker: {{fileID: 11400000, guid: {sdata['guid']}, type: 2}}")
        lines.append(f"    position: {{x: 100, y: {300 + i * 140}}}")

    lines.append("  editorResourcePositions:")
    for i, (rkey, rdata) in enumerate(RESOURCES.items()):
        lines.append(f"  - resource: {{fileID: 11400000, guid: {rdata['guid']}, type: 2}}")
        lines.append(f"    position: {{x: 100, y: {1450 + i * 140}}}")

    with open(DATABASE_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lines) + "\n")
    write_meta(f"{DATABASE_PATH}.meta", DATABASE_GUID)


def generate_twee():
    header = "\n".join([
        ":: StoryTitle",
        "The Wicked King",
        "",
        ":: StoryData",
        "{",
        '  "ifid": "FB82F825-C7BD-40E9-974F-F22F2DD60404",',
        '  "format": "Harlowe",',
        '  "format-version": "3.3.9",',
        '  "start": "Card_01_MotherGhost",',
        '  "zoom": 0.3',
        "}",
        "",
        ':: Resources breakdown {"position":"100,100","size":"100,100"}',
        "Crown | Gold | Army  --  any at zero = realm collapses.",
        "",
        ':: Endings {"position":"100,240","size":"100,100"}',
        "6 authored endings (Two Brothers, Abdication, Tyrant, Shadow King, Warlord, Merchant) + 3 resource collapses.",
    ])

    personas = "\n\n".join([
        f':: Persona - {sdata["en"]} [Persona] {{"position":"100,{400 + i*140}","size":"100,100"}}\n{sdata["prompt"]}'
        for i, (skey, sdata) in enumerate(SPEAKERS.items())
    ])

    collapse = "\n\n".join([
        f':: Collapse - {rdata["en"]} [Collapse Fallback] {{"position":"100,{1550 + i*140}","size":"100,100"}}\n{rdata["collapse_en"]}'
        for i, (rkey, rdata) in enumerate(RESOURCES.items())
    ])

    def fmt_card(c):
        name = c["asset"]
        kind = c.get("kind", "choice")
        speaker = c.get("speaker")
        spk = SPEAKERS[speaker]["en"] if speaker else "None"
        col, row = GRAPH_LAYOUT.get(name, (0, 0))
        px = 400 + col * 260
        py = 200 + row * 220
        L = [f':: {name} {{"position":"{px},{py}","size":"100,100"}}']
        L.append(f"[Speaker: {spk}]")
        L.append(f"EN: {c['desc']['en']}")
        L.append(f"AR: {c['desc']['ar']}")
        L.append("")
        seed = c.get("seed", "")
        if kind == "reaction":
            L.append("[Reaction Card]")
            if seed: L.append(f"[Seed: {seed}]")
            L.append(f"[[Continue->{c['continue_next']}]]")
        elif kind == "chat":
            L.append("[Chat Card]")
            if seed: L.append(f"[Seed: {seed}]")
            L.append(f"[[End Audience->{c['continue_next']}]]")
        elif kind == "petition":
            L.append("[Petition Card]")
            if seed: L.append(f"[Seed: {seed}]")
            L.append(f"[[Resolve->{c['continue_next']}]]")
        elif kind == "ending":
            L.append("[Ending - Terminal]")
        elif kind == "evaluator":
            L.append("[Ending Evaluator]")
            L.append("<!-- Unity evaluates Crown / Gold / Army + story flags and routes to the matching ending below -->")
            for e in c["endings"]:
                L.append(f"[[{e}]]")
        elif kind == "verdict":
            lr = c.get("left_res", {})
            mr = c.get("middle_res", {})
            rr = c.get("right_res", {})
            L.append(f"[SWIPE LEFT] {c['left']['en']}")
            L.append(f"AR: {c['left']['ar']}")
            L.append(f"<!-- Crown {lr.get('Crown',0):+d}  Gold {lr.get('Gold',0):+d}  Army {lr.get('Army',0):+d} -->")
            L.append(f"[[{c['left_next']}]]")
            L.append("")
            L.append(f"[SWIPE MIDDLE] {c['middle']['en']}")
            L.append(f"AR: {c['middle']['ar']}")
            L.append(f"<!-- Crown {mr.get('Crown',0):+d}  Gold {mr.get('Gold',0):+d}  Army {mr.get('Army',0):+d} -->")
            L.append(f"[[{c['middle_next']}]]")
            L.append("")
            L.append(f"[SWIPE RIGHT] {c['right']['en']}")
            L.append(f"AR: {c['right']['ar']}")
            L.append(f"<!-- Crown {rr.get('Crown',0):+d}  Gold {rr.get('Gold',0):+d}  Army {rr.get('Army',0):+d} -->")
            L.append(f"[[{c['right_next']}]]")
        else:
            lr = c.get("left_res", {})
            rr = c.get("right_res", {})
            L.append(f"[SWIPE LEFT] {c['left']['en']}")
            L.append(f"AR: {c['left']['ar']}")
            L.append(f"<!-- Crown {lr.get('Crown',0):+d}  Gold {lr.get('Gold',0):+d}  Army {lr.get('Army',0):+d} -->")
            L.append(f"[[{c['left_next']}]]")
            L.append("")
            L.append(f"[SWIPE RIGHT] {c['right']['en']}")
            L.append(f"AR: {c['right']['ar']}")
            L.append(f"<!-- Crown {rr.get('Crown',0):+d}  Gold {rr.get('Gold',0):+d}  Army {rr.get('Army',0):+d} -->")
            L.append(f"[[{c['right_next']}]]")
        return "\n".join(L)

    cards_text = "\n\n".join([fmt_card(c) for c in CARDS])
    all_text = f"{header}\n\n{personas}\n\n{collapse}\n\n{cards_text}\n"

    with open(TWEE_PATH, "w", encoding="utf-8", newline="\n") as f:
        f.write(all_text)


def main():
    os.makedirs(CARDS_DIR, exist_ok=True)
    os.makedirs(SPEAKERS_DIR, exist_ok=True)
    os.makedirs(RESOURCES_DIR, exist_ok=True)

    # Load existing or create stable GUIDs for cards
    card_guids = {}
    for c in CARDS:
        name = c["asset"]
        meta_p = os.path.join(CARDS_DIR, f"{name}.asset.meta")
        if os.path.exists(meta_p):
            with open(meta_p, encoding="utf-8") as f:
                m = re.search(r"guid:\s*([a-f0-9]+)", f.read())
                if m:
                    card_guids[name] = m.group(1)
        if name not in card_guids:
            card_guids[name] = hashlib.md5(f"Weave_Card_{name}".encode("utf-8")).hexdigest()

    stale = [
        os.path.join(CARDS_DIR, "Card_19_BlightDecree.asset"),
        os.path.join(CARDS_DIR, "Card_19_BlightDecree.asset.meta"),
        os.path.join(RESOURCES_DIR, "Loyalty.asset"),
        os.path.join(RESOURCES_DIR, "Loyalty.asset.meta"),
    ]
    for path in stale:
        if os.path.exists(path):
            os.remove(path)
            print("Removed stale %s" % os.path.basename(path))

    print(f"Generating {len(CARDS)} cards...")
    for c in CARDS:
        generate_card_asset(c, card_guids)

    print(f"Generating {len(SPEAKERS)} speakers...")
    generate_speakers()

    print(f"Generating {len(RESOURCES)} resources...")
    generate_resources()

    print("Generating ResourceCatalog...")
    generate_catalog()

    print("Generating NarrativeDatabase...")
    generate_database(card_guids)

    print("Generating Twine file...")
    generate_twee()

    print(f"Successfully generated all assets! Total cards: {len(CARDS)}")

if __name__ == "__main__":
    main()
