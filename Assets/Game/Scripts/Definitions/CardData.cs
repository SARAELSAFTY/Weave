using UnityEngine;
using UnityEngine.Serialization;

// Data asset defining a single card's text, speaker, and choice outcomes.
[CreateAssetMenu(
    fileName = "NewCard",
    menuName = "Weave/Card Data",
    order = 0)]
public class CardData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique ID for this card.")]
    public string cardId;

    [Header("Card Content")]
    [TextArea(3, 6), Tooltip("Main story text shown on card.")]
    public string description;
    [Tooltip("ID of character speaking this card.")]
    public string speakerId;
    [Min(0), Tooltip("Days time advances when played.")]
    public int dayAdvance;

    [Header("Left Choice")]
    [Tooltip("Text shown when swiping left.")]
    public string leftChoiceText;
    [Tooltip("Resource changes when choosing left.")]
    public ResourceChange leftResourceChange;
    [Tooltip("Card ID to load when choosing left.")]
    public string leftNextCardId;

    [Header("Right Choice")]
    [Tooltip("Text shown when swiping right.")]
    public string rightChoiceText;
    [Tooltip("Resource changes when choosing right.")]
    public ResourceChange rightResourceChange;
    [Tooltip("Card ID to load when choosing right.")]
    public string rightNextCardId;

    [Header("Chain / Ending")]
    [FormerlySerializedAs("isResumePoint")]
    [Tooltip("Returns to main story after chain ends.")]
    public bool returnToMainCard;
    [Tooltip("Playing this card ends the game.")]
    public bool isEnding;
}
