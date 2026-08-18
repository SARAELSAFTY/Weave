using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Shared identity for authored content assets: author-facing ID, player-facing label, file-rename-on-save.</summary>
    public abstract class NamedGameAsset : ScriptableObject
    {
        public string assetName;
        public string displayName;

        /// <summary>Author-facing ID for tooling. Falls back to the Unity asset name.</summary>
        public string AssetName => !string.IsNullOrWhiteSpace(assetName) ? assetName.Trim() : name;

        /// <summary>Player-facing label. Falls back to <see cref="AssetName"/> when unset.</summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : AssetName;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(assetName)) return;
            ScheduleRenameToMatch(this, assetName.Trim());
        }

        private static void ScheduleRenameToMatch(Object asset, string trimmedName)
        {
            if (asset == null || string.IsNullOrWhiteSpace(trimmedName)) return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (asset == null) return;
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(asset);
                if (string.IsNullOrEmpty(assetPath)) return;

                string currentFilename = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (currentFilename == trimmedName) return;

                UnityEditor.AssetDatabase.RenameAsset(assetPath, trimmedName);
                UnityEditor.AssetDatabase.SaveAssets();
            };
        }
#endif
    }
}
