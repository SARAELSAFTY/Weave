using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Narrative
{
    /// <summary>Resolved labels and routing for the card currently on screen, after flags remap locked choices.</summary>
    public readonly struct CardPresentation
    {
        public readonly string Description;
        public readonly string LeftLabel;
        public readonly string RightLabel;
        public readonly string MiddleHint;
        public readonly bool ShowMiddle;
        public readonly ResourceChange LeftChange;
        public readonly ResourceChange RightChange;
        public readonly ResourceChange MiddleChange;
        public readonly CardData LeftNext;
        public readonly CardData RightNext;
        public readonly CardData MiddleNext;
        public readonly StoryFlags LeftSetFlags;
        public readonly StoryFlags RightSetFlags;
        public readonly StoryFlags MiddleSetFlags;

        public CardPresentation(
            string description,
            string leftLabel,
            string rightLabel,
            string middleHint,
            bool showMiddle,
            ResourceChange leftChange,
            ResourceChange rightChange,
            ResourceChange middleChange,
            CardData leftNext,
            CardData rightNext,
            CardData middleNext,
            StoryFlags leftSetFlags,
            StoryFlags rightSetFlags,
            StoryFlags middleSetFlags)
        {
            Description = description;
            LeftLabel = leftLabel;
            RightLabel = rightLabel;
            MiddleHint = middleHint;
            ShowMiddle = showMiddle;
            LeftChange = leftChange;
            RightChange = rightChange;
            MiddleChange = middleChange;
            LeftNext = leftNext;
            RightNext = rightNext;
            MiddleNext = middleNext;
            LeftSetFlags = leftSetFlags;
            RightSetFlags = rightSetFlags;
            MiddleSetFlags = middleSetFlags;
        }

        /// <summary>Builds presentation for a card, hiding flag-locked choices and surfacing resource tags.</summary>
        public static CardPresentation Resolve(
            CardData card,
            StoryFlagState flags,
            GameLanguage language,
            string swipeDownHint)
        {
            if (card == null)
            {
                return default;
            }

            string description = card.GetDescription(language);
            if (card.UsesAlternateDescription(flags != null ? flags.Value : StoryFlags.None)
                && !card.alternateDescriptionLocalized.IsEmpty)
            {
                description = card.alternateDescriptionLocalized.Get(language);
            }

            bool leftUnlocked = ChoiceUnlocked(card.leftRequiresFlags, flags);
            bool rightUnlocked = ChoiceUnlocked(card.rightRequiresFlags, flags);
            bool middleExists = card.isThreeWayVerdict;
            bool middleUnlocked = middleExists && ChoiceUnlocked(card.middleRequiresFlags, flags);

            CardData middleNext = card.middleNextCard != null ? card.middleNextCard : card.continueNextCard;
            ResourceChange middleChange = card.middleResourceChange;
            string middleLabel = card.GetMiddleChoice(language);
            StoryFlags middleSet = card.middleSetFlags;

            string leftLabel = card.GetLeftChoice(language);
            ResourceChange leftChange = card.leftResourceChange;
            CardData leftNext = card.leftNextCard;
            StoryFlags leftSet = card.leftSetFlags;

            string rightLabel = card.GetRightChoice(language);
            ResourceChange rightChange = card.rightResourceChange;
            CardData rightNext = card.rightNextCard;
            StoryFlags rightSet = card.rightSetFlags;

            // If the brother option is locked, promote abdication onto the left swipe so the
            // player still has two real choices instead of a dead left commit.
            if (!leftUnlocked && middleUnlocked)
            {
                leftLabel = middleLabel;
                leftChange = middleChange;
                leftNext = middleNext;
                leftSet = middleSet;
                leftUnlocked = true;
                middleUnlocked = false;
            }

            if (!leftUnlocked)
            {
                leftLabel = string.Empty;
                leftNext = null;
            }

            if (!rightUnlocked)
            {
                rightLabel = string.Empty;
                rightNext = null;
            }

            bool showMiddle = middleUnlocked && middleNext != null && !string.IsNullOrWhiteSpace(middleLabel);
            string middleHint = string.Empty;
            if (showMiddle)
            {
                string taggedMiddle = ResourceChangeFormatter.WithTags(middleLabel, middleChange, language);
                middleHint = string.IsNullOrWhiteSpace(swipeDownHint)
                    ? taggedMiddle
                    : swipeDownHint + " " + taggedMiddle;
                description = string.IsNullOrEmpty(description)
                    ? middleHint
                    : description + "\n\n" + middleHint;
            }

            return new CardPresentation(
                description,
                ResourceChangeFormatter.WithTags(leftLabel, leftChange, language),
                ResourceChangeFormatter.WithTags(rightLabel, rightChange, language),
                middleHint,
                showMiddle,
                leftChange,
                rightChange,
                middleChange,
                leftNext,
                rightNext,
                showMiddle ? middleNext : null,
                leftSet,
                rightSet,
                showMiddle ? middleSet : StoryFlags.None);
        }

        private static bool ChoiceUnlocked(StoryFlags required, StoryFlagState flags)
        {
            if (required == StoryFlags.None)
            {
                return true;
            }

            return flags != null && flags.Has(required);
        }
    }
}
