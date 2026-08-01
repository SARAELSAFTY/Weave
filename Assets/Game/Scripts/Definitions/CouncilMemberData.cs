using UnityEngine;

// Data asset holding name, title, and portrait for character speakers.
[CreateAssetMenu(
    fileName = "NewCouncilMember",
    menuName = "Weave/Council Member",
    order = 1)]
public class CouncilMemberData : ScriptableObject
{
    [Tooltip("Unique ID for this character.")]
    public string memberId;
    [Tooltip("Character name shown in UI.")]
    public string displayName;
    [Tooltip("Role or title shown with name.")]
    public string title;
    [Tooltip("Character portrait image.")]
    public Sprite portrait;
}
