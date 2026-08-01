using System.Collections.Generic;
using UnityEngine;

// Central story database linking cards, characters, and story chains.
[CreateAssetMenu(fileName = "NarrativeDatabase", menuName = "Weave/Narrative Database", order = 3)]
public class NarrativeDatabase : ScriptableObject
{
    [Header("Entry Point")]
    [Tooltip("First card ID shown when game starts.")]
    public string startingCardId;

    [Header("Authored Content")]
    [Tooltip("All card assets in the game.")]
    public List<CardData> cards = new List<CardData>();
    [Tooltip("All character speaker assets.")]
    public List<CouncilMemberData> speakers = new List<CouncilMemberData>();
    [Tooltip("All story chain assets.")]
    public List<CardChainData> chains = new List<CardChainData>();
}
