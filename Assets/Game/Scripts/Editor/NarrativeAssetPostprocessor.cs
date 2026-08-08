using System;
using System.IO;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;

namespace Game.Scripts.Editor
{
    // Keeps CardData, CouncilMemberData, and ResourceData IDs in sync with their asset file names
    // whenever they are imported, renamed, or moved in the Project window.
    public class NarrativeAssetPostprocessor : AssetPostprocessor
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

                if (!newPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                
                // Cards
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(newPath);
                if (card != null)
                {
                    CardGraphEditor.SyncIdToAssetName(card, repoint: true);
                    graphNeedsRefresh = true;
                    continue;
                }

                // Speakers
                CouncilMemberData speaker = AssetDatabase.LoadAssetAtPath<CouncilMemberData>(newPath);
                if (speaker != null)
                {
                    CardGraphEditor.SyncIdToAssetName(speaker, repoint: true);
                    graphNeedsRefresh = true;
                    continue;
                }

                // Resources
                ResourceData resource = AssetDatabase.LoadAssetAtPath<ResourceData>(newPath);
                if (resource != null)
                {
                    CardGraphEditor.SyncIdToAssetName(resource, repoint: true);
                    graphNeedsRefresh = true;
                    continue;
                }
            }

            // Handle imported assets (newly created or duplicated)
            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null)
                {
                    CardGraphEditor.SyncIdToAssetName(card, repoint: false);
                    graphNeedsRefresh = true;
                    continue;
                }

                CouncilMemberData speaker = AssetDatabase.LoadAssetAtPath<CouncilMemberData>(path);
                if (speaker != null)
                {
                    CardGraphEditor.SyncIdToAssetName(speaker, repoint: false);
                    graphNeedsRefresh = true;
                    continue;
                }

                ResourceData resource = AssetDatabase.LoadAssetAtPath<ResourceData>(path);
                if (resource != null)
                {
                    CardGraphEditor.SyncIdToAssetName(resource, repoint: false);
                    graphNeedsRefresh = true;
                    continue;
                }
            }

            if (graphNeedsRefresh)
            {
                CardGraphWindow.RefreshOpenWindows();
            }
        }
    }
}
