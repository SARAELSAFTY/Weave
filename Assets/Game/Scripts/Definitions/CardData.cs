using UnityEngine;

namespace Game.Scripts.Definitions
{
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

        /// <summary>Author-facing ID for tooling. Falls back to the Unity asset name.</summary>
        public string AssetName => !string.IsNullOrWhiteSpace(assetName) ? assetName.Trim() : name;

        /// <summary>Player-facing label. Falls back to <see cref="AssetName"/> when unset.</summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(displayName) ? displayName.Trim() : AssetName;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(assetName)) return;
            DefinitionAssetRenamer.ScheduleRenameToMatch(this, assetName.Trim());
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

        [Header("Petition Card")]
        [Tooltip("Marks this card as a player petition card requiring free-form text input.")]
        public bool isPetitionCard;

        [TextArea(3, 6), Tooltip("Seed prompt giving the AI context on what kind of petitions this card expects.")]
        public string petitionSeedPrompt;

        [Tooltip("Card to advance to upon swipe/resolution (used when isLlmReactionCard or isPetitionCard is true).")]
        public CardData continueNextCard;

        /// <summary>True when routing uses <see cref="continueNextCard"/> instead of left/right branches.</summary>
        public bool UsesContinueExit => isLlmReactionCard || isPetitionCard;

        public CardData ResolveLeftNextCard()
        {
            return UsesContinueExit ? continueNextCard : leftNextCard;
        }

        public CardData ResolveRightNextCard()
        {
            return UsesContinueExit ? continueNextCard : rightNextCard;
        }

        /// <summary>True when there is no valid outgoing link (terminal card).</summary>
        public bool IsEnding => UsesContinueExit
            ? continueNextCard == null
            : leftNextCard == null && rightNextCard == null;
    }
}
