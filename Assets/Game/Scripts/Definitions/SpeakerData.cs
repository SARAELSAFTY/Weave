using UnityEngine;

namespace Game.Scripts.Definitions
{
    [CreateAssetMenu(fileName = "Spk_NewSpeaker", menuName = "Weave/Speaker Data", order = 1)]
    public class SpeakerData : NamedGameAsset
    {
        [Tooltip("Character portrait image.")]
        public Sprite portrait;

        [TextArea(4, 8), Tooltip("Base persona sent to the LLM whenever this speaker is voiced by AI: identity, tone, and behavior rules.")]
        public string llmPersonaPrompt;

    }
}
