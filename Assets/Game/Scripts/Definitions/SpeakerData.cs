using UnityEngine;

namespace Game.Scripts.Definitions
{
    /// <summary>Defines a narrative speaker with a portrait and an LLM persona prompt.</summary>
    [CreateAssetMenu(fileName = "Spk_NewSpeaker", menuName = "Weave/Speaker Data", order = 1)]
    public class SpeakerData : NamedGameAsset
    {
        [Tooltip("Portrait sprite displayed when this speaker presents a card.")]
        public Sprite portrait;

        [TextArea(4, 8)]
        [Tooltip("System prompt defining this speaker's personality and voice for LLM generation.")]
        public string llmPersonaPrompt;
    }
}
