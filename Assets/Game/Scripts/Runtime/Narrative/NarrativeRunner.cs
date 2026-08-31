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
        public readonly ResourceData collapsedResource;

        public bool HasError => !string.IsNullOrEmpty(error);
        public bool HasEnded => endingCard != null;

        public NarrativeStepResult(CardData endingCard, string error, bool isCollapseEnding = false, ResourceData collapsedResource = null)
        {
            this.endingCard = endingCard;
            this.error = error;
            this.isCollapseEnding = isCollapseEnding;
            this.collapsedResource = collapsedResource;
        }
    }

    /// <summary>Advances cards, applies resource changes, and tracks day progression.</summary>
    public class NarrativeRunner
    {
        private readonly NarrativeDatabase database;
        private readonly ResourceState resourceState;
        private readonly ResourceCatalog resourceCatalog;

        public CardData CurrentCard { get; private set; }
        public int Day { get; private set; } = 1;

        public event Action DayChanged;

        public NarrativeRunner(NarrativeDatabase database, ResourceState resourceState)
        {
            this.database = database;
            this.resourceState = resourceState;
            resourceCatalog = database != null ? database.resourceCatalog : null;
        }

        public bool StartRun(out string error)
        {
            error = null;
            if (database == null || resourceState == null)
            {
                error = "NarrativeRunner needs a NarrativeDatabase and ResourceState.";
                return false;
            }

            if (!ValidateCards(out error))
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

            bool usesContinueExit = CurrentCard.UsesContinueExit;

            CardData nextCard = usesContinueExit
                ? CurrentCard.continueNextCard
                : (choseRight ? CurrentCard.rightNextCard : CurrentCard.leftNextCard);

            if (!usesContinueExit)
            {
                ResourceChange change = choseRight ? CurrentCard.rightResourceChange : CurrentCard.leftResourceChange;
                resourceState.Apply(change);
            }

            if (TryGetCollapsedResourceEnding(out CardData collapseCard, out ResourceData collapsedResource))
            {
                AdvanceDay(CurrentCard.dayAdvance);
                return new NarrativeStepResult(collapseCard, null, isCollapseEnding: true, collapsedResource);
            }

            AdvanceDay(CurrentCard.dayAdvance);

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

        private bool TryGetCollapsedResourceEnding(out CardData collapseCard, out ResourceData collapsedResource)
        {
            collapseCard = null;
            collapsedResource = null;
            if (resourceCatalog?.resources == null)
            {
                return false;
            }

            foreach (ResourceData resource in resourceCatalog.resources)
            {
                if (resource != null && resourceState.Get(resource) <= resource.collapseThreshold)
                {
                    collapsedResource = resource;
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

        private void AdvanceDay(int dayAdvance)
        {
            if (dayAdvance <= 0)
            {
                return;
            }

            Day += dayAdvance;
            DayChanged?.Invoke();
        }

        private bool ValidateCards(out string error)
        {
            error = null;
            if (database.cards == null)
            {
                return true;
            }

            List<string> brokenBranchCards = new List<string>();

            foreach (CardData card in database.cards)
            {
                if (card == null)
                {
                    continue;
                }

                if (card.HasBrokenBranch)
                {
                    brokenBranchCards.Add(card.AssetName);
                }
            }

            if (brokenBranchCards.Count > 0)
            {
                error = $"Choice cards must link both branches or end the card. Broken on: {string.Join(", ", brokenBranchCards)}.";
                return false;
            }

            return true;
        }
    }
}
