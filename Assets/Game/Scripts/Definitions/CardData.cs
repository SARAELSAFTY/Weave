using Game.Scripts.Runtime.Llm;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    [CreateAssetMenu(fileName = "Scene_Speaker_Slug", menuName = "Weave/Card Data", order = 0)]
    public class CardData : NamedGameAsset
    {
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

        [TextArea(2, 4), Tooltip("Optional override for this card's reaction seed prompt. Leave empty to use " +
            "LlmPromptTemplates.defaultReactionSeedPrompt.")]
        public string reactionSeedOverride;

        [Header("Petition Card")]
        [Tooltip("Marks this card as a player petition card requiring free-form text input.")]
        public bool isPetitionCard;

        [TextArea(2, 4), Tooltip("Optional override for this card's petition seed prompt (used for the " +
            "opening announcement and every turn). Leave empty to use LlmPromptTemplates.defaultPetitionSeedPrompt.")]
        public string petitionSeedOverride;

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

        /// <summary>This card's reaction seed: its own override if set, else the global default.</summary>
        public string EffectiveReactionSeed(LlmPromptTemplates templates)
        {
            return !string.IsNullOrWhiteSpace(reactionSeedOverride)
                ? reactionSeedOverride
                : (templates != null ? templates.defaultReactionSeedPrompt : string.Empty);
        }

        /// <summary>This card's petition seed: its own override if set, else the global default.</summary>
        public string EffectivePetitionSeed(LlmPromptTemplates templates)
        {
            return !string.IsNullOrWhiteSpace(petitionSeedOverride)
                ? petitionSeedOverride
                : (templates != null ? templates.defaultPetitionSeedPrompt : string.Empty);
        }
    }
}
