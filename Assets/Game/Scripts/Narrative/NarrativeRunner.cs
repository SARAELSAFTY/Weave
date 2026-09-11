using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
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

        /// <summary>True when the run has concluded: an authored ending card is present, or a resource collapsed
        /// and the ending card is generated at runtime from the collapsed resource.</summary>
        public bool HasEnded => endingCard != null || isCollapseEnding;

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
        private readonly StoryFlagState storyFlags = new StoryFlagState();
        private CardPresentation currentPresentation;

        /// <summary>The card currently being presented to the player.</summary>
        public CardData CurrentCard { get; private set; }

        /// <summary>Flags set so far this reign.</summary>
        public StoryFlagState Flags => storyFlags;

        /// <summary>Resolved labels and routing for <see cref="CurrentCard"/> after flag remaps.</summary>
        public CardPresentation CurrentPresentation => currentPresentation;

        /// <summary>When set, the next <see cref="Choose"/> uses this card instead of the usual exit. Cleared after use.</summary>
        public CardData PendingNextCard { get; set; }

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
        /// <param name="error">Receives a validation error message on failure; null on success.</summary>
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

            storyFlags.Reset();
            PendingNextCard = null;
            SetCurrentCard(database.startingCard);
            return true;
        }

        /// <summary>Rebuilds <see cref="CurrentPresentation"/> for the current language (call after language changes).</summary>
        public void RefreshPresentation(GameLanguage language, string swipeDownHint)
        {
            currentPresentation = CardPresentation.Resolve(CurrentCard, storyFlags, language, swipeDownHint);
        }

        /// <summary>Resolves a player choice on the current card, applies resource changes, advances the day, and checks for collapse endings.</summary>
        public NarrativeStepResult Choose(CardChoice choice)
        {
            return Choose(choice, applyChoiceResources: true);
        }

        /// <summary>Resolves the current card using <see cref="PendingNextCard"/> or the continue exit, without applying left/right deltas.</summary>
        public NarrativeStepResult ContinueWithoutChoiceResources()
        {
            return Choose(CardChoice.Right, applyChoiceResources: false);
        }

        private NarrativeStepResult Choose(CardChoice choice, bool applyChoiceResources)
        {
            if (CurrentCard == null)
            {
                return new NarrativeStepResult(null, "No current card is available.");
            }

            if (CurrentCard.IsEnding)
            {
                return new NarrativeStepResult(CurrentCard, null);
            }

            CardData card = CurrentCard;
            CardData nextCard;
            StoryFlags setFlags = StoryFlags.None;

            if (PendingNextCard != null)
            {
                nextCard = PendingNextCard;
                PendingNextCard = null;
                setFlags = card.continueSetFlags;
            }
            else if (card.UsesContinueExit)
            {
                nextCard = card.continueNextCard;
                setFlags = card.continueSetFlags;
            }
            else
            {
                CardPresentation presentation = currentPresentation.LeftNext != null || currentPresentation.RightNext != null
                    ? currentPresentation
                    : CardPresentation.Resolve(card, storyFlags, GameLanguage.English, string.Empty);

                if (choice == CardChoice.Middle)
                {
                    if (!presentation.ShowMiddle || presentation.MiddleNext == null)
                    {
                        return new NarrativeStepResult(null, "No middle choice is available on this card.");
                    }

                    nextCard = presentation.MiddleNext;
                    setFlags = presentation.MiddleSetFlags;
                    if (applyChoiceResources)
                    {
                        resourceState.Apply(presentation.MiddleChange);
                    }
                }
                else if (choice == CardChoice.Right)
                {
                    nextCard = presentation.RightNext;
                    setFlags = presentation.RightSetFlags;
                    if (applyChoiceResources)
                    {
                        resourceState.Apply(presentation.RightChange);
                    }
                }
                else
                {
                    nextCard = presentation.LeftNext;
                    setFlags = presentation.LeftSetFlags;
                    if (applyChoiceResources)
                    {
                        resourceState.Apply(presentation.LeftChange);
                    }
                }
            }

            storyFlags.Set(setFlags);

            bool nextIsAuthoredEnding = nextCard != null && nextCard.IsEnding;

            if (!nextIsAuthoredEnding && TryGetCollapsedResource(out ResourceData collapsedResource))
            {
                AdvanceDay(card.dayAdvance);
                return new NarrativeStepResult(null, null, isCollapseEnding: true, collapsedResource);
            }

            AdvanceDay(card.dayAdvance);

            if (nextCard == null)
            {
                return new NarrativeStepResult(null, $"'{card.AssetName}' has no valid next card to show.");
            }

            nextCard = ResolveEntry(nextCard);

            if (nextCard == null)
            {
                return new NarrativeStepResult(null, $"'{card.AssetName}' skipped to an empty gate.");
            }

            if (nextCard.isEndingEvaluator)
            {
                CardData ending = EndingEvaluator.Select(nextCard, resourceState, resourceCatalog, storyFlags);
                AdvanceDay(nextCard.dayAdvance);
                if (ending == null)
                {
                    return new NarrativeStepResult(null, $"'{nextCard.AssetName}' evaluator has no wired endings.");
                }

                return new NarrativeStepResult(ending, null);
            }

            if (nextCard.IsEnding)
            {
                return new NarrativeStepResult(nextCard, null);
            }

            SetCurrentCard(nextCard);
            return new NarrativeStepResult(null, null);
        }

        private void SetCurrentCard(CardData card)
        {
            CurrentCard = ResolveEntry(card);
            currentPresentation = CardPresentation.Resolve(CurrentCard, storyFlags, GameLanguage.English, string.Empty);
        }

        /// <summary>Walks skip gates until a playable card remains, or null if the chain is empty.</summary>
        public CardData ResolveEntry(CardData card)
        {
            int guard = 0;
            while (card != null && !PassesEntry(card) && guard++ < 8)
            {
                card = PickSkipTarget(card);
            }

            return card;
        }

        private bool PassesEntry(CardData card)
        {
            if (card == null)
            {
                return false;
            }

            if (card.entryRequiresFlags != StoryFlags.None && !storyFlags.Has(card.entryRequiresFlags))
            {
                return false;
            }

            if (!card.hasResourceGate || card.gateResource == null)
            {
                return true;
            }

            int value = resourceState.Get(card.gateResource);
            return value >= card.gateMinInclusive && value <= card.gateMaxInclusive;
        }

        private CardData PickSkipTarget(CardData card)
        {
            if (card.skipToCard != null && card.skipToAltCard != null && resourceCatalog != null)
            {
                int army = GetNamed("Army");
                int gold = GetNamed("Gold");
                return gold > army ? card.skipToAltCard : card.skipToCard;
            }

            return card.skipToCard != null ? card.skipToCard : card.skipToAltCard;
        }

        private int GetNamed(string assetName)
        {
            ResourceData resource = resourceCatalog != null ? resourceCatalog.FindByAssetName(assetName) : null;
            return resource != null ? resourceState.Get(resource) : 0;
        }

        private bool TryGetCollapsedResource(out ResourceData collapsedResource)
        {
            collapsedResource = null;
            if (resourceCatalog?.resources == null)
            {
                Debug.LogWarning("[NarrativeRunner] TryGetCollapsedResource called with null resource catalog.");
                return false;
            }

            foreach (ResourceData resource in resourceCatalog.resources)
            {
                if (resource == null)
                {
                    continue;
                }

                if (resourceState.Get(resource) <= resource.collapseThreshold)
                {
                    // GameManager generates the runtime collapse ending or falls back to the
                    // text stored on the resource; authored ending transitions are exempted
                    // in Choose before this check runs.
                    collapsedResource = resource;
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
