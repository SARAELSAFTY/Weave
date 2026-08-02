using System.IO;
using UnityEditor;

// Keeps CardData.cardId in sync with its asset file name whenever cards are imported,
// renamed, or moved in the Project window (as opposed to renamed via the inspector or
// graph, which call CardGraph.SyncIdToAssetName directly).
public class CardAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        bool graphNeedsRefresh = false;

        // Handle moved or renamed assets
        for (int i = 0; i < movedAssets.Length; i++)
        {
            string newPath = movedAssets[i];
            string oldPath = movedFromAssetPaths[i];

            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(newPath);
            if (card == null)
            {
                continue;
            }

            string oldFileName = Path.GetFileNameWithoutExtension(oldPath);
            string newFileName = Path.GetFileNameWithoutExtension(newPath);

            if (oldFileName == newFileName && card.cardId == newFileName)
            {
                continue;
            }

            // Repoint anything that referenced either the old file name or the old cardId.
            CardGraph.RepointReferences(card.cardId, newFileName);
            if (!string.IsNullOrEmpty(oldFileName) && oldFileName != card.cardId)
            {
                CardGraph.RepointReferences(oldFileName, newFileName);
            }

            Undo.RecordObject(card, "Sync Card ID with Asset Name");
            card.cardId = newFileName;
            EditorUtility.SetDirty(card);
            graphNeedsRefresh = true;
        }

        // Handle imported assets
        foreach (string path in importedAssets)
        {
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card == null)
            {
                continue;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (card.cardId == fileName)
            {
                continue;
            }

            Undo.RecordObject(card, "Sync Card ID");
            card.cardId = fileName;
            EditorUtility.SetDirty(card);
            graphNeedsRefresh = true;
        }

        if (graphNeedsRefresh)
        {
            CardGraphWindow.RefreshOpenWindows();
        }
    }
}
