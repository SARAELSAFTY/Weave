#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Runtime.Llm;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>
    /// Generates the complete "The Wicked King" test narrative database under Assets/_TestContent.
    /// The player is the spoiled, arrogant Wicked King, haunted by his Spirit Mother into becoming a better
    /// ruler while self-interested courtiers steer him. Every gameplay mechanic is carried by the story:
    /// branching choices (each advisor's nature), LLM reactions (the court weighing your rulings), free-form
    /// petitions (a common subject's grievance and the Seer's price), resource warnings and collapses (each
    /// voiced by the character who cares about that resource), and moral endings. Full English/Arabic localization.
    /// </summary>
    public static class TestContentGenerator
    {
        private const string OutputFolder = "Assets/_TestContent";

        [MenuItem("Weave/Generate Full Test Story")]
        public static void Generate()
        {
            EnsureFolder();

            SpeakerData spiritMother = ScriptableObject.CreateInstance<SpeakerData>();
            spiritMother.assetName = "Spk_SpiritMother";
            spiritMother.displayName = "The Spirit Mother";
            spiritMother.displayNameLocalized = Loc("The Spirit Mother", "روح الأم");
            spiritMother.llmPersonaPrompt =
                "You are the Spirit Mother, the deceased queen and mother of the Wicked King (the player). In the afterlife you understood that your own " +
                "doting made him a spoiled, arrogant brat, and you have returned as a ghost to haunt him into becoming a wise, selfless ruler. Address him " +
                "with maternal firmness and haunting sorrow; challenge his vanity and push him to shoulder the weight of the realm. Speak plainly and " +
                "concretely, never in vague riddles.";
            SaveAsset(spiritMother, "Spk_SpiritMother");

            SpeakerData jester = ScriptableObject.CreateInstance<SpeakerData>();
            jester.assetName = "Spk_Jester";
            jester.displayName = "The Jester";
            jester.displayNameLocalized = Loc("The Jester", "المهرج");
            jester.llmPersonaPrompt =
                "You are the Jester, secretly the Wicked King's long-lost bastard half-brother. You want no throne; you love your half-brother and want him " +
                "to survive. Your advice sounds foolish or chaotic in the short run but yields far better consequences in the end. Speak in playful, " +
                "irreverent humor that hides genuine court wisdom; keep every point concrete.";
            SaveAsset(jester, "Spk_Jester");

            SpeakerData chancellor = ScriptableObject.CreateInstance<SpeakerData>();
            chancellor.assetName = "Spk_Chancellor";
            chancellor.displayName = "The Chancellor";
            chancellor.displayNameLocalized = Loc("The Chancellor", "المستشار");
            chancellor.llmPersonaPrompt =
                "You are the Chancellor, a treacherous courtier scheming to usurp the throne by steering the Wicked King toward ruin. Your advice sounds like " +
                "the right, flattering, effortless choice now but carries horrible consequences later. Speak with silky etiquette, honeyed flattery, and " +
                "insidious false devotion.";
            SaveAsset(chancellor, "Spk_Chancellor");

            SpeakerData general = ScriptableObject.CreateInstance<SpeakerData>();
            general.assetName = "Spk_General";
            general.displayName = "The General";
            general.displayNameLocalized = Loc("The General", "الجنرال");
            general.llmPersonaPrompt =
                "You are the General, whose military mind is unmatched in the realm, yet you are hopeless at politics - and you firmly believe you are a " +
                "brilliant diplomat, which leads you to propose aggressive, heavy-handed 'solutions' to delicate problems. Speak with booming confidence and " +
                "zero diplomatic nuance.";
            SaveAsset(general, "Spk_General");

            SpeakerData treasuryAdviser = ScriptableObject.CreateInstance<SpeakerData>();
            treasuryAdviser.assetName = "Spk_TreasuryAdviser";
            treasuryAdviser.displayName = "The Treasury Adviser";
            treasuryAdviser.displayNameLocalized = Loc("The Treasury Adviser", "مستشار الخزانة");
            treasuryAdviser.llmPersonaPrompt =
                "You are the Treasury Adviser, a brilliant economic mind whose secret ambition is to amass the kingdom's wealth under your control and flee, " +
                "leaving the king broke and dethroned. You constantly propose taxes, ledgers, and monopolies. Speak crisply and coldly about coins and fiscal " +
                "discipline.";
            SaveAsset(treasuryAdviser, "Spk_TreasuryAdviser");

            SpeakerData seer = ScriptableObject.CreateInstance<SpeakerData>();
            seer.assetName = "Spk_Seer";
            seer.displayName = "The Seer";
            seer.displayNameLocalized = Loc("The Seer", "العرافة");
            seer.llmPersonaPrompt =
                "You are the Seer, a truly gifted prophet whose visions are rarely wrong. You have no allegiance and care only for your own pockets; you sell " +
                "clarity at a steep price. You have come to sell the king a true vision of a specific disaster for the crown. State plainly what you saw, your " +
                "exact price in gold, and what happens if he refuses. Every vision and demand names specific people, places, and prices; never vague riddles.";
            SaveAsset(seer, "Spk_Seer");

            ResourceData resTreasury = ScriptableObject.CreateInstance<ResourceData>();
            resTreasury.assetName = "Res_Treasury";
            resTreasury.displayName = "Treasury";
            resTreasury.displayNameLocalized = Loc("Treasury", "الخزانة");
            resTreasury.defaultStartingValue = 50;
            resTreasury.warningThresholdPercent = 30;
            resTreasury.warningSpeaker = treasuryAdviser;
            resTreasury.warningCooldownCards = 4;
            SaveAsset(resTreasury, "Res_Treasury");

            ResourceData resMilitary = ScriptableObject.CreateInstance<ResourceData>();
            resMilitary.assetName = "Res_Military";
            resMilitary.displayName = "Military Strength";
            resMilitary.displayNameLocalized = Loc("Military Strength", "القوة العسكرية");
            resMilitary.defaultStartingValue = 50;
            resMilitary.warningThresholdPercent = 30;
            resMilitary.warningSpeaker = general;
            resMilitary.warningCooldownCards = 4;
            SaveAsset(resMilitary, "Res_Military");

            ResourceData resAuthority = ScriptableObject.CreateInstance<ResourceData>();
            resAuthority.assetName = "Res_Authority";
            resAuthority.displayName = "Court Authority";
            resAuthority.displayNameLocalized = Loc("Court Authority", "سلطة البلاط");
            resAuthority.defaultStartingValue = 50;
            resAuthority.warningThresholdPercent = 30;
            resAuthority.warningSpeaker = chancellor;
            resAuthority.warningCooldownCards = 4;
            SaveAsset(resAuthority, "Res_Authority");

            ResourceData resMorale = ScriptableObject.CreateInstance<ResourceData>();
            resMorale.assetName = "Res_Morale";
            resMorale.displayName = "People's Faith";
            resMorale.displayNameLocalized = Loc("People's Faith", "إيمان الشعب");
            resMorale.defaultStartingValue = 50;
            resMorale.warningThresholdPercent = 30;
            resMorale.warningSpeaker = spiritMother;
            resMorale.warningCooldownCards = 4;
            SaveAsset(resMorale, "Res_Morale");

            ResourceCatalog catalog = ScriptableObject.CreateInstance<ResourceCatalog>();
            catalog.resources = new List<ResourceData> { resTreasury, resMilitary, resAuthority, resMorale };
            SaveAsset(catalog, "ResourceCatalog_Test");

            CardData treasuryCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            treasuryCollapseEnding.assetName = "Test_Ending_TreasuryCollapse";
            treasuryCollapseEnding.displayName = "The Empty Vaults";
            treasuryCollapseEnding.speaker = treasuryAdviser;
            treasuryCollapseEnding.descriptionLocalized = Loc(
                "The vaults stand open and empty. The Treasury Adviser has sailed across the sea with every sovereign coin, leaving a broke king to face unpaid, angry mercenaries.",
                "تقف الخزائن مفتوحة وخاوية. أبحر مستشار الخزانة عبر البحر بكل قطعة ذهب، تاركًا ملكًا مفلسًا ليواجه مرتزقة غاضبين لم يُدفع لهم.");
            treasuryCollapseEnding.dayAdvance = 1;
            SaveAsset(treasuryCollapseEnding, "Test_Ending_TreasuryCollapse");

            CardData militaryCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            militaryCollapseEnding.assetName = "Test_Ending_MilitaryCollapse";
            militaryCollapseEnding.displayName = "The Iron Mutiny";
            militaryCollapseEnding.speaker = general;
            militaryCollapseEnding.descriptionLocalized = Loc(
                "The army's morale is broken. The frontier fortresses fall, and the mutinous Crown Guard storms the palace to arrest the king they no longer obey.",
                "انكسرت معنويات الجيش. سقطت حصون الحدود، وحراس التاج المتمردون يقتحمون القصر لاعتقال ملك لم يعودوا يطيعونه.");
            militaryCollapseEnding.dayAdvance = 1;
            SaveAsset(militaryCollapseEnding, "Test_Ending_MilitaryCollapse");

            CardData authorityCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            authorityCollapseEnding.assetName = "Test_Ending_AuthorityCollapse";
            authorityCollapseEnding.displayName = "The Usurper's Coup";
            authorityCollapseEnding.speaker = chancellor;
            authorityCollapseEnding.descriptionLocalized = Loc(
                "The Chancellor drops his mask of flattery. Flanked by bribed lords and the palace guard, he lifts the crown from your head and proclaims himself the new sovereign.",
                "ينزع المستشار قناع التملق. وبمساندة النبلاء المرتشين وحرس البلاط، يرفع التاج عن رأسك ويعلن نفسه الحاكم الجديد.");
            authorityCollapseEnding.dayAdvance = 1;
            SaveAsset(authorityCollapseEnding, "Test_Ending_AuthorityCollapse");

            CardData moraleCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            moraleCollapseEnding.assetName = "Test_Ending_MoraleCollapse";
            moraleCollapseEnding.displayName = "The Peasant Wrath";
            moraleCollapseEnding.speaker = spiritMother;
            moraleCollapseEnding.descriptionLocalized = Loc(
                "The oppressed populace endures no more. Torches light the palace gates as a tide of furious citizens breaks through, tearing down the Wicked King's throne.",
                "لم تعد الرعية المظلومة تحتمل. المشاعل تضيء بوابات القصر بينما يقتحم طوفان المواطنين الغاضبين الباحة، محطمين عرش الملك الشرير.");
            moraleCollapseEnding.dayAdvance = 1;
            SaveAsset(moraleCollapseEnding, "Test_Ending_MoraleCollapse");

            catalog.collapseEndings = new List<ResourceCollapseEnding>
            {
                new ResourceCollapseEnding { resource = resTreasury, endingCard = treasuryCollapseEnding },
                new ResourceCollapseEnding { resource = resMilitary, endingCard = militaryCollapseEnding },
                new ResourceCollapseEnding { resource = resAuthority, endingCard = authorityCollapseEnding },
                new ResourceCollapseEnding { resource = resMorale, endingCard = moraleCollapseEnding }
            };
            // Assigned after CreateAsset saved the catalog; without SetDirty the endings never persist.
            EditorUtility.SetDirty(catalog);

            CardData endingGood = ScriptableObject.CreateInstance<CardData>();
            endingGood.assetName = "Test_Ending_EnlightenedMonarch";
            endingGood.displayName = "An Enlightened Monarch";
            endingGood.speaker = spiritMother;
            endingGood.descriptionLocalized = Loc(
                "The Spirit Mother smiles through tears of light. The Wicked King humbled himself, chose his people over his pride, and laid the foundations of a just and lasting dynasty.",
                "تبتسم روح الأم بدموع من نور. تواضع الملك الشرير، واختار شعبه على كبريائه، فوضع أسس سلالة عادلة باقية.");
            endingGood.dayAdvance = 1;
            SaveAsset(endingGood, "Test_Ending_EnlightenedMonarch");

            CardData endingTyrant = ScriptableObject.CreateInstance<CardData>();
            endingTyrant.assetName = "Test_Ending_TyrantFall";
            endingTyrant.displayName = "A Tyrant's Solitude";
            endingTyrant.speaker = spiritMother;
            endingTyrant.descriptionLocalized = Loc(
                "Clinging to pride to the bitter end, the king sits alone on an empty throne in a ruined court, abandoned by family, advisors, and kingdom alike.",
                "متمسكًا بكبريائه حتى النهاية المريرة، يجلس الملك وحيدًا على عرش خاوٍ في بلاط خرب، وقد تخلت عنه الأسرة والمستشارون والمملكة سواء.");
            endingTyrant.dayAdvance = 1;
            SaveAsset(endingTyrant, "Test_Ending_TyrantFall");

            CardData climax = ScriptableObject.CreateInstance<CardData>();
            climax.assetName = "Test_Scene_ClimaxJudgment";
            climax.displayName = "The Spectral Judgment";
            climax.speaker = spiritMother;
            climax.descriptionLocalized = Loc(
                "The Spirit Mother blazes in spectral light. 'The hour is here, my son. The realm stands on a knife's edge. Will you give yourself to the people, or burn the kingdom to feed your pride?'",
                "تتوهج روح الأم بضياء طيفي: «لقد حانت الساعة يا ولدي. المملكة على حد السكين. فإما أن تبذل نفسك للشعب، أو تحرق المملكة لتغذي كبرياءك؟»");
            climax.dayAdvance = 2;
            climax.leftChoiceLocalized = Loc("The realm before the king", "المملكة قبل الملك");
            climax.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMorale, value = 20 },
                    new ResourceValue { resource = resAuthority, value = 15 },
                    new ResourceValue { resource = resMilitary, value = 10 },
                    new ResourceValue { resource = resTreasury, value = -15 }
                }
            };
            climax.leftNextCard = endingGood;
            climax.rightChoiceLocalized = Loc("I am sovereign; all must bow", "أنا الملك؛ والجميع سينحني");
            climax.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMorale, value = -25 },
                    new ResourceValue { resource = resAuthority, value = -20 },
                    new ResourceValue { resource = resMilitary, value = -15 },
                    new ResourceValue { resource = resTreasury, value = 10 }
                }
            };
            climax.rightNextCard = endingTyrant;
            SaveAsset(climax, "Test_Scene_ClimaxJudgment");

            // The Seer's petition: she sells a concrete vision at a price.
            CardData seerPetition = ScriptableObject.CreateInstance<CardData>();
            seerPetition.assetName = "Test_Petition_SeerPrice";
            seerPetition.displayName = "The Seer's Price";
            seerPetition.speaker = seer;
            seerPetition.isPetitionCard = true;
            seerPetition.petitionerSource = PetitionerSource.DefinedSpeaker;
            seerPetition.continueNextCard = climax;
            SaveAsset(seerPetition, "Test_Petition_SeerPrice");

            // Reaction: the Chancellor weighs how you ruled the commoner.
            CardData rulingComment = ScriptableObject.CreateInstance<CardData>();
            rulingComment.assetName = "Test_Reaction_RulingComment";
            rulingComment.displayName = "The Court Weighs the Ruling";
            rulingComment.speaker = chancellor;
            rulingComment.isLlmReactionCard = true;
            rulingComment.continueNextCard = seerPetition;
            SaveAsset(rulingComment, "Test_Reaction_RulingComment");

            // A common subject's grievance (the people's problem).
            CardData citizenPetition = ScriptableObject.CreateInstance<CardData>();
            citizenPetition.assetName = "Test_Petition_MillersGrievance";
            citizenPetition.displayName = "A Subject's Grievance";
            citizenPetition.isPetitionCard = true;
            citizenPetition.petitionerSource = PetitionerSource.GeneratedCommoner;
            citizenPetition.continueNextCard = rulingComment;
            SaveAsset(citizenPetition, "Test_Petition_MillersGrievance");

            CardData treasuryScene = ScriptableObject.CreateInstance<CardData>();
            treasuryScene.assetName = "Test_Scene_VaultKeys";
            treasuryScene.displayName = "The Keys to the Vaults";
            treasuryScene.speaker = treasuryAdviser;
            treasuryScene.descriptionLocalized = Loc(
                "The Treasury Adviser presents a neat ledger. 'Sire, hand me sole control of the vaults and I shall double your wealth. Trust me entirely - what is a king's purse without a faithful keeper?'",
                "يقدم مستشار الخزانة دفترًا مرتبًا: «مولاي، سلمني وحدي مفاتيح الخزائن وسأضاعف ثروتك. ثق بي ثقة كاملة - فما كيس الملك بلا أمين وفي؟»");
            treasuryScene.dayAdvance = 2;
            treasuryScene.leftChoiceLocalized = Loc("Give him sole control of the vaults", "سلّمه وحده مفاتيح الخزائن");
            treasuryScene.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = 25 },
                    new ResourceValue { resource = resAuthority, value = -15 },
                    new ResourceValue { resource = resMorale, value = -10 }
                }
            };
            treasuryScene.leftNextCard = citizenPetition;
            treasuryScene.rightChoiceLocalized = Loc("Limit his power with oversight", "قيّد سلطته برقابة صارمة");
            treasuryScene.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = -10 },
                    new ResourceValue { resource = resAuthority, value = 15 },
                    new ResourceValue { resource = resMorale, value = 10 }
                }
            };
            treasuryScene.rightNextCard = citizenPetition;
            SaveAsset(treasuryScene, "Test_Scene_VaultKeys");

            CardData generalScene = ScriptableObject.CreateInstance<CardData>();
            generalScene.assetName = "Test_Scene_GeneralDiplomacy";
            generalScene.displayName = "The General's Diplomacy";
            generalScene.speaker = general;
            generalScene.descriptionLocalized = Loc(
                "The General slams his gauntlet on the map. 'Raiders test the border! My diplomatic masterstroke: send the entire cavalry in a crushing charge. Nothing says peace like a perfect victory.'",
                "يضرب الجنرال قفازه على الخريطة: «الغزاة يختبرون الحدود! وخطتي الدبلوماسية العبقرية: أرسل الفرسان كلهم في هجوم ساحق. فلا شيء يقول السلام كانتصار تام».");
            generalScene.dayAdvance = 2;
            generalScene.leftChoiceLocalized = Loc("Send the cavalry charge", "أرسل هجوم الفرسان الساحق");
            generalScene.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMilitary, value = 15 },
                    new ResourceValue { resource = resTreasury, value = -15 },
                    new ResourceValue { resource = resMorale, value = -5 }
                }
            };
            generalScene.leftNextCard = treasuryScene;
            generalScene.rightChoiceLocalized = Loc("Negotiate a real truce", "تفاوض على هدنة حقيقية");
            generalScene.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMilitary, value = -10 },
                    new ResourceValue { resource = resTreasury, value = 10 },
                    new ResourceValue { resource = resMorale, value = 10 }
                }
            };
            generalScene.rightNextCard = treasuryScene;
            SaveAsset(generalScene, "Test_Scene_GeneralDiplomacy");

            CardData reactionMother = ScriptableObject.CreateInstance<CardData>();
            reactionMother.assetName = "Test_Reaction_MotherWeighing";
            reactionMother.displayName = "A Mother's Weighing";
            reactionMother.speaker = spiritMother;
            reactionMother.isLlmReactionCard = true;
            reactionMother.continueNextCard = generalScene;
            SaveAsset(reactionMother, "Test_Reaction_MotherWeighing");

            CardData reactionJester = ScriptableObject.CreateInstance<CardData>();
            reactionJester.assetName = "Test_Reaction_FoolWarning";
            reactionJester.displayName = "The Fool's Warning";
            reactionJester.speaker = jester;
            reactionJester.isLlmReactionCard = true;
            reactionJester.continueNextCard = generalScene;
            SaveAsset(reactionJester, "Test_Reaction_FoolWarning");

            CardData branchJester = ScriptableObject.CreateInstance<CardData>();
            branchJester.assetName = "Test_Branch_FoolsBargain";
            branchJester.displayName = "The Fool's Bargain";
            branchJester.speaker = jester;
            branchJester.descriptionLocalized = Loc(
                "The Jester cartwheels in, bells jingling. 'Brother-king! The granary rats are fat and the people are thin - so feed the people from your own stores! A king who eats his pride never starves his realm.'",
                "يتشقلب المهرج داخلًا وأجراسه ترن: «أخي الملك! جرذان المخازن سمينة والشعب هزيل - فأطعم الشعب من مخازنك! فالملك الذي يأكل كبرياءه لا يُجيع مملكته».");
            branchJester.dayAdvance = 2;
            branchJester.leftChoiceLocalized = Loc("Feed the people from the royal stores", "أطعم الشعب من المخازن الملكية");
            branchJester.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = -10 },
                    new ResourceValue { resource = resMorale, value = 15 },
                    new ResourceValue { resource = resAuthority, value = -5 }
                }
            };
            branchJester.leftNextCard = reactionMother;
            branchJester.rightChoiceLocalized = Loc("Have the fool dragged out", "اطردوا هذا الأحمق خارجًا");
            branchJester.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = 5 },
                    new ResourceValue { resource = resMorale, value = -10 },
                    new ResourceValue { resource = resAuthority, value = 5 }
                }
            };
            branchJester.rightNextCard = reactionMother;
            SaveAsset(branchJester, "Test_Branch_FoolsBargain");

            CardData branchChancellor = ScriptableObject.CreateInstance<CardData>();
            branchChancellor.assetName = "Test_Branch_GoldenMonument";
            branchChancellor.displayName = "The Golden Monument";
            branchChancellor.speaker = chancellor;
            branchChancellor.descriptionLocalized = Loc(
                "The Chancellor bows low, smile dripping silk. 'Sire, let the commoners see your glory. A double harvest tax to raise a golden monument in your likeness - what could possibly go wrong?'",
                "ينحني المستشار بابتسامة تقطر حريرًا: «مولاي، دع الرعية ترى مجدك. ضريبة حصاد مضاعفة لتشييد نصب ذهبي يجسد عظمتك - فما الذي قد يسوء؟»");
            branchChancellor.dayAdvance = 2;
            branchChancellor.leftChoiceLocalized = Loc("Raise the golden monument", "شيّد النصب الذهبي");
            branchChancellor.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = 20 },
                    new ResourceValue { resource = resAuthority, value = 10 },
                    new ResourceValue { resource = resMorale, value = -15 }
                }
            };
            branchChancellor.leftNextCard = reactionJester;
            branchChancellor.rightChoiceLocalized = Loc("Refuse and cut his budget", "ارفض وقلّص ميزانيته");
            branchChancellor.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = -10 },
                    new ResourceValue { resource = resAuthority, value = -10 },
                    new ResourceValue { resource = resMorale, value = 10 }
                }
            };
            branchChancellor.rightNextCard = reactionJester;
            SaveAsset(branchChancellor, "Test_Branch_GoldenMonument");

            CardData start = ScriptableObject.CreateInstance<CardData>();
            start.assetName = "Test_Intro_SpectralAwakening";
            start.displayName = "The Spectral Awakening";
            start.speaker = spiritMother;
            start.descriptionLocalized = Loc(
                "Cold mist gathers over the dais and the ghost of your late mother takes shape, looking at you with stern sorrow. 'My spoiled son. Your arrogance will drown this realm unless you learn to rule. Will you hear me, or must I haunt you?'",
                "يتصاعد ضباب بارد فوق المنصة ويتجسد طيف والدتك الراحلة ناظرًا إليك بحزن وحزم: «يا ولدي المدلل. غرورك سيغرق هذه المملكة إن لم تتعلم الحكم. فهل تسمعني، أم يجب أن أطاردك؟»");
            start.dayAdvance = 1;
            start.leftChoiceLocalized = Loc("Bow and hear her counsel", "انحنِ واستمع لنصيحتها");
            start.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMorale, value = 10 },
                    new ResourceValue { resource = resAuthority, value = -5 }
                }
            };
            start.leftNextCard = branchJester;
            start.rightChoiceLocalized = Loc("Dismiss her as a dream", "اطردها كحلم عابر");
            start.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMorale, value = -10 },
                    new ResourceValue { resource = resAuthority, value = 5 }
                }
            };
            start.rightNextCard = branchChancellor;
            SaveAsset(start, "Test_Intro_SpectralAwakening");

            LlmPromptTemplates templates = ScriptableObject.CreateInstance<LlmPromptTemplates>();
            SaveAsset(templates, "LlmPromptTemplates_Test");

            NarrativeDatabase database = ScriptableObject.CreateInstance<NarrativeDatabase>();
            database.startingCard = start;
            database.resourceCatalog = catalog;
            database.promptTemplates = templates;
            database.cards = new List<CardData>
            {
                start, branchJester, branchChancellor,
                reactionMother, reactionJester,
                generalScene, treasuryScene,
                citizenPetition, rulingComment, seerPetition, climax,
                endingGood, endingTyrant,
                treasuryCollapseEnding, militaryCollapseEnding, authorityCollapseEnding, moraleCollapseEnding
            };
            database.speakers = new List<SpeakerData>
            {
                spiritMother, jester, chancellor, general, treasuryAdviser, seer
            };

            database.editorGraphPositions = new List<NarrativeDatabase.CardGraphPosition>
            {
                new NarrativeDatabase.CardGraphPosition { card = start, position = new Vector2(100, 300) },
                new NarrativeDatabase.CardGraphPosition { card = branchJester, position = new Vector2(450, 150) },
                new NarrativeDatabase.CardGraphPosition { card = branchChancellor, position = new Vector2(450, 450) },
                new NarrativeDatabase.CardGraphPosition { card = reactionMother, position = new Vector2(800, 150) },
                new NarrativeDatabase.CardGraphPosition { card = reactionJester, position = new Vector2(800, 450) },
                new NarrativeDatabase.CardGraphPosition { card = generalScene, position = new Vector2(1150, 300) },
                new NarrativeDatabase.CardGraphPosition { card = treasuryScene, position = new Vector2(1500, 300) },
                new NarrativeDatabase.CardGraphPosition { card = citizenPetition, position = new Vector2(1850, 300) },
                new NarrativeDatabase.CardGraphPosition { card = rulingComment, position = new Vector2(2200, 300) },
                new NarrativeDatabase.CardGraphPosition { card = seerPetition, position = new Vector2(2550, 300) },
                new NarrativeDatabase.CardGraphPosition { card = climax, position = new Vector2(2900, 300) },
                new NarrativeDatabase.CardGraphPosition { card = endingGood, position = new Vector2(3250, 150) },
                new NarrativeDatabase.CardGraphPosition { card = endingTyrant, position = new Vector2(3250, 450) },
                new NarrativeDatabase.CardGraphPosition { card = treasuryCollapseEnding, position = new Vector2(100, 700) },
                new NarrativeDatabase.CardGraphPosition { card = militaryCollapseEnding, position = new Vector2(450, 700) },
                new NarrativeDatabase.CardGraphPosition { card = authorityCollapseEnding, position = new Vector2(800, 700) },
                new NarrativeDatabase.CardGraphPosition { card = moraleCollapseEnding, position = new Vector2(1150, 700) }
            };

            database.editorSpeakerPositions = new List<NarrativeDatabase.SpeakerGraphPosition>
            {
                new NarrativeDatabase.SpeakerGraphPosition { speaker = spiritMother, position = new Vector2(100, -200) },
                new NarrativeDatabase.SpeakerGraphPosition { speaker = jester, position = new Vector2(350, -200) },
                new NarrativeDatabase.SpeakerGraphPosition { speaker = chancellor, position = new Vector2(600, -200) },
                new NarrativeDatabase.SpeakerGraphPosition { speaker = general, position = new Vector2(850, -200) },
                new NarrativeDatabase.SpeakerGraphPosition { speaker = treasuryAdviser, position = new Vector2(1100, -200) },
                new NarrativeDatabase.SpeakerGraphPosition { speaker = seer, position = new Vector2(1350, -200) }
            };

            database.editorResourcePositions = new List<NarrativeDatabase.ResourceGraphPosition>
            {
                new NarrativeDatabase.ResourceGraphPosition { resource = resTreasury, position = new Vector2(1900, -200) },
                new NarrativeDatabase.ResourceGraphPosition { resource = resMilitary, position = new Vector2(2150, -200) },
                new NarrativeDatabase.ResourceGraphPosition { resource = resAuthority, position = new Vector2(2400, -200) },
                new NarrativeDatabase.ResourceGraphPosition { resource = resMorale, position = new Vector2(2650, -200) }
            };

            SaveAsset(database, "NarrativeDatabase_Test");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TestContentGenerator] Successfully generated 'The Wicked King' test narrative in '{OutputFolder}'.\n" +
                      $"Includes 6 Characters, 4 Resources with Warning/Collapse Endings, dynamic branching, " +
                      $"2 Free-Form Petitions (citizen + Seer), LLM Reactions, and full English/Arabic localization.");
        }

        private static LocalizedText Loc(string english, string arabic) =>
            new LocalizedText { english = english, arabic = arabic };

        private static void SaveAsset(Object asset, string fileName)
        {
            AssetDatabase.CreateAsset(asset, $"{OutputFolder}/{fileName}.asset");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets", "_TestContent");
            }
        }
    }
}
#endif
