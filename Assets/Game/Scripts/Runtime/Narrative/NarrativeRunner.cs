using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    public readonly struct NarrativeStepResult
    {
        public readonly CardData endingCard;
        public readonly string error;
        public readonly bool isCollapseEnding;

        public bool HasError => !string.IsNullOrEmpty(error);
        public bool HasEnded => endingCard != null;

        public NarrativeStepResult(CardData endingCard, string error, bool isCollapseEnding = false)
        {
            this.endingCard = endingCard;
            this.error = error;
            this.isCollapseEnding = isCollapseEnding;
        }
    }

    /// <summary>Advances cards, applies resource changes, and tracks day progression.</summary>
    public class NarrativeRunner
    {
        private readonly NarrativeDatabase database;
        private readonly ResourceState resourceState;
        private readonly ResourceCatalog resourceCatalog;
        private readonly NarrativeState state = new NarrativeState();

        public CardData CurrentCard { get; private set; }
        public int Day => state.Day;

        public event Action DayChanged
        {
            add => state.DayChanged += value;
            remove => state.DayChanged -= value;
        }

        public NarrativeRunner(NarrativeDatabase database, ResourceState resourceState, ResourceCatalog resourceCatalog = null)
        {
            this.database = database;
            this.resourceState = resourceState;
            this.resourceCatalog = resourceCatalog ?? (database != null ? database.resourceCatalog : null);
        }

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

            if (!CurrentCard.isLlmReactionCard && !CurrentCard.isPetitionCard)
            {
                ResourceChange change = choseRight ? CurrentCard.rightResourceChange : CurrentCard.leftResourceChange;
                resourceState.Apply(change);
            }

            if (TryGetCollapsedResourceEnding(out CardData collapseCard))
            {
                state.Advance(CurrentCard.dayAdvance);
                return new NarrativeStepResult(collapseCard, null, isCollapseEnding: true);
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
                if (resource != null && resourceState.Get(resource) <= resource.collapseThreshold)
                {
                    collapseCard = resourceCatalog.GetCollapseEndingCard(resource);
                    if (collapseCard == null)
                    {
                        Debug.LogWarning($"[NarrativeRunner] '{resource.AssetName}' reached its collapse threshold but has no fallback collapse-ending CardData assigned. Add an entry to ResourceCatalog.collapseEndings.");
                    }
                    return true;
                }
            }

            return false;
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
