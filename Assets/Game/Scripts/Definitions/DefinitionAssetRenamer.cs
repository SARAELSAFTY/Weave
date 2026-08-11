#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>
    /// OnValidate helper: renames a ScriptableObject asset file to match its author-facing name.
    /// Uses delayCall so rename does not run mid-serialization.
    /// </summary>
    internal static class DefinitionAssetRenamer
    {
        public static void ScheduleRenameToMatch(Object asset, string trimmedName)
        {
            if (asset == null || string.IsNullOrWhiteSpace(trimmedName)) return;

            EditorApplication.delayCall += () =>
            {
                if (asset == null) return;
                string assetPath = AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath)) return;

                string currentFilename = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (currentFilename == trimmedName) return;

                AssetDatabase.RenameAsset(assetPath, trimmedName);
                AssetDatabase.SaveAssets();
            };
        }
    }
}
#endif
