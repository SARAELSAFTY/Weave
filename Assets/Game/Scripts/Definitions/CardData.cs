using Game.Scripts.Localization;
using Game.Scripts.Llm;
using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Determines how the petitioner persona is sourced for petition cards.</summary>
    public enum PetitionerSource
    {
        /// <summary>Generate a temporary commoner persona for the petition text.</summary>
        GeneratedCommoner = 0,
        /// <summary>Use the card's assigned <see cref="CardData.speaker"/> as the petitioner.</summary>
        DefinedSpeaker = 1
    }

    /// <summary>Selects which image source is displayed on the card face.</summary>
    public enum CardArtMode
    {
        /// <summary>Show the speaker's portrait sprite.</summary>
        SpeakerPortrait = 0,
        /// <summary>Show a custom event image assigned via <see cref="CardData.cardImage"/>.</summary>
        EventImage = 1,
        /// <summary>No card image overlay.</summary>
        None = 2
    }

    /// <summary>A single narrative card defining speaker, choices, resource changes, and branching logic.</summary>
    /// <remarks>Cards may be standard (left/right choices), LLM reaction cards, petition cards, or free chat cards. The exit mode is determined by <see cref="UsesContinueExit"/>.</remarks>
    [CreateAssetMenu(fileName = "Scene_Speaker_Slug", menuName = "Weave/Card Data", order = 0)]
    public class CardData : NamedGameAsset
    {
        [Header("Speaker & Description")]
        [Tooltip("The character presenting this card; null for event cards with no speaker.")]
        public SpeakerData speaker;

        [Tooltip("Narrative description displayed on the card, resolved per language.")]
        public LocalizedText descriptionLocalized;

        [Header("Timing")]
        [Min(0)]
        [Tooltip("Number of in-game days that advance when this card is resolved.")]
        public int dayAdvance = 1;

        [Header("Left Choice")]
        [Tooltip("Label text for the left choice button, resolved per language.")]
        public LocalizedText leftChoiceLocalized;

        [Tooltip("Resource deltas applied when the player picks the left choice.")]
        public ResourceChange leftResourceChange;

        [Tooltip("Card shown after the left choice; null means this branch ends.")]
        public CardData leftNextCard;

        [Header("Right Choice")]
        [Tooltip("Label text for the right choice button, resolved per language.")]
        public LocalizedText rightChoiceLocalized;

        [Tooltip("Resource deltas applied when the player picks the right choice.")]
        public ResourceChange rightResourceChange;

        [Tooltip("Card shown after the right choice; null means this branch ends.")]
        public CardData rightNextCard;

        [Header("Middle Choice (three-way verdict)")]
        [Tooltip("Label for the down-swipe / middle option on three-way cards.")]
        public LocalizedText middleChoiceLocalized;

        [Tooltip("Resource deltas applied when the player picks the middle choice.")]
        public ResourceChange middleResourceChange;

        [Tooltip("Card shown after the middle choice. When null, continueNextCard is used.")]
        public CardData middleNextCard;

        [Tooltip("When true, left / down / right are all valid exits (Fool's Verdict).")]
        public bool isThreeWayVerdict;

        [Header("Ending Evaluator")]
        [Tooltip("When true, this card is never shown; the runner picks an ending from its wired targets.")]
        public bool isEndingEvaluator;

        [Header("Story Flags")]
        [Tooltip("Flags set when the player picks the left choice.")]
        public StoryFlags leftSetFlags;

        [Tooltip("Flags set when the player picks the right choice.")]
        public StoryFlags rightSetFlags;

        [Tooltip("Flags set when the player picks the middle choice.")]
        public StoryFlags middleSetFlags;

        [Tooltip("Flags set when this card is resolved via continue (reaction / petition / chat).")]
        public StoryFlags continueSetFlags;

        [Tooltip("Left choice is hidden unless these flags are set.")]
        public StoryFlags leftRequiresFlags;

        [Tooltip("Right choice is hidden unless these flags are set.")]
        public StoryFlags rightRequiresFlags;

        [Tooltip("Middle choice is hidden unless these flags are set.")]
        public StoryFlags middleRequiresFlags;

        [Tooltip("When these flags match (see Alternate If Missing), use alternateDescriptionLocalized.")]
        public StoryFlags alternateDescriptionIfFlags;

        [Tooltip("When true, show the alternate description if the flags above are NOT all set.")]
        public bool alternateDescriptionIfMissing;

        [Tooltip("Optional body text used instead of descriptionLocalized when the flag condition matches.")]
        public LocalizedText alternateDescriptionLocalized;

        [Header("Entry Gate")]
        [Tooltip("Card is skipped unless these flags are set.")]
        public StoryFlags entryRequiresFlags;

        [Tooltip("When true, gateResource must sit between gateMinInclusive and gateMaxInclusive or the card is skipped.")]
        public bool hasResourceGate;

        [Tooltip("Resource checked by the entry gate.")]
        public ResourceData gateResource;

        [Tooltip("Inclusive minimum for the gated resource.")]
        public int gateMinInclusive;

        [Tooltip("Inclusive maximum for the gated resource.")]
        public int gateMaxInclusive = 100;

        [Tooltip("Card to show instead when the entry gate fails.")]
        public CardData skipToCard;

        [Tooltip("Alternate skip target; used when Gold is strictly greater than Army.")]
        public CardData skipToAltCard;

        [Header("LLM Reaction")]
        [Tooltip("When true, this card uses an LLM-generated reaction instead of fixed choices.")]
        public bool isLlmReactionCard;

        [TextArea(2, 4)]
        [Tooltip("Custom seed prompt overriding the default reaction template from LlmPromptTemplates.")]
        public string reactionSeedOverride;

        [Header("Petition")]
        [Tooltip("When true, this card presents a petition generated via LLM.")]
        public bool isPetitionCard;

        [TextArea(2, 4)]
        [Tooltip("Custom seed prompt overriding the default petition template from LlmPromptTemplates.")]
        public string petitionSeedOverride;

        [Tooltip("Whether the petitioner persona is a generated commoner or the card's defined speaker.")]
        public PetitionerSource petitionerSource = PetitionerSource.GeneratedCommoner;

        [Header("Chat")]
        [Tooltip("When true, this card opens a free multi-turn LLM chat with the speaker. It cannot change resources or history; the player ends it with the audience button.")]
        public bool isChatCard;

        [TextArea(2, 4)]
        [Tooltip("Custom seed prompt overriding the default chat template from LlmPromptTemplates.")]
        public string chatSeedOverride;

        [Header("Continue Exit")]
        [Tooltip("Card shown after the continue action on reaction/petition cards; null means the narrative ends.")]
        public CardData continueNextCard;

        [Header("Visuals")]
        [Tooltip("Selects which image source appears on the card face.")]
        public CardArtMode artMode = CardArtMode.SpeakerPortrait;

        [Tooltip("Custom image sprite used when Art Mode is Event Image.")]
        public Sprite cardImage;

        [Tooltip("Optional visual template providing background and border sprites for this card.")]
        public CardVisualTemplate visualTemplate;

        /// <summary>True when this card exits via the continue path (reaction, petition, or chat) instead of left/right choices.</summary>
        public bool UsesContinueExit => isLlmReactionCard || isPetitionCard || isChatCard;

        /// <summary>True when no next card is reachable from this card's active exit path.</summary>
        public bool IsEnding => isEndingEvaluator
            ? false
            : UsesContinueExit
                ? continueNextCard == null
                : leftNextCard == null && rightNextCard == null;

        /// <summary>True when this is a standard choice card with exactly one null branch, indicating incomplete authoring.</summary>
        public bool HasBrokenBranch => !UsesContinueExit && !IsEnding && !isEndingEvaluator && (leftNextCard == null || rightNextCard == null);

        /// <summary>Returns the localized card description for the given language.</summary>
        /// <param name="language">Target language.</param>
        public string GetDescription(GameLanguage language) => descriptionLocalized.Get(language);

        /// <summary>Returns the localized left-choice label for the given language.</summary>
        /// <param name="language">Target language.</param>
        public string GetLeftChoice(GameLanguage language) => leftChoiceLocalized.Get(language);

        /// <summary>Returns the localized right-choice label for the given language.</summary>
        /// <param name="language">Target language.</param>
        public string GetRightChoice(GameLanguage language) => rightChoiceLocalized.Get(language);

        /// <summary>Returns the localized middle-choice label for the given language.</summary>
        public string GetMiddleChoice(GameLanguage language) => middleChoiceLocalized.Get(language);

        /// <summary>True when alternateDescriptionLocalized should replace the main body for the given flags.</summary>
        public bool UsesAlternateDescription(StoryFlags currentFlags)
        {
            if (alternateDescriptionIfFlags == StoryFlags.None || alternateDescriptionLocalized.IsEmpty)
            {
                return false;
            }

            bool hasFlags = (currentFlags & alternateDescriptionIfFlags) == alternateDescriptionIfFlags;
            return alternateDescriptionIfMissing ? !hasFlags : hasFlags;
        }

        /// <summary>Returns the effective reaction seed prompt, using the override if set or falling back to the template default.</summary>
        /// <param name="templates">Prompt templates providing the default seed; may be null.</param>
        /// <returns>The override value when non-blank, otherwise the template default.</returns>
        public string EffectiveReactionSeed(LlmPromptTemplates templates)
        {
            return ResolveSeed(reactionSeedOverride, templates != null ? templates.defaultReactionSeedPrompt : string.Empty);
        }

        /// <summary>Returns the effective petition seed prompt, using the override if set or falling back to the template default.</summary>
        /// <param name="templates">Prompt templates providing the default seed; may be null.</param>
        /// <returns>The override value when non-blank, otherwise the template default.</returns>
        public string EffectivePetitionSeed(LlmPromptTemplates templates)
        {
            return ResolveSeed(petitionSeedOverride, templates != null ? templates.defaultPetitionSeedPrompt : string.Empty);
        }

        /// <summary>Returns the effective chat seed prompt, using the override if set or falling back to the template default.</summary>
        /// <param name="templates">Prompt templates providing the default seed; may be null.</param>
        /// <returns>The override value when non-blank, otherwise the template default.</returns>
        public string EffectiveChatSeed(LlmPromptTemplates templates)
        {
            return ResolveSeed(chatSeedOverride, templates != null ? templates.defaultChatSeedPrompt : string.Empty);
        }

        // Returns the per-card override when non-blank, otherwise falls back to the shared template default.
        private static string ResolveSeed(string overrideValue, string templateDefault) =>
            !string.IsNullOrWhiteSpace(overrideValue) ? overrideValue : templateDefault;
    }
}
