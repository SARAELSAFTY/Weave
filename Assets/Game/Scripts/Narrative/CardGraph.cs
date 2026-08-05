using System.Collections.Generic;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

// Shared card-graph queries used by both runtime (NarrativeRunner) and editor tools
// (CardDataEditor, CardGraphWindow, CardNode). Keeping this in one place means
// "how do we look up a card by ID" and "what counts as a broken link" only need fixing once.
public static class CardGraph
{
    // Builds a cardId -> CardData lookup from a database's card list.
    // Cards with a missing or duplicate ID are skipped and reported via onIssue (first one wins).
    public static Dictionary<string, CardData> BuildLookup(NarrativeDatabase database, System.Action<string> onIssue = null)
    {
        Dictionary<string, CardData> lookup = new Dictionary<string, CardData>();
        if (database == null || database.cards == null)
        {
            return lookup;
        }

        foreach (CardData card in database.cards)
        {
            if (card == null)
            {
                onIssue?.Invoke("Database contains a missing card reference.");
                continue;
            }

            if (string.IsNullOrEmpty(card.cardId))
            {
                onIssue?.Invoke($"'{card.name}' has no Card ID.");
                continue;
            }

            if (!lookup.TryAdd(card.cardId, card))
            {
                onIssue?.Invoke($"Duplicate Card ID '{card.cardId}' used by both '{lookup[card.cardId].name}' and '{card.name}'.");
            }
        }

        return lookup;
    }

    // Builds a memberId -> CouncilMemberData lookup the same way.
    public static Dictionary<string, CouncilMemberData> BuildSpeakerLookup(NarrativeDatabase database, System.Action<string> onIssue = null)
    {
        Dictionary<string, CouncilMemberData> lookup = new Dictionary<string, CouncilMemberData>();
        if (database == null || database.speakers == null)
        {
            return lookup;
        }

        foreach (CouncilMemberData speaker in database.speakers)
        {
            if (speaker == null || string.IsNullOrEmpty(speaker.memberId))
            {
                continue;
            }

            if (!lookup.TryAdd(speaker.memberId, speaker))
            {
                onIssue?.Invoke($"Duplicate speaker ID '{speaker.memberId}' used by both '{lookup[speaker.memberId].name}' and '{speaker.name}'.");
            }
        }

        return lookup;
    }

    // True if targetCardId is non-empty but doesn't resolve to any card in lookup.
    public static bool IsBrokenLink(string targetCardId, Dictionary<string, CardData> lookup)
    {
        return !string.IsNullOrEmpty(targetCardId) && (lookup == null || !lookup.ContainsKey(targetCardId));
    }

#if UNITY_EDITOR
    // Keeps a card's cardId (and asset name) in sync with its asset's file name, and repoints
    // any other card/database that referenced the old ID. Editor-only: cardId is authored by
    // renaming the asset, so this is the single place that rename propagation happens.
    public static void SyncIdToAssetName(CardData card)
    {
        if (card == null)
        {
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(card);
        if (string.IsNullOrEmpty(assetPath))
        {
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(assetPath);

        if (string.IsNullOrEmpty(card.cardId))
        {
            card.cardId = fileName;
            EditorUtility.SetDirty(card);
            return;
        }

        if (card.cardId == fileName)
        {
            return;
        }

        RepointReferences(card.cardId, fileName);
        Undo.RecordObject(card, "Sync Card ID with Asset Name");
        card.cardId = fileName;
        card.name = fileName;
        EditorUtility.SetDirty(card);
    }

    // Updates every NarrativeDatabase's startingCardId, every CardData's left/right next-card
    // IDs, and every saved graph-node position that pointed at oldId, so a rename doesn't
    // silently break links or drop the card back to a default grid position.
    public static void RepointReferences(string oldId, string newId)
    {
        if (string.IsNullOrEmpty(oldId) || string.IsNullOrEmpty(newId) || oldId == newId)
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:NarrativeDatabase");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            NarrativeDatabase db = AssetDatabase.LoadAssetAtPath<NarrativeDatabase>(path);
            if (db == null)
            {
                continue;
            }

            if (db.startingCardId == oldId)
            {
                Undo.RecordObject(db, "Update Starting Card ID");
                db.startingCardId = newId;
                EditorUtility.SetDirty(db);
            }

            if (db.editorGraphPositions != null)
            {
                for (int i = 0; i < db.editorGraphPositions.Count; i++)
                {
                    if (db.editorGraphPositions[i].cardId != oldId)
                    {
                        continue;
                    }

                    Undo.RecordObject(db, "Update Graph Node Position Id");
                    NarrativeDatabase.CardGraphPosition entry = db.editorGraphPositions[i];
                    entry.cardId = newId;
                    db.editorGraphPositions[i] = entry;
                    EditorUtility.SetDirty(db);
                }
            }

            if (db.cards == null)
            {
                continue;
            }

            foreach (CardData c in db.cards)
            {
                if (c == null)
                {
                    continue;
                }

                bool changed = false;
                if (c.leftNextCardId == oldId)
                {
                    Undo.RecordObject(c, "Update Left Next Card ID");
                    c.leftNextCardId = newId;
                    changed = true;
                }

                if (c.rightNextCardId == oldId)
                {
                    Undo.RecordObject(c, "Update Right Next Card ID");
                    c.rightNextCardId = newId;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(c);
                }
            }
        }
    }
#endif
}