#if UNITY_EDITOR
using UnityEditor;
#endif
using Game.Scripts.Localization;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Base class for ScriptableObject definitions whose identity is the stable asset name, with an optional localized display name.</summary>
    /// <remarks>The asset filename is kept in sync with <see cref="assetName"/> via editor-only validation.</remarks>
    public abstract class NamedGameAsset : ScriptableObject
    {
        /// <summary>Stable identifier for this asset; falls back to the Unity object name when blank.</summary>
        public string assetName;

        /// <summary>Localized display name shown to the player; falls back to <see cref="AssetName"/> when empty or whitespace.</summary>
        public LocalizedText displayNameLocalized;

        /// <summary>Returns the trimmed <see cref="assetName"/>, or the Unity object name if <see cref="assetName"/> is blank.</summary>
        public string AssetName => !string.IsNullOrWhiteSpace(assetName) ? assetName.Trim() : name;

        /// <summary>Returns the localized display name for the given language, falling back to <see cref="AssetName"/>.</summary>
        /// <param name="language">Target language.</param>
        /// <returns>Localized display text, or the stable asset name when no localization is available.</returns>
        public string GetDisplayName(GameLanguage language)
        {
            if (!displayNameLocalized.IsEmpty)
            {
                string value = displayNameLocalized.Get(language);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return AssetName;
        }

#if UNITY_EDITOR
        // Schedules a deferred rename so the asset filename stays in sync with assetName without triggering
        // re-entrant serialization. This hook lives on the asset class (inside the runtime assembly) because
        // OnValidate is the only callback that fires on assetName edits; editor assemblies cannot be
        // referenced from here, so the editor-only logic stays behind #if UNITY_EDITOR.
        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return;
            }

            string trimmedName = assetName.Trim();
            EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                    return;
                }

                string assetPath = AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(assetPath))
                {
                    return;
                }

                string currentFilename = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (currentFilename == trimmedName)
                {
                    return;
                }

                AssetDatabase.RenameAsset(assetPath, trimmedName);
                AssetDatabase.SaveAssets();
            };
        }
#endif
    }
}
