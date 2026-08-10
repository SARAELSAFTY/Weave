using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Represents the result of applying one narrative choice.</summary>
    public readonly struct NarrativeStepResult
    {
        public readonly CardData endingCard;
        public readonly string error;
        public readonly bool isCollapseEnding;

        /// <summary>Gets whether the step failed with an error.</summary>
        public bool HasError => !string.IsNullOrEmpty(error);

        /// <summary>Gets whether the step reached an ending card.</summary>
        public bool HasEnded => endingCard != null;

        /// <summary>Creates a step result value.</summary>
        public NarrativeStepResult(CardData endingCard, string error, bool isCollapseEnding = false)
        {
            this.endingCard = endingCard;
            this.error = error;
            this.isCollapseEnding = isCollapseEnding;
        }
    }

    /// <summary>Runs card progression, resource updates, and day progression.</summary>
    public class NarrativeRunner
    {
        private readonly NarrativeDatabase database;
        private readonly ResourceState resourceState;
        private readonly ResourceCatalog resourceCatalog;
        private readonly NarrativeState state = new NarrativeState();

        /// <summary>Gets the card currently shown to the player.</summary>
        public CardData CurrentCard { get; private set; }

        /// <summary>Gets the current in-game day.</summary>
        public int Day => state.Day;

        /// <summary>Raised after the in-game day changes.</summary>
        public event Action DayChanged
        {
            add => state.DayChanged += value;
            remove => state.DayChanged -= value;
        }

        /// <summary>Creates a narrative runner for one database, resource state, and catalog.</summary>
        public NarrativeRunner(NarrativeDatabase database, ResourceState resourceState, ResourceCatalog resourceCatalog = null)
        {
            this.database = database;
            this.resourceState = resourceState;
            this.resourceCatalog = resourceCatalog ?? (database != null ? database.resourceCatalog : null);
        }

        /// <summary>Validates setup and moves the runner to the starting card.</summary>
        public bool StartRun(out string error)
        {
            error = null;
            if (database == null || resourceState == null)
            {
                error = "NarrativeRunner needs a NarrativeDatabase and ResourceState.";
                return false;
            }

            if (!ValidateSpeakers(out error))
            {
                return false;
            }

            if (database.startingCard == null)
            {
                error = "NarrativeDatabase does not have a Starting Card assigned.";
                return false;
            }

            CurrentCard = database.startingCard;
            return true;
        }

        /// <summary>Applies one player choice and advances narrative state.</summary>
        public NarrativeStepResult Choose(bool choseRight)
        {
            if (CurrentCard == null)
            {
                return new NarrativeStepResult(null, "No current card is available.");
            }

            if (CurrentCard.IsEnding)
            {
                return new NarrativeStepResult(CurrentCard, null);
            }

            CardData nextCard = choseRight
                ? CurrentCard.ResolveRightNextCard()
                : CurrentCard.ResolveLeftNextCard();

            if (!CurrentCard.isLlmReactionCard)
            {
                ResourceChange change = choseRight ? CurrentCard.rightResourceChange : CurrentCard.leftResourceChange;
                resourceState.Apply(change);

                if (TryGetCollapsedResourceEnding(out CardData collapseCard))
                {
                    state.Advance(CurrentCard.dayAdvance);
                    return new NarrativeStepResult(collapseCard, null, isCollapseEnding: true);
                }
            }

            state.Advance(CurrentCard.dayAdvance);

            if (nextCard == null)
            {
                return new NarrativeStepResult(null, $"'{CurrentCard.AssetName}' has no valid next card to show.");
            }

            if (nextCard.IsEnding)
            {
                return new NarrativeStepResult(nextCard, null);
            }

            CurrentCard = nextCard;
            return new NarrativeStepResult(null, null);
        }

        private bool TryGetCollapsedResourceEnding(out CardData collapseCard)
        {
            collapseCard = null;
            if (resourceCatalog?.resources == null)
            {
                return false;
            }

            foreach (ResourceData resource in resourceCatalog.resources)
            {
                if (resource != null && resource.collapseEndingCard != null && resourceState.Get(resource) <= 0)
                {
                    collapseCard = resource.collapseEndingCard;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Gets speaker data for a card's speaker, or null when not assigned.</summary>
        public SpeakerData GetSpeaker(CardData card)
        {
            return card != null ? card.speaker : null;
        }

        private bool ValidateSpeakers(out string error)
        {
            error = null;
            if (database.cards == null)
            {
                return true;
            }

            List<string> missingSpeakerCards = new List<string>();

            foreach (CardData card in database.cards)
            {
                if (card != null && card.speaker == null)
                {
                    missingSpeakerCards.Add(card.AssetName);
                }
            }

            if (missingSpeakerCards.Count > 0)
            {
                error = $"Every card must have a speaker. Missing on: {string.Join(", ", missingSpeakerCards)}.";
                return false;
            }

            return true;
        }
    }
}
