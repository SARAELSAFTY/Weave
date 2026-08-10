using System.Collections.Generic;
using System.IO;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{   
    // One-click exporter: Tools > Weave > Export Cards to JSON
    // Writes cards, speakers, and resources from a NarrativeDatabase to Assets/StreamingAssets/cards.json
    public static class CardJsonExporter
    {
        [MenuItem("Tools/Weave/Export Cards to JSON")]
        public static void ExportCards()
        {
            string[] guids = AssetDatabase.FindAssets("t:NarrativeDatabase");
            if (guids.Length == 0)
            {
                Debug.LogError("[CardJsonExporter] No NarrativeDatabase found in project.");
                return;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            NarrativeDatabase db = AssetDatabase.LoadAssetAtPath<NarrativeDatabase>(path);
            ExportDatabase(db);
        }

        public static void ExportDatabase(NarrativeDatabase db)
        {
            if (db == null) return;

            var cardList = new List<CardJson>();
            foreach (CardData card in db.cards)
            {
                if (card == null) continue;

                cardList.Add(new CardJson
                {
                    assetName = card.AssetName,
                    displayName = card.displayName,
                    description = card.description,
                    speaker = card.speaker != null ? card.speaker.AssetName : string.Empty,
                    dayAdvance = card.dayAdvance,
                    leftChoiceText = card.leftChoiceText,
                    leftNextCard = card.leftNextCard != null ? card.leftNextCard.AssetName : string.Empty,
                    leftResourceChange = ToResourceJson(card.leftResourceChange),
                    rightChoiceText = card.rightChoiceText,
                    rightNextCard = card.rightNextCard != null ? card.rightNextCard.AssetName : string.Empty,
                    rightResourceChange = ToResourceJson(card.rightResourceChange),
                    isLlmReactionCard = card.isLlmReactionCard,
                    llmPromptSeed = card.llmPromptSeed,
                    continueNextCard = card.continueNextCard != null ? card.continueNextCard.AssetName : string.Empty,
                    isEnding = card.IsEnding
                });
            }
            cardList.Sort((a, b) => string.Compare(a.assetName, b.assetName, System.StringComparison.Ordinal));

            var speakerList = new List<SpeakerJson>();
            foreach (SpeakerData speaker in db.speakers)
            {
                if (speaker == null) continue;

                speakerList.Add(new SpeakerJson
                {
                    assetName = speaker.AssetName,
                    displayName = speaker.displayName,
                    llmPersonaPrompt = speaker.llmPersonaPrompt
                });
            }
            speakerList.Sort((a, b) => string.Compare(a.assetName, b.assetName, System.StringComparison.Ordinal));

            var resourceList = new List<ResourceDefJson>();
            if (db.resourceCatalog != null)
            {
                foreach (var res in db.resourceCatalog.resources)
                {
                    if (res == null) continue;
                    resourceList.Add(new ResourceDefJson
                    {
                        assetName = res.AssetName,
                        displayName = res.displayName,
                        collapseEndingCard = res.collapseEndingCard != null ? res.collapseEndingCard.AssetName : string.Empty
                    });
                }
            }

            var export = new CardListJson
            {
                cards = cardList,
                speakers = speakerList,
                resources = resourceList
            };

            string json = JsonUtility.ToJson(export, prettyPrint: true);

            string dir = Path.Combine(Application.dataPath, "StreamingAssets");
            Directory.CreateDirectory(dir);
            string outputPath = Path.Combine(dir, "cards.json");
            File.WriteAllText(outputPath, json);

            AssetDatabase.Refresh();
            Debug.Log($"[CardJsonExporter] Exported {cardList.Count} cards to {outputPath}");
            EditorUtility.DisplayDialog("Export Complete", $"Exported {cardList.Count} cards to:\n{outputPath}", "OK");
        }

        private static ResourceEntryJson[] ToResourceJson(ResourceChange rc)
        {
            if (rc.values == null) return System.Array.Empty<ResourceEntryJson>();

            var result = new ResourceEntryJson[rc.values.Length];
            for (int i = 0; i < rc.values.Length; i++)
            {
                result[i] = new ResourceEntryJson
                {
                    resource = rc.values[i].resource != null ? rc.values[i].resource.AssetName : string.Empty,
                    value = rc.values[i].value
                };
            }

            return result;
        }

        [System.Serializable]
        private class CardListJson
        {
            public List<CardJson> cards;
            public List<SpeakerJson> speakers;
            public List<ResourceDefJson> resources;
        }

        [System.Serializable]
        private class CardJson
        {
            public string assetName;
            public string displayName;
            public string description;
            public string speaker;
            public int dayAdvance;
            public string leftChoiceText;
            public string leftNextCard;
            public ResourceEntryJson[] leftResourceChange;
            public string rightChoiceText;
            public string rightNextCard;
            public ResourceEntryJson[] rightResourceChange;
            public bool isLlmReactionCard;
            public string llmPromptSeed;
            public string continueNextCard;
            public bool isEnding;
        }

        [System.Serializable]
        private class SpeakerJson
        {
            public string assetName;
            public string displayName;
            public string llmPersonaPrompt;
        }

        [System.Serializable]
        private class ResourceDefJson
        {
            public string assetName;
            public string displayName;
            public string collapseEndingCard;
        }

        [System.Serializable]
        private class ResourceEntryJson
        {
            public string resource;
            public int value;
        }
    }
}
