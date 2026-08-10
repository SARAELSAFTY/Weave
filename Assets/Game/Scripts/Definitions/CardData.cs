using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Scripts.Definitions
{
    /// <summary>Defines authored content and outcomes for one narrative card.</summary>
    [CreateAssetMenu(fileName = "Scene_Speaker_Slug", menuName = "Weave/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Author-facing identifier shown in the Card Graph editor. Drives the asset filename on save. " +
                 "Convention: <Scene>_<Speaker>_<Slug>, e.g. Market_Advisor_WarnsBlight. " +
                 "Changing this field renames the .asset file automatically. NOT shown to players.")]
        public string assetName;

        [Tooltip("Player-facing label for this card, if needed by the UI layer. " +
                 "Leave empty — most cards do not expose a title to the player.")]
        public string displayName;

        /// <summary>
        /// Author-facing identifier (graph node title, dropdowns, tooling). Falls back to Unity asset name.
        /// </summary>
        public string AssetName => !string.IsNullOrWhiteSpace(assetName) ? assetName.Trim() : name;

        /// <summary>
        /// Player-facing display label. Falls back to AssetName when not set.
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

        [Header("Speaker")]
        [Tooltip("Character speaking this card. Required for run execution.")]
        public SpeakerData speaker;

        [Header("Card Content")]
        [TextArea(3, 6), Tooltip("Main story text shown on card.")]
        public string description;

        [Min(0), Tooltip("Days time advances when played.")]
        public int dayAdvance = 1;

        [Header("Left Choice")]
        [Tooltip("Text shown when swiping left.")]
        public string leftChoiceText;

        [Tooltip("Resource changes when choosing left.")]
        public ResourceChange leftResourceChange;

        [Tooltip("Card loaded when choosing left.")]
        public CardData leftNextCard;

        [Header("Right Choice")]
        [Tooltip("Text shown when swiping right.")]
        public string rightChoiceText;

        [Tooltip("Resource changes when choosing right.")]
        public ResourceChange rightResourceChange;

        [Tooltip("Card loaded when choosing right.")]
        public CardData rightNextCard;

        [Header("LLM Reaction Card")]
        [Tooltip("Marks this card as an LLM-generated reaction card.")]
        public bool isLlmReactionCard;

        [TextArea(3, 6), Tooltip("Seed prompt instructing the AI on how to react.")]
        public string llmPromptSeed;

        [Tooltip("Card to advance to upon swipe (used when isLlmReactionCard is true).")]
        public CardData continueNextCard;

        /// <summary>Returns the left branch target for this card.</summary>
        public CardData ResolveLeftNextCard()
        {
            return isLlmReactionCard ? continueNextCard : leftNextCard;
        }

        /// <summary>Returns the right branch target for this card.</summary>
        public CardData ResolveRightNextCard()
        {
            return isLlmReactionCard ? continueNextCard : rightNextCard;
        }

        /// <summary>Returns true when the card has no valid outgoing links.</summary>
        public bool IsEnding => isLlmReactionCard
            ? continueNextCard == null
            : leftNextCard == null && rightNextCard == null;
    }
}