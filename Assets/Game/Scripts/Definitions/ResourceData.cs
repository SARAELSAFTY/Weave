using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Scripts.Definitions
{
    /// <summary>Defines display and default values for one game resource.</summary>
    [CreateAssetMenu(fileName = "Res_NewResource", menuName = "Weave/Resource Data", order = 2)]
    public class ResourceData : ScriptableObject
    {
        [Tooltip("Author-facing identifier used in the Card Graph editor and as the asset filename. " +
                 "Convention: Res_<PascalName>, e.g. Res_Trust, Res_Supplies. " +
                 "Changing this field renames the .asset file automatically.")]
        public string assetName;

        [Tooltip("Player-facing name shown in in-game UI (HUD, resource bars). " +
                 "Leave empty to fall back to the Asset Name.")]
        public string displayName;

        [Tooltip("Resource icon image.")]
        public Sprite icon;

        [Tooltip("Starting value for this resource at the beginning of a run.")]
        public int defaultStartingValue = 50;

        [Header("Warning Threshold")]
        [Range(0, 100), Tooltip("Resource triggers a Warning reaction at or below this percent of its starting value.")]
        public int warningThresholdPercent = 30;

        [Header("Collapse Ending")]
        [Tooltip("Ending card shown immediately when this resource hits 0 or below. " +
                 "If left unset, this resource will NOT force an immediate game over — " +
                 "it will only ever fire its Warning reaction, same as today.")]
        public CardData collapseEndingCard;

        [Header("Warning Alert Speaker & Prompt")]
        [Tooltip("Speaker who reacts when this resource crosses the Warning threshold.")]
        public SpeakerData warningSpeaker;

        [TextArea(2, 4), Tooltip("Seed prompt used when the Warning threshold is crossed. Use {resourceName} as a placeholder for the resource name.")]
        public string warningSeedPrompt = "{resourceName} is running low. React with mild concern, in character, and suggest the player pay attention soon.";

        [Min(0), Tooltip("Minimum story cards that must pass after a warning before this resource can warn again.")]
        public int warningCooldownCards = 5;



        /// <summary>
        /// Author-facing identifier (graph node title, tooling). Falls back to Unity asset name.
        /// </summary>
        public string AssetName => !string.IsNullOrWhiteSpace(assetName) ? assetName.Trim() : name;

        /// <summary>
        /// Player-facing display label (in-game UI, resource bars). Falls back to AssetName.
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
