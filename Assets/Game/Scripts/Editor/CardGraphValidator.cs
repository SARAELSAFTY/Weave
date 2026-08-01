using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor tool for checking card links and story chains in NarrativeDatabase assets.
public static class CardGraphValidator
{
    [MenuItem("CONTEXT/NarrativeDatabase/Validate Card Links")]
    private static void ValidateCardLinks(MenuCommand command)
    {
        NarrativeDatabase database = (NarrativeDatabase)command.context;
        if (database == null)
        {
            return;
        }

        List<string> issues = new List<string>();
        Dictionary<string, CardData> cardsById = BuildCardLookup(database, issues);

        if (string.IsNullOrEmpty(database.startingCardId) || !cardsById.ContainsKey(database.startingCardId))
        {
            issues.Add($"Narrative Database '{database.name}' has an invalid Starting Card ID '{database.startingCardId}'.");
        }

        foreach (CardData card in cardsById.Values)
        {
            if (card.returnToMainCard && card.isEnding)
            {
                issues.Add($"'{card.name}' is both Return To Main Card and Is Ending.");
            }

            CheckLink(card.name, "left", card.leftNextCardId, cardsById, issues);
            CheckLink(card.name, "right", card.rightNextCardId, cardsById, issues);
        }

        if (database.chains != null)
        {
            CheckChains(database.chains, cardsById, issues);
        }

        LogResults(database, cardsById.Count, issues);
    }

    private static Dictionary<string, CardData> BuildCardLookup(NarrativeDatabase database, List<string> issues)
    {
        Dictionary<string, CardData> cardsById = new Dictionary<string, CardData>();
        if (database.cards == null)
        {
            return cardsById;
        }

        foreach (CardData card in database.cards)
        {
            if (card == null)
            {
                issues.Add($"Narrative Database '{database.name}' contains a missing card reference.");
                continue;
            }

            if (string.IsNullOrEmpty(card.cardId))
            {
                issues.Add($"'{card.name}' has no Card ID.");
                continue;
            }

            if (!cardsById.TryAdd(card.cardId, card))
            {
                issues.Add($"Duplicate Card ID '{card.cardId}' used by both '{cardsById[card.cardId].name}' and '{card.name}'.");
            }
        }

        return cardsById;
    }

    private static void CheckChains(List<CardChainData> chains, Dictionary<string, CardData> cardsById, List<string> issues)
    {
        Dictionary<string, string> chainNamesById = new Dictionary<string, string>();
        foreach (CardChainData chain in chains)
        {
            if (chain == null)
            {
                issues.Add("Narrative Database contains a missing chain reference.");
                continue;
            }

            CheckChainId(chain, chainNamesById, issues);
            CheckChainCards(chain, cardsById, issues);
        }
    }

    private static void CheckChainId(CardChainData chain, Dictionary<string, string> chainNamesById, List<string> issues)
    {
        if (string.IsNullOrEmpty(chain.chainId))
        {
            issues.Add($"Chain asset '{chain.name}' has no Chain ID.");
        }
        else if (!chainNamesById.TryAdd(chain.chainId, chain.name))
        {
            issues.Add($"Duplicate Chain ID '{chain.chainId}' on '{chainNamesById[chain.chainId]}' and '{chain.name}'.");
        }
    }

    private static void CheckChainCards(CardChainData chain, Dictionary<string, CardData> cardsById, List<string> issues)
    {
        if (string.IsNullOrEmpty(chain.startCardId))
        {
            issues.Add($"Chain '{chain.name}' has no Start Card ID.");
            return;
        }

        HashSet<string> chainCardIds = new HashSet<string>();
        Stack<string> pendingCardIds = new Stack<string>();
        pendingCardIds.Push(chain.startCardId);
        bool hasTerminalCard = false;

        while (pendingCardIds.Count > 0)
        {
            string cardId = pendingCardIds.Pop();
            if (!cardsById.TryGetValue(cardId, out CardData card))
            {
                issues.Add($"Chain '{chain.name}' points to missing Card ID '{cardId}'.");
                continue;
            }

            if (!chainCardIds.Add(cardId))
            {
                continue;
            }

            hasTerminalCard |= card.returnToMainCard || card.isEnding;
            if (card.returnToMainCard || card.isEnding)
            {
                continue;
            }

            AddNextCardId(chain, card, "left", card.leftNextCardId, pendingCardIds, issues);
            AddNextCardId(chain, card, "right", card.rightNextCardId, pendingCardIds, issues);
        }

        if (!hasTerminalCard)
        {
            issues.Add($"Chain '{chain.name}' has no Return To Main Card or Is Ending card.");
        }
    }

    private static void AddNextCardId(CardChainData chain, CardData card, string side, string nextCardId,
        Stack<string> pendingCardIds, List<string> issues)
    {
        if (string.IsNullOrEmpty(nextCardId))
        {
            issues.Add($"Chain '{chain.name}' card '{card.name}' has no {side} link.");
            return;
        }

        pendingCardIds.Push(nextCardId);
    }

    private static void CheckLink(string cardName, string side, string nextCardId,
        Dictionary<string, CardData> cardsById, List<string> issues)
    {
        if (!string.IsNullOrEmpty(nextCardId) && !cardsById.ContainsKey(nextCardId))
        {
            issues.Add($"'{cardName}' ({side}) points to missing Card ID '{nextCardId}'.");
        }
    }

    private static void LogResults(NarrativeDatabase database, int cardCount, List<string> issues)
    {
        if (issues.Count == 0)
        {
            Debug.Log($"[CardGraphValidator] '{database.name}': {cardCount} cards checked. No issues found.");
            return;
        }

        Debug.LogWarning($"[CardGraphValidator] '{database.name}': {issues.Count} issue(s) found across {cardCount} cards:\n- " +
                         string.Join("\n- ", issues));
    }
}
