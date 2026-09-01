#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Editor-only utility that keeps ScriptableObject asset filenames in sync with their logical names.</summary>
    internal static class DefinitionAssetRenamer
    {
        /// <summary>Schedules a deferred rename of the asset file to match <paramref name="trimmedName"/> on the next editor tick.</summary>
        /// <remarks>Uses <see cref="EditorApplication.delayCall"/> to avoid re-entrant asset database operations during serialization callbacks.</remarks>
        /// <param name="asset">The asset whose filename should be updated.</param>
        /// <param name="trimmedName">Desired filename without extension.</param>
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
