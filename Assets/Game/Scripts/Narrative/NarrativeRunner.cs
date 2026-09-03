using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Narrative
{
    /// <summary>Immutable result of a single narrative step, carrying either the next card, an error, or a collapse ending.</summary>
    public readonly struct NarrativeStepResult
    {
        /// <summary>The ending card to display when the run has concluded; null if the run continues.</summary>
        public readonly CardData endingCard;

        /// <summary>A human-readable error message when the step could not resolve; null or empty on success.</summary>
        public readonly string error;

        /// <summary>True when the ending was triggered by a resource dropping to or below its collapse threshold.</summary>
        public readonly bool isCollapseEnding;

        /// <summary>The resource whose collapse threshold was breached; only meaningful when <see cref="isCollapseEnding"/> is true.</summary>
        public readonly ResourceData collapsedResource;

        /// <summary>True when <see cref="error"/> contains a non-empty message.</summary>
        public bool HasError => !string.IsNullOrEmpty(error);

        /// <summary>True when an ending card is present and should be displayed.</summary>
        public bool HasEnded => endingCard != null;

        /// <summary>Constructs a step result with optional collapse-ending metadata.</summary>
        /// <param name="endingCard">The ending card, or null if the run continues.</param>
        /// <param name="error">An error message, or null/empty on success.</param>
        /// <param name="isCollapseEnding">Whether this ending was caused by resource collapse.</param>
        /// <param name="collapsedResource">The resource that collapsed, if applicable.</param>
        public NarrativeStepResult(CardData endingCard, string error, bool isCollapseEnding = false, ResourceData collapsedResource = null)
        {
            this.endingCard = endingCard;
            this.error = error;
            this.isCollapseEnding = isCollapseEnding;
            this.collapsedResource = collapsedResource;
        }
    }

    /// <summary>Drives the narrative card sequence by resolving choices, applying resource changes, advancing days, and detecting collapse endings.</summary>
    /// <remarks>Non-MonoBehaviour; constructed with a <see cref="NarrativeDatabase"/> and <see cref="ResourceState"/>. Call <see cref="StartRun"/> before <see cref="Choose"/>.</remarks>
    public class NarrativeRunner
    {
        private readonly NarrativeDatabase database;
        private readonly ResourceState resourceState;
        private readonly ResourceCatalog resourceCatalog;

        /// <summary>The card currently being presented to the player.</summary>
        public CardData CurrentCard { get; private set; }

        /// <summary>The current in-game day, starting at 1 and incremented by each card's dayAdvance value.</summary>
        public int Day { get; private set; } = 1;

        /// <summary>Raised after <see cref="Day"/> is incremented by a positive dayAdvance value.</summary>
        public event Action DayChanged;

        /// <summary>Creates a runner bound to the given database and resource state.</summary>
        /// <param name="database">The narrative database providing cards and the resource catalog.</param>
        /// <param name="resourceState">The mutable resource values modified by card choices.</param>
        public NarrativeRunner(NarrativeDatabase database, ResourceState resourceState)
        {
            this.database = database;
            this.resourceState = resourceState;
            resourceCatalog = database != null ? database.resourceCatalog : null;
        }

        /// <summary>Validates the database and sets <see cref="CurrentCard"/> to the starting card.</summary>
        /// <param name="error">Receives a validation error message on failure; null on success.</param>
        /// <returns>True if the run started successfully.</returns>
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

        /// <summary>Resolves a player choice on the current card, applies resource changes, advances the day, and checks for collapse endings.</summary>
        /// <param name="choseRight">True for the right branch, false for the left branch. Ignored when the card uses a continue exit.</param>
        /// <returns>A <see cref="NarrativeStepResult"/> describing the outcome of this step.</returns>
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
