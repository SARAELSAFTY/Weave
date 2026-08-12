#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Llm;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>Menu command that scaffolds a complete test NarrativeDatabase under Assets/_TestContent.</summary>
    public static class TestContentGenerator
    {
        private const string OutputFolder = "Assets/_TestContent";

        [MenuItem("Weave/Generate Full Test Story")]
        public static void Generate()
        {
            EnsureFolder();

            SpeakerData advisor = ScriptableObject.CreateInstance<SpeakerData>();
            advisor.assetName = "Spk_Advisor";
            advisor.displayName = "Advisor Renn";
            advisor.llmPersonaPrompt =
                "You are Renn, a blunt, practical royal advisor. You speak in short, direct sentences, " +
                "value caution over ambition, and get visibly uneasy when supplies or morale run low. " +
                "You never use flowery language.";
            SaveAsset(advisor, "Spk_Advisor");

            SpeakerData spymaster = ScriptableObject.CreateInstance<SpeakerData>();
            spymaster.assetName = "Spk_Spymaster";
            spymaster.displayName = "Spymaster Vex";
            spymaster.llmPersonaPrompt =
                "You are Vex, the crown's spymaster. You speak in clipped, guarded sentences, treat trust " +
                "as a resource to be spent carefully, and rarely show open emotion. You favor pragmatic, " +
                "sometimes morally grey solutions.";
            SaveAsset(spymaster, "Spk_Spymaster");

            ResourceData supplies = ScriptableObject.CreateInstance<ResourceData>();
            supplies.assetName = "Res_Supplies";
            supplies.displayName = "Supplies";
            supplies.defaultStartingValue = 50;
            supplies.warningThresholdPercent = 30;
            supplies.warningSpeaker = advisor;
            supplies.warningCooldownCards = 5;
            SaveAsset(supplies, "Res_Supplies");

            ResourceData trust = ScriptableObject.CreateInstance<ResourceData>();
            trust.assetName = "Res_Trust";
            trust.displayName = "Trust";
            trust.defaultStartingValue = 50;
            trust.warningThresholdPercent = 25;
            trust.warningSpeaker = spymaster;
            trust.warningCooldownCards = 4;
            SaveAsset(trust, "Res_Trust");

            ResourceData morale = ScriptableObject.CreateInstance<ResourceData>();
            morale.assetName = "Res_Morale";
            morale.displayName = "Morale";
            morale.defaultStartingValue = 60;
            morale.warningThresholdPercent = 35;
            morale.warningSpeaker = advisor;
            morale.warningCooldownCards = 3;
            SaveAsset(morale, "Res_Morale");

            ResourceCatalog catalog = ScriptableObject.CreateInstance<ResourceCatalog>();
            catalog.resources = new List<ResourceData> { supplies, trust, morale };
            SaveAsset(catalog, "ResourceCatalog_Test");

            // Build cards leaf-first so continue/next references resolve when assigned.
            CardData ending = ScriptableObject.CreateInstance<CardData>();
            ending.assetName = "Test_Ending_Victory";
            ending.displayName = "Reign Endures";
            ending.speaker = advisor;
            ending.description = "The kingdom holds, battered but standing. History will remember this reign as steady, if not glorious.";
            ending.dayAdvance = 1;
            SaveAsset(ending, "Test_Ending_Victory");

            CardData suppliesCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            suppliesCollapseEnding.assetName = "Test_Ending_SuppliesCollapse";
            suppliesCollapseEnding.displayName = "Supplies Collapse";
            suppliesCollapseEnding.speaker = advisor;
            suppliesCollapseEnding.description = "The granaries fail. Hunger spreads faster than any decree can contain it.";
            suppliesCollapseEnding.dayAdvance = 1;
            SaveAsset(suppliesCollapseEnding, "Test_Ending_SuppliesCollapse");

            CardData trustCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            trustCollapseEnding.assetName = "Test_Ending_TrustCollapse";
            trustCollapseEnding.displayName = "Trust Collapse";
            trustCollapseEnding.speaker = spymaster;
            trustCollapseEnding.description = "The court turns inward and suspicion hardens into open fracture.";
            trustCollapseEnding.dayAdvance = 1;
            SaveAsset(trustCollapseEnding, "Test_Ending_TrustCollapse");

            CardData moraleCollapseEnding = ScriptableObject.CreateInstance<CardData>();
            moraleCollapseEnding.assetName = "Test_Ending_MoraleCollapse";
            moraleCollapseEnding.displayName = "Morale Collapse";
            moraleCollapseEnding.speaker = advisor;
            moraleCollapseEnding.description = "The kingdom loses its will to endure. Resolve dissolves into despair.";
            moraleCollapseEnding.dayAdvance = 1;
            SaveAsset(moraleCollapseEnding, "Test_Ending_MoraleCollapse");

            catalog.collapseEndings = new List<ResourceCollapseEnding>
            {
                new ResourceCollapseEnding { resource = supplies, endingCard = suppliesCollapseEnding },
                new ResourceCollapseEnding { resource = trust, endingCard = trustCollapseEnding },
                new ResourceCollapseEnding { resource = morale, endingCard = moraleCollapseEnding }
            };

            CardData postPetitionReaction = ScriptableObject.CreateInstance<CardData>();
            postPetitionReaction.assetName = "Test_Reaction_AdvisorReflects";
            postPetitionReaction.speaker = advisor;
            postPetitionReaction.isLlmReactionCard = true;
            postPetitionReaction.continueNextCard = ending;
            SaveAsset(postPetitionReaction, "Test_Reaction_AdvisorReflects");

            CardData petition = ScriptableObject.CreateInstance<CardData>();
            petition.assetName = "Test_Petition_SpymasterRequest";
            petition.speaker = spymaster;
            petition.isPetitionCard = true;
            petition.continueNextCard = postPetitionReaction;
            SaveAsset(petition, "Test_Petition_SpymasterRequest");

            CardData courtReacts = ScriptableObject.CreateInstance<CardData>();
            courtReacts.assetName = "Test_Reaction_CourtReacts";
            courtReacts.speaker = advisor;
            courtReacts.isLlmReactionCard = true;
            courtReacts.continueNextCard = petition;
            SaveAsset(courtReacts, "Test_Reaction_CourtReacts");

            CardData branchRation = ScriptableObject.CreateInstance<CardData>();
            branchRation.assetName = "Test_Branch_Ration";
            branchRation.displayName = "Rationing Begins";
            branchRation.speaker = advisor;
            branchRation.description = "Rationing begins. Grain stores are locked and guarded. The people grumble but comply - for now.";
            branchRation.dayAdvance = 1;
            branchRation.leftChoiceText = "Enforce strictly";
            branchRation.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = supplies, value = 15 },
                    new ResourceValue { resource = trust, value = -10 }
                }
            };
            branchRation.leftNextCard = courtReacts;
            branchRation.rightChoiceText = "Allow exceptions for the sick";
            branchRation.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = supplies, value = 5 },
                    new ResourceValue { resource = trust, value = 5 },
                    new ResourceValue { resource = morale, value = 5 }
                }
            };
            branchRation.rightNextCard = courtReacts;
            SaveAsset(branchRation, "Test_Branch_Ration");

            CardData branchReassure = ScriptableObject.CreateInstance<CardData>();
            branchReassure.assetName = "Test_Branch_Reassure";
            branchReassure.displayName = "Words of Comfort";
            branchReassure.speaker = spymaster;
            branchReassure.description = "The court is reassured with careful words. Whether they believe them is another matter.";
            branchReassure.dayAdvance = 1;
            branchReassure.leftChoiceText = "Promise more than you can deliver";
            branchReassure.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = trust, value = 15 },
                    new ResourceValue { resource = supplies, value = -10 }
                }
            };
            branchReassure.leftNextCard = courtReacts;
            branchReassure.rightChoiceText = "Speak only the truth";
            branchReassure.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = trust, value = 5 },
                    new ResourceValue { resource = morale, value = 10 }
                }
            };
            branchReassure.rightNextCard = courtReacts;
            SaveAsset(branchReassure, "Test_Branch_Reassure");

            CardData start = ScriptableObject.CreateInstance<CardData>();
            start.assetName = "Test_Intro_Start";
            start.displayName = "The Granary Question";
            start.speaker = advisor;
            start.description = "The granary sits half-empty and the court grows restless. The kingdom looks to you for a decision.";
            start.dayAdvance = 2;
            start.leftChoiceText = "Ration the food";
            start.leftResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = supplies, value = 5 },
                    new ResourceValue { resource = morale, value = -5 }
                }
            };
            start.leftNextCard = branchRation;
            start.rightChoiceText = "Reassure the court";
            start.rightResourceChange = new ResourceChange
            {
                values = new[]
                {
                    new ResourceValue { resource = trust, value = 5 },
                    new ResourceValue { resource = morale, value = 5 }
                }
            };
            start.rightNextCard = branchReassure;
            SaveAsset(start, "Test_Intro_Start");

            LlmPromptTemplates templates = ScriptableObject.CreateInstance<LlmPromptTemplates>();
            SaveAsset(templates, "LlmPromptTemplates_Test");

            NarrativeDatabase database = ScriptableObject.CreateInstance<NarrativeDatabase>();
            database.startingCard = start;
            database.resourceCatalog = catalog;
            database.promptTemplates = templates;
            database.cards = new List<CardData>
            {
                start, branchRation, branchReassure, courtReacts, petition, postPetitionReaction, ending,
                suppliesCollapseEnding, trustCollapseEnding, moraleCollapseEnding
            };
            database.speakers = new List<SpeakerData> { advisor, spymaster };
            SaveAsset(database, "NarrativeDatabase_Test");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TestContentGenerator] Generated full test story in '{OutputFolder}'. " +
                      "Assign 'NarrativeDatabase_Test' to your GameManager's Narrative Database field. " +
                      "Story: Test_Intro_Start -> (Ration|Reassure branch) -> Test_Reaction_CourtReacts -> " +
                      "Test_Petition_SpymasterRequest -> Test_Reaction_AdvisorReflects -> Test_Ending_Victory.");
        }

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
