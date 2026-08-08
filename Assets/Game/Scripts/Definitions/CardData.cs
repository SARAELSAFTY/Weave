using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Defines authored content and outcomes for one narrative card.</summary>
    [CreateAssetMenu(fileName = "NewCard", menuName = "Weave/Card Data", order = 0)]
    public class CardData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique ID for this card.")]
        public string cardId;

        [Header("Card Content")]
        [TextArea(3, 6), Tooltip("Main story text shown on card.")]
        public string description;

        [Tooltip("ID of character speaking this card. Required - NarrativeRunner.StartRun fails if any card has none.")]
        public string speakerId;

        [Min(0), Tooltip("Days time advances when played.")]
        public int dayAdvance = 1;

        [Header("Left Choice")]
        [Tooltip("Text shown when swiping left.")]
        public string leftChoiceText;

        [Tooltip("Resource changes when choosing left.")]
        public ResourceChange leftResourceChange;

        [Tooltip("Card ID to load when choosing left.")]
        public string leftNextCardId;

        [Header("Right Choice")]
        [Tooltip("Text shown when swiping right.")]
        public string rightChoiceText;

        [Tooltip("Resource changes when choosing right.")]
        public ResourceChange rightResourceChange;

        [Tooltip("Card ID to load when choosing right.")]
        public string rightNextCardId;

        [Header("LLM Reaction Card")]
        [Tooltip("Marks this card as an LLM-generated reaction card.")]
        public bool isLlmReactionCard;

        [TextArea(3, 6), Tooltip("Seed prompt instructing the AI on how to react (e.g. 'React with suspicion to the player's recent choices').")]
        public string llmPromptSeed;

        [Tooltip("Card ID to advance to upon swipe (used when isLlmReactionCard is true).")]
        public string continueNextCardId;

        /// <summary>Returns the left branch target for this card.</summary>
        public string ResolveLeftNextCardId()
        {
            return isLlmReactionCard ? continueNextCardId : leftNextCardId;
        }

        /// <summary>Returns the right branch target for this card.</summary>
        public string ResolveRightNextCardId()
        {
            return isLlmReactionCard ? continueNextCardId : rightNextCardId;
        }

        /// <summary>Returns true when the card has no valid outgoing links.</summary>
        public bool IsEnding => isLlmReactionCard
            ? string.IsNullOrEmpty(continueNextCardId)
            : string.IsNullOrEmpty(leftNextCardId) && string.IsNullOrEmpty(rightNextCardId);
    }
}