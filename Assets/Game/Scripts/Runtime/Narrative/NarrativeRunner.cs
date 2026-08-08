using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEngine;

namespace Game.Scripts.Runtime.Narrative
{
    /// <summary>Represents the result of applying one narrative choice.</summary>
    public readonly struct NarrativeStepResult
    {
        public readonly CardData endingCard;
        public readonly string error;

        /// <summary>Gets whether the step failed with an error.</summary>
        public bool HasError => !string.IsNullOrEmpty(error);

        /// <summary>Gets whether the step reached an ending card.</summary>
        public bool HasEnded => endingCard != null;

        /// <summary>Creates a step result value.</summary>
        public NarrativeStepResult(CardData endingCard, string error)
        {
            this.endingCard = endingCard;
            this.error = error;
        }
    }

    /// <summary>Runs card progression, resource updates, and day progression.</summary>
    public class NarrativeRunner
    {
        private readonly NarrativeDatabase database;
        private readonly ResourceState resourceState;
        private readonly Dictionary<string, CardData> cardsById = new Dictionary<string, CardData>();
        private readonly Dictionary<string, CouncilMemberData> speakersById = new Dictionary<string, CouncilMemberData>();
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

        /// <summary>Creates a narrative runner for one database and resource state.</summary>
        public NarrativeRunner(NarrativeDatabase database, ResourceState resourceState)
        {
            this.database = database;
            this.resourceState = resourceState;
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

            cardsById.Clear();
            Dictionary<string, CardData> cards = CardGraph.BuildLookup(database, Debug.LogWarning);
            foreach (KeyValuePair<string, CardData> pair in cards)
            {
                cardsById[pair.Key] = pair.Value;
            }

            speakersById.Clear();
            Dictionary<string, CouncilMemberData> speakers = CardGraph.BuildSpeakerLookup(database, Debug.LogWarning);
            foreach (KeyValuePair<string, CouncilMemberData> pair in speakers)
            {
                speakersById[pair.Key] = pair.Value;
            }

            if (!ValidateSpeakers(out error))
            {
                return false;
            }

            if (!cardsById.TryGetValue(database.startingCardId, out CardData startCard))
            {
                error = $"Starting Card ID '{database.startingCardId}' does not resolve to a card.";
                return false;
            }

            CurrentCard = startCard;
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

            string selectedNextCardId = choseRight
                ? CurrentCard.ResolveRightNextCardId()
                : CurrentCard.ResolveLeftNextCardId();

            if (!CurrentCard.isLlmReactionCard)
            {
                ResourceChange change = choseRight ? CurrentCard.rightResourceChange : CurrentCard.leftResourceChange;
                resourceState.Apply(change);
            }

            state.Advance(CurrentCard.dayAdvance);

            if (string.IsNullOrEmpty(selectedNextCardId) || !cardsById.TryGetValue(selectedNextCardId, out CardData nextCard))
            {
                return new NarrativeStepResult(null, $"'{CurrentCard.cardId}' has no valid next card to show.");
            }

            if (nextCard.IsEnding)
            {
                return new NarrativeStepResult(nextCard, null);
            }

            CurrentCard = nextCard;
            return new NarrativeStepResult(null, null);
        }

        /// <summary>Gets speaker data for a speaker ID, or null when not found.</summary>
        public CouncilMemberData GetSpeaker(string speakerId)
        {
            return !string.IsNullOrEmpty(speakerId) && speakersById.TryGetValue(speakerId, out CouncilMemberData speaker)
                ? speaker
                : null;
        }

        private bool ValidateSpeakers(out string error)
        {
            error = null;
            List<string> missingSpeakerCards = new List<string>();

            foreach (CardData card in cardsById.Values)
            {
                if (string.IsNullOrEmpty(card.speakerId))
                {
                    missingSpeakerCards.Add(card.cardId);
                    continue;
                }

                CouncilMemberData speaker = GetSpeaker(card.speakerId);
                if (card.isLlmReactionCard && !CardGraph.IsLlmSpeaker(database, speaker))
                {
                    Debug.LogWarning($"Card '{card.cardId}' is an LLM reaction card but speaker '{card.speakerId}' is not the designated LLM Speaker in the Narrative Database.");
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
