using UnityEngine;

[CreateAssetMenu(
    fileName = "NewCard",
    menuName = "Weave/Card Data",
    order = 0)]
public class CardData : ScriptableObject
{
    [Header("Card Content")]
    public string cardTitle;

    [TextArea(3, 6)]
    public string description;

    [Header("Left Choice")]
    public string leftChoiceText;
    public ResourceChange leftResourceChange;

    [Header("Right Choice")]
    public string rightChoiceText;
    public ResourceChange rightResourceChange;
}