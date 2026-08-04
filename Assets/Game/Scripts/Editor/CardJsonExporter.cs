using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// One-click exporter: Tools > Weave > Export Cards to JSON
// Writes all CardData assets to Assets/StreamingAssets/cards.json
public static class CardJsonExporter
{
    [MenuItem("Tools/Weave/Export Cards to JSON")]
    public static void ExportCards()
    {
        string[] guids = AssetDatabase.FindAssets("t:CardData");
        var cardList = new List<CardJson>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
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
                isEnding = card.isEnding
            });
        }

        // Sort by cardId so the file is easy to read
        cardList.Sort((a, b) => string.Compare(a.cardId, b.cardId, System.StringComparison.Ordinal));

        string json = JsonUtility.ToJson(new CardListJson { cards = cardList }, prettyPrint: true);

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
        if (rc.values == null) return new ResourceEntryJson[0];

        var result = new ResourceEntryJson[rc.values.Length];
        for (int i = 0; i < rc.values.Length; i++)
            result[i] = new ResourceEntryJson { id = rc.values[i].id, value = rc.values[i].value };

        return result;
    }

    [System.Serializable]
    private class CardListJson
    {
        public List<CardJson> cards;
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
        public bool isEnding;
    }

    [System.Serializable]
    private class ResourceEntryJson
    {
        public string id;
        public int value;
    }
}
