using System;
using System.Collections.Generic;
using UnityEngine;

// Result of a card swipe choice.
public readonly struct NarrativeStepResult
{
    public readonly CardData EndingCard;
    public readonly string Error;

    public bool HasError => !string.IsNullOrEmpty(Error);
    public bool HasEnded => EndingCard != null;

    public NarrativeStepResult(CardData endingCard, string error)
    {
        EndingCard = endingCard;
        Error = error;
    }
}

// Drives card progression, applying choice outcomes and resolving next cards.
public class NarrativeRunner
{
    private readonly NarrativeDatabase database;
    private readonly ResourceState resourceState;
    private readonly Dictionary<string, CardData> cardsById = new Dictionary<string, CardData>();
    private readonly Dictionary<string, CouncilMemberData> speakersById = new Dictionary<string, CouncilMemberData>();
    private readonly NarrativeState state = new NarrativeState();
    private CardChainRunner chainRunner;

    public CardData CurrentCard { get; private set; }
    public int Day => state.Day;

    public event Action DayChanged
    {
        add => state.DayChanged += value;
        remove => state.DayChanged -= value;
    }

    public NarrativeRunner(NarrativeDatabase database, ResourceState resourceState)
    {
        this.database = database;
        this.resourceState = resourceState;
    }

    // Prepares lookups and sets the starting card for the narrative run.
    public bool StartRun(out string error)
    {
        error = null;
        if (database == null || resourceState == null)
        {
            error = "NarrativeRunner needs a NarrativeDatabase and ResourceState.";
            return false;
        }

        BuildCardLookup();
        BuildSpeakerLookup();
        chainRunner = new CardChainRunner(resourceState, database.chains);

        if (!cardsById.TryGetValue(database.startingCardId, out CardData startCard))
        {
            error = $"Starting Card ID '{database.startingCardId}' does not resolve to a card.";
            return false;
        }

        CurrentCard = startCard;
        return true;
    }

    // Processes the chosen side (left or right), applying resource changes and resolving the next card.
    public NarrativeStepResult Choose(bool choseRight)
    {
        if (CurrentCard == null)
        {
            return new NarrativeStepResult(null, "No current card is available.");
        }

        if (CurrentCard.isEnding)
        {
            return new NarrativeStepResult(CurrentCard, null);
        }

        ResourceChange change = choseRight ? CurrentCard.rightResourceChange : CurrentCard.leftResourceChange;
        string selectedNextCardId = choseRight ? CurrentCard.rightNextCardId : CurrentCard.leftNextCardId;

        resourceState.Apply(change);
        state.Advance(CurrentCard.dayAdvance);

        string resolvedNextId = chainRunner.ResolveNextCardId(CurrentCard, selectedNextCardId);

        if (string.IsNullOrEmpty(resolvedNextId) || !cardsById.TryGetValue(resolvedNextId, out CardData nextCard))
        {
            return new NarrativeStepResult(null,
                $"'{CurrentCard.cardId}' has no valid next card to show.");
        }

        if (nextCard.isEnding)
        {
            return new NarrativeStepResult(nextCard, null);
        }

        CurrentCard = nextCard;
        return new NarrativeStepResult(null, null);
    }

    public CouncilMemberData GetSpeaker(string speakerId)
    {
        return !string.IsNullOrEmpty(speakerId) && speakersById.TryGetValue(speakerId, out CouncilMemberData speaker)
            ? speaker
            : null;
    }

    private void BuildCardLookup()
    {
        cardsById.Clear();
        if (database.cards == null)
        {
            return;
        }

        foreach (CardData card in database.cards)
        {
            if (card == null || string.IsNullOrEmpty(card.cardId))
            {
                Debug.LogWarning("A card is missing a Card ID and cannot be reached by branching.");
                continue;
            }

            if (!cardsById.TryAdd(card.cardId, card))
            {
                Debug.LogError($"Duplicate Card ID '{card.cardId}' found. The first card will be used.");
            }
        }
    }

    private void BuildSpeakerLookup()
    {
        speakersById.Clear();
        if (database.speakers == null)
        {
            return;
        }

        foreach (CouncilMemberData speaker in database.speakers)
        {
            if (speaker == null || string.IsNullOrEmpty(speaker.memberId))
            {
                continue;
            }

            if (!speakersById.TryAdd(speaker.memberId, speaker))
            {
                Debug.LogError($"Duplicate speaker ID '{speaker.memberId}' found. The first speaker will be used.");
            }
        }
    }
}
