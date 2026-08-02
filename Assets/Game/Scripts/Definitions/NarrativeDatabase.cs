using System.Collections.Generic;
using UnityEngine;

// Central story database linking cards and characters.
[CreateAssetMenu(fileName = "NarrativeDatabase", menuName = "Weave/Narrative Database", order = 3)]
public class NarrativeDatabase : ScriptableObject
{
    // One saved node position for the Card Graph editor window, keyed by cardId. Stored on the
    // database asset (rather than only in memory on the graph view) so a card's layout survives
    // closing/reopening the Card Graph window and Unity domain reloads. Editor-tooling data only
    // - never read at runtime.
    [System.Serializable]
    public struct CardGraphPosition
    {
        public string cardId;
        public Vector2 position;
    }

    [Header("Entry Point")]
    [Tooltip("First card ID shown when game starts.")]
    public string startingCardId;

    [Header("Authored Content")]
    [Tooltip("All card assets in the game.")]
    public List<CardData> cards = new List<CardData>();
    [Tooltip("All character speaker assets.")]
    public List<CouncilMemberData> speakers = new List<CouncilMemberData>();

    [HideInInspector]
    public List<CardGraphPosition> editorGraphPositions = new List<CardGraphPosition>();
}