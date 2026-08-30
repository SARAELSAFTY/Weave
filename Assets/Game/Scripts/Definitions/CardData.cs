using Game.Scripts.Localization;
using Game.Scripts.Runtime.Llm;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>How a petition card chooses who brings the matter to the throne.</summary>
    public enum PetitionerSource
    {
        GeneratedCommoner = 0,
        DefinedSpeaker = 1
    }

    [CreateAssetMenu(fileName = "Scene_Speaker_Slug", menuName = "Weave/Card Data", order = 0)]
    public class CardData : NamedGameAsset
    {
        [Header("Speaker")]
        [Tooltip("Character speaking this card. Required for run execution.")]
        public SpeakerData speaker;

        [Header("Card Content")]
        [Tooltip("Main story text shown on card.")]
        public LocalizedText descriptionLocalized;

        [Min(0), Tooltip("Days time advances when played.")]
        public int dayAdvance = 1;

        [Header("Left Choice")]
        [Tooltip("Text shown when swiping left.")]
        public LocalizedText leftChoiceLocalized;

        [Tooltip("Resource changes when choosing left.")]
        public ResourceChange leftResourceChange;

        [Tooltip("Card loaded when choosing left.")]
        public CardData leftNextCard;

        [Header("Right Choice")]
        [Tooltip("Text shown when swiping right.")]
        public LocalizedText rightChoiceLocalized;

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

        [Tooltip("Who brings this petition. Generated Commoner (default) creates a temporary common subject with a " +
            "new name, trade, and problem each audience and discards it afterwards. Defined Speaker uses the Speaker " +
            "above and lets the AI craft a problem suited to that character.")]
        public PetitionerSource petitionerSource = PetitionerSource.GeneratedCommoner;

        [Tooltip("Card to advance to upon swipe/resolution (used when isLlmReactionCard or isPetitionCard is true).")]
        public CardData continueNextCard;

        /// <summary>True when routing uses <see cref="continueNextCard"/> instead of left/right branches.</summary>
        public bool UsesContinueExit => isLlmReactionCard || isPetitionCard;

        /// <summary>
        /// False only for petitions whose petitioner is generated on the fly; every other card must
        /// have a speaker for the run to execute.
        /// </summary>
        public bool RequiresSpeaker => !(isPetitionCard && petitionerSource == PetitionerSource.GeneratedCommoner);

        /// <summary>True when there is no valid outgoing link (terminal card).</summary>
        public bool IsEnding => UsesContinueExit
            ? continueNextCard == null
            : leftNextCard == null && rightNextCard == null;

        /// <summary>True when a choice card is missing exactly one branch - that swipe dead-ends the run.</summary>
        public bool HasBrokenBranch => !UsesContinueExit && !IsEnding && (leftNextCard == null || rightNextCard == null);

        public string GetDescription(GameLanguage language) => descriptionLocalized.Get(language);

        public string GetLeftChoice(GameLanguage language) => leftChoiceLocalized.Get(language);

        public string GetRightChoice(GameLanguage language) => rightChoiceLocalized.Get(language);

        /// <summary>This card's reaction seed: its own override if set, else the global default.</summary>
        public string EffectiveReactionSeed(LlmPromptTemplates templates)
        {
            return ResolveSeed(reactionSeedOverride, templates != null ? templates.defaultReactionSeedPrompt : string.Empty);
        }

        /// <summary>This card's petition seed: its own override if set, else the global default.</summary>
        public string EffectivePetitionSeed(LlmPromptTemplates templates)
        {
            return ResolveSeed(petitionSeedOverride, templates != null ? templates.defaultPetitionSeedPrompt : string.Empty);
        }

        private static string ResolveSeed(string overrideValue, string templateDefault) =>
            !string.IsNullOrWhiteSpace(overrideValue) ? overrideValue : templateDefault;
    }
}
