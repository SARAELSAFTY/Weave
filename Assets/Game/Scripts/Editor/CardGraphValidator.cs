using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Editor tool for checking card links in NarrativeDatabase assets.
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
        Dictionary<string, CardData> cardsById = CardGraph.BuildLookup(database, issues.Add);

        if (string.IsNullOrEmpty(database.startingCardId) || !cardsById.ContainsKey(database.startingCardId))
        {
            issues.Add($"Narrative Database '{database.name}' has an invalid Starting Card ID '{database.startingCardId}'.");
        }

        foreach (CardData card in cardsById.Values)
        {
            CheckLink(card.name, "left", card.leftNextCardId, cardsById, issues);
            CheckLink(card.name, "right", card.rightNextCardId, cardsById, issues);
        }

        LogResults(database, cardsById.Count, issues);
    }

    private static void CheckLink(string cardName, string side, string nextCardId,
        Dictionary<string, CardData> cardsById, List<string> issues)
    {
        if (CardGraph.IsBrokenLink(nextCardId, cardsById))
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
