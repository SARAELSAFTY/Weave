using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Scripts.Definitions
{
    /// <summary>Defines a narrative speaker and optional LLM persona settings.</summary>
    [CreateAssetMenu(fileName = "Spk_NewSpeaker", menuName = "Weave/Speaker Data", order = 1)]
    public class SpeakerData : ScriptableObject
    {
        [Tooltip("Author-facing identifier used in the Card Graph editor and as the asset filename. " +
                 "Convention: Spk_<PascalName>, e.g. Spk_Advisor, Spk_Narrator. " +
                 "Changing this field renames the .asset file automatically.")]
        public string assetName;

        [Tooltip("Player-facing name shown in in-game UI (HUD, dialogue labels). " +
                 "Leave empty to fall back to the Asset Name.")]
        public string displayName;

        [Tooltip("Character portrait image.")]
        public Sprite portrait;

        [TextArea(4, 8), Tooltip("Base persona sent to the LLM whenever this speaker is voiced by AI: identity, tone, and behavior rules.")]
        public string llmPersonaPrompt;

        /// <summary>
        /// Author-facing identifier (graph node title, tooling). Falls back to Unity asset name.
        /// </summary>
        public string AssetName => !string.IsNullOrWhiteSpace(assetName) ? assetName.Trim() : name;

        /// <summary>
        /// Player-facing display label (in-game UI). Falls back to AssetName.
        /// </summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : AssetName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(assetName)) return;

            string trimmed = assetName.Trim();
            // Defer: AssetDatabase calls are not allowed mid-serialization (inside OnValidate).
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(assetPath)) return;

                string currentFilename = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (currentFilename == trimmed) return;

                UnityEditor.AssetDatabase.RenameAsset(assetPath, trimmed);
                UnityEditor.AssetDatabase.SaveAssets();
            };
        }
#endif
    }
}
