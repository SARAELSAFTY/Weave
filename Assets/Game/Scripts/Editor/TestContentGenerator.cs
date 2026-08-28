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
    /// Generates the complete "Wicked King & The Royal Court" test narrative database under Assets/_TestContent.
    /// Exercises all gameplay systems: branching choices, LLM reactions, free-form petitions, resource warnings,
    /// and resource collapses with full English and Arabic localization.
    /// </summary>
    public static class TestContentGenerator
    {
        private const string OutputFolder = "Assets/_TestContent";

        [MenuItem("Weave/Generate Full Test Story")]
        public static void Generate()
        {
            EnsureFolder();

            // ==========================================
            // 1. CHARACTERS / SPEAKERS
            // ==========================================
            SpeakerData spiritMother = ScriptableObject.CreateInstance<SpeakerData>();
            spiritMother.assetName = "Spk_SpiritMother";
            spiritMother.displayName = "The Spirit Mother";
            spiritMother.displayNameLocalized = Loc("The Spirit Mother", "روح الأم");
            spiritMother.llmPersonaPrompt =
                "You are the Spirit Mother, the deceased queen and loving mother of the Wicked King (the player). " +
                "You realized in the afterlife that your excessive doting caused your son to become a spoiled, arrogant brat. " +
                "You have returned as a ghost to haunt him into becoming a wise, selfless sovereign. Address him with maternal firmness, " +
                "haunting sorrow, and relentless expectation. Challenge his vanity and push him to shoulder the true weight of the realm.";
            SaveAsset(spiritMother, "Spk_SpiritMother");

            SpeakerData jester = ScriptableObject.CreateInstance<SpeakerData>();
            jester.assetName = "Spk_Jester";
            jester.displayName = "The Jester";
            jester.displayNameLocalized = Loc("The Jester", "المهرج");
            jester.llmPersonaPrompt =
                "You are the Jester, secretly the Wicked King's long-lost bastard half-brother. You hold zero desire for the throne, " +
                "but you love your half-brother and want to help him survive. Your advice sounds chaotic, ridiculous, or foolish in the short run, " +
                "but yields far better strategic consequences in the end. Speak in playful riddles, irreverent humor, and veiled court wisdom.";
            SaveAsset(jester, "Spk_Jester");

            SpeakerData chancellor = ScriptableObject.CreateInstance<SpeakerData>();
            chancellor.assetName = "Spk_Chancellor";
            chancellor.displayName = "Chancellor Malakor";
            chancellor.displayNameLocalized = Loc("Chancellor Malakor", "المستشار مالاكور");
            chancellor.llmPersonaPrompt =
                "You are Chancellor Malakor, a treacherous courtier scheming to usurp the throne by steering the Wicked King toward ruin. " +
                "Your advice sounds flattering, gratifying, and effortless at first, but carries devastating consequences down the line. " +
                "Speak with silky court etiquette, honeyed flattery, and insidious false devotion.";
            SaveAsset(chancellor, "Spk_Chancellor");

            SpeakerData general = ScriptableObject.CreateInstance<SpeakerData>();
            general.assetName = "Spk_General";
            general.displayName = "General Valerius";
            general.displayNameLocalized = Loc("General Valerius", "الجنرال فاليريوس");
            general.llmPersonaPrompt =
                "You are General Valerius, supreme commander of the crown's armies. Your tactical military genius is unmatched across the realm, " +
                "yet you are completely incompetent at subtle politics. Crucially, you firmly believe you are a brilliant diplomat, which leads you to " +
                "propose aggressive, heavy-handed solutions to delicate political dilemmas. Speak with booming military confidence and zero diplomatic nuance.";
            SaveAsset(general, "Spk_General");

            SpeakerData treasuryAdviser = ScriptableObject.CreateInstance<SpeakerData>();
            treasuryAdviser.assetName = "Spk_TreasuryAdviser";
            treasuryAdviser.displayName = "Treasury Adviser Midas";
            treasuryAdviser.displayNameLocalized = Loc("Treasury Adviser Midas", "مستشار الخزانة ميداس");
            treasuryAdviser.llmPersonaPrompt =
                "You are Midas, the High Treasury Adviser. You have a brilliant economic mind, but your secret ambition is to amass the kingdom's wealth " +
                "under your direct control and flee across the sea, leaving the crown bankrupt. You constantly propose taxes, ledgers, and monopolies. " +
                "Speak crisply and coldly about coins, ledgers, and fiscal discipline.";
            SaveAsset(treasuryAdviser, "Spk_TreasuryAdviser");

            SpeakerData seer = ScriptableObject.CreateInstance<SpeakerData>();
            seer.assetName = "Spk_Seer";
            seer.displayName = "The Seer Cassandra";
            seer.displayNameLocalized = Loc("The Seer Cassandra", "العرافة كاساندرا");
            seer.llmPersonaPrompt =
                "You are Cassandra the Seer, an authentic prophet whose visions are unerringly accurate. You have zero loyalty to king or realm and care " +
                "exclusively for gold and personal luxury. You only grant clarity to those who pay your steep price. Speak in enigmatic, chillingly accurate prophecies " +
                "wrapped in mercantile demands.";
            SaveAsset(seer, "Spk_Seer");

            // ==========================================
            // 2. KINGDOM RESOURCES
            // ==========================================
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

            // ==========================================
            // 3. COLLAPSE ENDING CARDS (LEAF LEVEL)
            // ==========================================
            CardData treasuryCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            treasuryCollapseEnding.assetName = "Test_Ending_TreasuryCollapse";
            treasuryCollapseEnding.displayName = "The Empty Vaults";
            treasuryCollapseEnding.speaker = treasuryAdviser;
            treasuryCollapseEnding.descriptionLocalized = Loc(
                "The royal vaults stand open and empty. Adviser Midas has fled across the sea with every sovereign coin, leaving the broke King to face the wrath of unpaid mercenary legions.",
                "أبواب الخزائن الملكية مفتوحة على مصراعيها وخاوية تمامًا. هرب المستشار ميداس عبر البحر بكل ذهب المملكة، تاركًا الملك المفلس ليواجه بطش جحافل المرتزقة الغاضبين.");
            treasuryCollapseEnding.dayAdvance = 1;
            SaveAsset(treasuryCollapseEnding, "Test_Ending_TreasuryCollapse");

            CardData militaryCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            militaryCollapseEnding.assetName = "Test_Ending_MilitaryCollapse";
            militaryCollapseEnding.displayName = "The Iron Mutiny";
            militaryCollapseEnding.speaker = general;
            militaryCollapseEnding.descriptionLocalized = Loc(
                "General Valerius's reckless campaigns broke the army's morale. The frontier fortresses have fallen, and the mutinous Crown Guard storms the palace to arrest the King.",
                "أدت حملات الجنرال فاليريوس المتهورة إلى تحطيم معنويات الجيش. سقطت حصون الحدود، وحراس التاج المتمردون يقتحمون القصر لاعتقال الملك.");
            militaryCollapseEnding.dayAdvance = 1;
            SaveAsset(militaryCollapseEnding, "Test_Ending_MilitaryCollapse");

            CardData authorityCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            authorityCollapseEnding.assetName = "Test_Ending_AuthorityCollapse";
            authorityCollapseEnding.displayName = "The Usurper's Coup";
            authorityCollapseEnding.speaker = chancellor;
            authorityCollapseEnding.descriptionLocalized = Loc(
                "Chancellor Malakor drops his mask of flattery. Flanked by bribed lords and the palace guard, he strips the crown from your head and proclaims himself the new Sovereign.",
                "نزع المستشار مالاكور قناع التملق أخيرًا. وبمساندة النبلاء المرتشين وحرس البلاط، ينتزع التاج من رأسك ويعلن نفسه الحاكم الجديد للبلاد.");
            authorityCollapseEnding.dayAdvance = 1;
            SaveAsset(authorityCollapseEnding, "Test_Ending_AuthorityCollapse");

            CardData moraleCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            moraleCollapseEnding.assetName = "Test_Ending_MoraleCollapse";
            moraleCollapseEnding.displayName = "The Peasant Wrath";
            moraleCollapseEnding.speaker = spiritMother;
            moraleCollapseEnding.descriptionLocalized = Loc(
                "The oppressed populace can endure no more. Torches illuminate the palace gates as a tidal wave of furious citizens breaks through the courtyard, tearing down the throne of the Wicked King.",
                "لم تعد الرعية قادرة على تحمل المزيد من الظلم. المشاعل تضيء بوابات القصر بينما يقتحم طوفان المواطنين الغاضبين الباحة الملكية، محطمين عرش الملك الشرير.");
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

            // ==========================================
            // 4. STORYLINE ENDING CARDS
            // ==========================================
            CardData endingVictory = ScriptableObject.CreateInstance<CardData>();
            endingVictory.assetName = "Test_Ending_EnlightenedMonarch";
            endingVictory.displayName = "An Enlightened Monarch";
            endingVictory.speaker = spiritMother;
            endingVictory.descriptionLocalized = Loc(
                "The Spirit Mother smiles with tears of ethereal joy. The Wicked King humbled himself, chose his people over his ego, and laid the foundations of a just and enduring dynasty.",
                "تبتسم روح الأم بدموع الفرح الطيفي. تواضع الملك الشرير، واختار شعبه على أنانيته، واضعًا أسس سلالة ملكية عادلة ومجيدة تدوم عبر الأجيال.");
            endingVictory.dayAdvance = 1;
            SaveAsset(endingVictory, "Test_Ending_EnlightenedMonarch");

            CardData endingTyrant = ScriptableObject.CreateInstance<CardData>();
            endingTyrant.assetName = "Test_Ending_TyrantFall";
            endingTyrant.displayName = "A Tyrant's Solitude";
            endingTyrant.speaker = spiritMother;
            endingTyrant.descriptionLocalized = Loc(
                "Clinging to arrogance to the bitter end, the King sits alone upon an empty throne in a ruined court, abandoned by his family, his advisors, and his kingdom.",
                "متمسكًا بغروره حتى النهاية المريرة، يجلس الملك وحيدًا على عرش خاوٍ في بلاط متهالك، بعد أن تخلى عنه أهله ومستشاروه ومملكته بأسرها.");
            endingTyrant.dayAdvance = 1;
            SaveAsset(endingTyrant, "Test_Ending_TyrantFall");

            // ==========================================
            // 5. CLIMAX & POST-PETITION SCENES (LEAF-TO-ROOT)
            // ==========================================
            CardData climaxTrial = ScriptableObject.CreateInstance<CardData>();
            climaxTrial.assetName = "Test_Scene_ClimaxCrownTrial";
            climaxTrial.displayName = "The Spectral Judgment";
            climaxTrial.speaker = spiritMother;
            climaxTrial.descriptionLocalized = Loc(
                "The Spirit Mother materializes amidst thunderous spectral light. 'The hour has come, my son. The realm stands upon the precipice. Will you sacrifice your selfish pride for the people, or burn this kingdom to feed your vanity?'",
                "تتجسد روح الأم وسط ضياء طيفي مدوٍّ: 'لقد حانت اللحظة يا ولدي. المملكة تقف على حافة الهاوية. هل تضحي بكبريائك الأناني من أجل شعبك، أم تحرق المملكة لتغذي غرورك؟'");
            climaxTrial.dayAdvance = 2;
            climaxTrial.leftChoiceLocalized = Loc("The realm comes before the King!", "المملكة تأتي قبل الملك!");
            climaxTrial.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMorale, value = 20 },
                    new ResourceValue { resource = resAuthority, value = 15 },
                    new ResourceValue { resource = resMilitary, value = 10 },
                    new ResourceValue { resource = resTreasury, value = -15 }
                }
            };
            climaxTrial.leftNextCard = endingVictory;
            climaxTrial.rightChoiceLocalized = Loc("I am Sovereign! All must bow!", "أنا الملك الأوحد! والجميع سينحني!");
            climaxTrial.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMorale, value = -25 },
                    new ResourceValue { resource = resAuthority, value = -20 },
                    new ResourceValue { resource = resMilitary, value = -15 },
                    new ResourceValue { resource = resTreasury, value = 10 }
                }
            };
            climaxTrial.rightNextCard = endingTyrant;
            SaveAsset(climaxTrial, "Test_Scene_ClimaxCrownTrial");

            // Post-Petition LLM Reaction Card
            CardData postPetitionJester = ScriptableObject.CreateInstance<CardData>();
            postPetitionJester.assetName = "Test_Reaction_JesterReflection";
            postPetitionJester.displayName = "A Brother's Warning";
            postPetitionJester.speaker = jester;
            postPetitionJester.isLlmReactionCard = true;
            postPetitionJester.reactionSeedOverride =
                "React playfully to the ruler's recent decision regarding the Seer's prophecy. Remind your half-brother in a clever, brotherly riddle that gold cannot buy a soul, but wisdom might save a crown.";
            postPetitionJester.continueNextCard = climaxTrial;
            SaveAsset(postPetitionJester, "Test_Reaction_JesterReflection");

            // Interactive Petition Card
            CardData seerPetition = ScriptableObject.CreateInstance<CardData>();
            seerPetition.assetName = "Test_Petition_SeerProphecy";
            seerPetition.displayName = "The Seer's Demand";
            seerPetition.speaker = seer;
            seerPetition.isPetitionCard = true;
            seerPetition.petitionSeedOverride =
                "You have entered the royal throne room uninvited. You demand gold or royal privileges before unveiling a dire prophecy about the crown's fate. Challenge the ruler to state your price or face an unseen disaster.";
            seerPetition.continueNextCard = postPetitionJester;
            SaveAsset(seerPetition, "Test_Petition_SeerProphecy");

            // ==========================================
            // 6. MID-GAME SCENES (TREASURY & MILITARY)
            // ==========================================
            CardData treasuryAudit = ScriptableObject.CreateInstance<CardData>();
            treasuryAudit.assetName = "Test_Scene_TreasuryAudit";
            treasuryAudit.displayName = "The Golden Ledger";
            treasuryAudit.speaker = treasuryAdviser;
            treasuryAudit.descriptionLocalized = Loc(
                "Treasury Adviser Midas taps his ledger with a sharp quill. 'Sire, the vaults are leaking coin. Grant me unilateral authority over trade tolls and merchant taxes, and I shall make the crown wealthier than the gods.'",
                "ينقر مستشار الخزانة ميداس على سجله بريشة حادة: 'يا مولاي، الخزائن تنزف ذهبًا. امنحني السيطرة الكاملة على مكوس التجارة وضرائب التجار، وسأجعل التاج أغنى من الآلهة.'");
            treasuryAudit.dayAdvance = 2;
            treasuryAudit.leftChoiceLocalized = Loc("Grant Midas total ledger control", "امنح ميداس السيطرة المطلقة على السجلات");
            treasuryAudit.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = 25 },
                    new ResourceValue { resource = resAuthority, value = -15 },
                    new ResourceValue { resource = resMorale, value = -10 }
                }
            };
            treasuryAudit.leftNextCard = seerPetition;
            treasuryAudit.rightChoiceLocalized = Loc("Subject vaults to open inspection", "اخضع الخزائن لتفتيش علني صارم");
            treasuryAudit.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = -10 },
                    new ResourceValue { resource = resAuthority, value = 15 },
                    new ResourceValue { resource = resMorale, value = 10 }
                }
            };
            treasuryAudit.rightNextCard = seerPetition;
            SaveAsset(treasuryAudit, "Test_Scene_TreasuryAudit");

            CardData warCouncil = ScriptableObject.CreateInstance<CardData>();
            warCouncil.assetName = "Test_Scene_GeneralWarCouncil";
            warCouncil.displayName = "The General's Diplomacy";
            warCouncil.speaker = general;
            warCouncil.descriptionLocalized = Loc(
                "General Valerius storms into council, slamming his iron gauntlet on the map. 'Raiders test our borders! My strategy is a brilliant diplomatic masterstroke: send the entire cavalry in a crushing charge!'",
                "يقتحم الجنرال فاليريوس القاعة ضاربًا قفازه الحربي على الخريطة: 'الغزاة يختبرون حدودنا! خطتي هي تحفة دبلوماسية بارعة: أرسل سلاح الفرسان بأكمله في هجوم ساحق!'");
            warCouncil.dayAdvance = 2;
            warCouncil.leftChoiceLocalized = Loc("Full cavalry charge!", "هجوم كاسح بالفرسان!");
            warCouncil.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMilitary, value = 15 },
                    new ResourceValue { resource = resTreasury, value = -15 },
                    new ResourceValue { resource = resMorale, value = -5 }
                }
            };
            warCouncil.leftNextCard = treasuryAudit;
            warCouncil.rightChoiceLocalized = Loc("Fortify garrisons & negotiate", "حصّن الحاميات وابدأ التفاوض");
            warCouncil.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMilitary, value = -10 },
                    new ResourceValue { resource = resTreasury, value = 10 },
                    new ResourceValue { resource = resMorale, value = 10 }
                }
            };
            warCouncil.rightNextCard = treasuryAudit;
            SaveAsset(warCouncil, "Test_Scene_GeneralWarCouncil");

            // ==========================================
            // 7. LLM REACTION CARDS (EARLY COURT REACTIONS)
            // ==========================================
            CardData motherScornReaction = ScriptableObject.CreateInstance<CardData>();
            motherScornReaction.assetName = "Test_Reaction_MotherScorn";
            motherScornReaction.displayName = "A Mother's Scorn";
            motherScornReaction.speaker = spiritMother;
            motherScornReaction.isLlmReactionCard = true;
            motherScornReaction.reactionSeedOverride =
                "Admonish your spoiled royal son for his reckless, self-indulgent choice. Remind him with haunting sorrow that kings who indulge their vanity build their own gallows.";
            motherScornReaction.continueNextCard = warCouncil;
            SaveAsset(motherScornReaction, "Test_Reaction_MotherScorn");

            CardData chancellorGrimaceReaction = ScriptableObject.CreateInstance<CardData>();
            chancellorGrimaceReaction.assetName = "Test_Reaction_ChancellorGrimace";
            chancellorGrimaceReaction.displayName = "Silken Displeasure";
            chancellorGrimaceReaction.speaker = chancellor;
            chancellorGrimaceReaction.isLlmReactionCard = true;
            chancellorGrimaceReaction.reactionSeedOverride =
                "Conceal your secret frustration behind a veneer of obsequious court praise. Flatter the king's unexpected frugality while subtly plotting your next move.";
            chancellorGrimaceReaction.continueNextCard = warCouncil;
            SaveAsset(chancellorGrimaceReaction, "Test_Reaction_ChancellorGrimace");

            CardData generalBaffledReaction = ScriptableObject.CreateInstance<CardData>();
            generalBaffledReaction.assetName = "Test_Reaction_GeneralBaffled";
            generalBaffledReaction.displayName = "A General's Confusion";
            generalBaffledReaction.speaker = general;
            generalBaffledReaction.isLlmReactionCard = true;
            generalBaffledReaction.reactionSeedOverride =
                "Express booming confusion at the king's bizarre decision, comparing it to an unorthodox flank maneuver that defies all battlefield doctrine.";
            generalBaffledReaction.continueNextCard = warCouncil;
            SaveAsset(generalBaffledReaction, "Test_Reaction_GeneralBaffled");

            // ==========================================
            // 8. EARLY BRANCHING SCENES
            // ==========================================
            CardData branchChancellor = ScriptableObject.CreateInstance<CardData>();
            branchChancellor.assetName = "Test_Branch_ChancellorEgo";
            branchChancellor.displayName = "The Golden Colossus";
            branchChancellor.speaker = chancellor;
            branchChancellor.descriptionLocalized = Loc(
                "Chancellor Malakor bows low, his smile dripping with silk. 'Sire, the commoners must witness your supreme glory! Let us levy a double harvest tax to build a golden colossus in your likeness.'",
                "ينحني المستشار مالاكور بابتسامة متملقة: 'مولاي المعظم، يجب أن ترى الرعية مجدك المطلق! دعنا نفرض ضريبة حصاد مضاعفة لتشييد تمثال عملاق من الذهب الخالص يجسد عظمتكم.'");
            branchChancellor.dayAdvance = 2;
            branchChancellor.leftChoiceLocalized = Loc("Build the Golden Colossus!", "شيّدوا التمثال الذهبي فورًا!");
            branchChancellor.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = -20 },
                    new ResourceValue { resource = resMorale, value = -15 },
                    new ResourceValue { resource = resAuthority, value = 10 }
                }
            };
            branchChancellor.leftNextCard = motherScornReaction;
            branchChancellor.rightChoiceLocalized = Loc("Refuse and slash his court budget", "ارفض وخفّض ميزانية بلاطه");
            branchChancellor.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = 15 },
                    new ResourceValue { resource = resAuthority, value = -10 }
                }
            };
            branchChancellor.rightNextCard = chancellorGrimaceReaction;
            SaveAsset(branchChancellor, "Test_Branch_ChancellorEgo");

            CardData branchJester = ScriptableObject.CreateInstance<CardData>();
            branchJester.assetName = "Test_Branch_JesterRiddle";
            branchJester.displayName = "The Fool's Feast";
            branchJester.speaker = jester;
            branchJester.descriptionLocalized = Loc(
                "The Jester cartwheels into the throne room, jingling bells in your face. 'Brother King! The thieves scheme to loot the granaries! Why not throw the gates open and feed every beggar before they steal a crumb?'",
                "يتشقلب المهرج داخل قاعة العرش رانًا أجراسه في وجهك: 'أخي الملك! اللصوص يخططون لنهب مخازن الحبوب! لمَ لا نفتح الأبواب ونطعم كل متسول قبل أن يسرقوا كسرة واحدة؟'");
            branchJester.dayAdvance = 2;
            branchJester.leftChoiceLocalized = Loc("Host the grand Fool's Feast!", "أقم وليمة المهرج الكبرى!");
            branchJester.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = -10 },
                    new ResourceValue { resource = resMorale, value = 20 },
                    new ResourceValue { resource = resAuthority, value = -5 }
                }
            };
            branchJester.leftNextCard = generalBaffledReaction;
            branchJester.rightChoiceLocalized = Loc("Have him tossed into the moat", "ألقه في خندق القلعة المائي");
            branchJester.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resTreasury, value = 5 },
                    new ResourceValue { resource = resMorale, value = -10 },
                    new ResourceValue { resource = resAuthority, value = 5 }
                }
            };
            branchJester.rightNextCard = motherScornReaction;
            SaveAsset(branchJester, "Test_Branch_JesterRiddle");

            // ==========================================
            // 9. INTRO START CARD
            // ==========================================
            CardData start = ScriptableObject.CreateInstance<CardData>();
            start.assetName = "Test_Intro_SpiritMother";
            start.displayName = "The Spectral Awakening";
            start.speaker = spiritMother;
            start.descriptionLocalized = Loc(
                "A cold mist gathers above the royal dais. The ghostly figure of your late mother appears, looking down at you with stern sorrow. 'My spoiled son... your arrogance will drown this realm unless you learn to rule.'",
                "يتصاعد ضباب بارد فوق العرش الملكي. يظهر طيف والدتك الراحلة ناظرًا إليك بحزن وحزم: 'يا ولدي المدلل... إن غرورك سيغرق هذه المملكة إن لم تتعلم كيف تحكم حقًا.'");
            start.dayAdvance = 1;
            start.leftChoiceLocalized = Loc("Dismiss as a fever dream", "تجاهل الطيف كأنه كابوس");
            start.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMorale, value = -10 },
                    new ResourceValue { resource = resAuthority, value = 5 }
                }
            };
            start.leftNextCard = branchChancellor;
            start.rightChoiceLocalized = Loc("Bow and hear her counsel", "انحنِ واستمع لنصيحتها");
            start.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = resMorale, value = 10 },
                    new ResourceValue { resource = resAuthority, value = -5 }
                }
            };
            start.rightNextCard = branchJester;
            SaveAsset(start, "Test_Intro_SpiritMother");

            // ==========================================
            // 10. PROMPT TEMPLATES & DATABASE
            // ==========================================
            LlmPromptTemplates templates = ScriptableObject.CreateInstance<LlmPromptTemplates>();
            SaveAsset(templates, "LlmPromptTemplates_Test");

            NarrativeDatabase database = ScriptableObject.CreateInstance<NarrativeDatabase>();
            database.startingCard = start;
            database.resourceCatalog = catalog;
            database.promptTemplates = templates;
            database.cards = new List<CardData>
            {
                start, branchChancellor, branchJester,
                motherScornReaction, chancellorGrimaceReaction, generalBaffledReaction,
                warCouncil, treasuryAudit, seerPetition, postPetitionJester, climaxTrial,
                endingVictory, endingTyrant,
                treasuryCollapseEnding, militaryCollapseEnding, authorityCollapseEnding, moraleCollapseEnding
            };
            database.speakers = new List<SpeakerData>
            {
                spiritMother, jester, chancellor, general, treasuryAdviser, seer
            };

            // Pre-calculate visual graph node positions for Card Graph Editor
            database.editorGraphPositions = new List<NarrativeDatabase.CardGraphPosition>
            {
                new NarrativeDatabase.CardGraphPosition { card = start, position = new Vector2(100, 300) },
                new NarrativeDatabase.CardGraphPosition { card = branchChancellor, position = new Vector2(450, 150) },
                new NarrativeDatabase.CardGraphPosition { card = branchJester, position = new Vector2(450, 450) },
                new NarrativeDatabase.CardGraphPosition { card = motherScornReaction, position = new Vector2(800, 150) },
                new NarrativeDatabase.CardGraphPosition { card = chancellorGrimaceReaction, position = new Vector2(800, 300) },
                new NarrativeDatabase.CardGraphPosition { card = generalBaffledReaction, position = new Vector2(800, 450) },
                new NarrativeDatabase.CardGraphPosition { card = warCouncil, position = new Vector2(1150, 300) },
                new NarrativeDatabase.CardGraphPosition { card = treasuryAudit, position = new Vector2(1500, 300) },
                new NarrativeDatabase.CardGraphPosition { card = seerPetition, position = new Vector2(1850, 300) },
                new NarrativeDatabase.CardGraphPosition { card = postPetitionJester, position = new Vector2(2200, 300) },
                new NarrativeDatabase.CardGraphPosition { card = climaxTrial, position = new Vector2(2550, 300) },
                new NarrativeDatabase.CardGraphPosition { card = endingVictory, position = new Vector2(2900, 150) },
                new NarrativeDatabase.CardGraphPosition { card = endingTyrant, position = new Vector2(2900, 450) },
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
                new NarrativeDatabase.ResourceGraphPosition { resource = resTreasury, position = new Vector2(1650, -200) },
                new NarrativeDatabase.ResourceGraphPosition { resource = resMilitary, position = new Vector2(1900, -200) },
                new NarrativeDatabase.ResourceGraphPosition { resource = resAuthority, position = new Vector2(2150, -200) },
                new NarrativeDatabase.ResourceGraphPosition { resource = resMorale, position = new Vector2(2400, -200) }
            };

            SaveAsset(database, "NarrativeDatabase_Test");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TestContentGenerator] Successfully generated full 'Wicked King' test narrative in '{OutputFolder}'.\n" +
                      $"Includes 6 Characters, 4 Resources with Warning/Collapse Endings, dynamic branching, " +
                      $"LLM Reactions, Free-Form Petition, and full English/Arabic localization.");
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
