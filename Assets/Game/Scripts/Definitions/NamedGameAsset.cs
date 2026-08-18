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
            DefinitionAssetRenamer.ScheduleRenameToMatch(this, assetName.Trim());
        }
#endif
    }
}
