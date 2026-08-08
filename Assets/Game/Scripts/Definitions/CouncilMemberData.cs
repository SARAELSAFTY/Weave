using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Defines a narrative speaker and optional LLM persona settings.</summary>
    [CreateAssetMenu(fileName = "NewCouncilMember", menuName = "Weave/Council Member", order = 1)]
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

        [Header("LLM")]
        [Tooltip("If true, this speaker is voiced by AI at runtime.")]
        public bool isLlmSpeaker;

        [TextArea(4, 8), Tooltip("Base persona sent to the LLM whenever this speaker is voiced by AI: identity, tone, and behavior rules. Combined with any per-card situational prompt.")]
        public string llmPersonaPrompt;
    }
}
