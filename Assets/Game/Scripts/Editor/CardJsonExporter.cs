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
                cardId = card.cardId,
                description = card.description,
                speakerId = card.speakerId,
                dayAdvance = card.dayAdvance,
                leftChoiceText = card.leftChoiceText,
                leftNextCardId = card.leftNextCardId,
                leftResourceChange = ToResourceJson(card.leftResourceChange),
                rightChoiceText = card.rightChoiceText,
                rightNextCardId = card.rightNextCardId,
                rightResourceChange = ToResourceJson(card.rightResourceChange),
                isLlmReactionCard = card.isLlmReactionCard,
                llmPromptSeed = card.llmPromptSeed,
                continueNextCardId = card.continueNextCardId,
                isEnding = card.IsEnding
            });
        }
        cardList.Sort((a, b) => string.Compare(a.cardId, b.cardId, System.StringComparison.Ordinal));

        var speakerList = new List<SpeakerJson>();
        foreach (CouncilMemberData speaker in db.speakers)
        {
            if (speaker == null) continue;

            speakerList.Add(new SpeakerJson
            {
                memberId = speaker.memberId,
                displayName = speaker.displayName,
                title = speaker.title,
                isLlmSpeaker = speaker.isLlmSpeaker,
                llmPersonaPrompt = speaker.llmPersonaPrompt
            });
        }
        speakerList.Sort((a, b) => string.Compare(a.memberId, b.memberId, System.StringComparison.Ordinal));

        var resourceList = new List<ResourceDefJson>();
        if (db.resourceCatalog != null)
        {
            foreach (var res in db.resourceCatalog.resources)
            {
                resourceList.Add(new ResourceDefJson { id = res.id, displayName = res.displayName });
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
            result[i] = new ResourceEntryJson { id = rc.values[i].id, value = rc.values[i].value };

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
        public string cardId;
        public string description;
        public string speakerId;
        public int dayAdvance;
        public string leftChoiceText;
        public string leftNextCardId;
        public ResourceEntryJson[] leftResourceChange;
        public string rightChoiceText;
        public string rightNextCardId;
        public ResourceEntryJson[] rightResourceChange;
        public bool isLlmReactionCard;
        public string llmPromptSeed;
        public string continueNextCardId;
        public bool isEnding;
    }

    [System.Serializable]
    private class SpeakerJson
    {
        public string memberId;
        public string displayName;
        public string title;
        public bool isLlmSpeaker;
        public string llmPersonaPrompt;
    }

    [System.Serializable]
    private class ResourceDefJson
    {
        public string id;
        public string displayName;
    }

    [System.Serializable]
    private class ResourceEntryJson
    {
        public string id;
        public int value;
    }
}
}
