using UnityEngine;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Holds configurable settings for LLM reaction requests.</summary>
    [CreateAssetMenu(fileName = "LlmSettings", menuName = "Weave/LLM Settings", order = 10)]
    public class LlmSettings : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("The Groq model to use for completion.")]
        public string groqModel = "llama-3.3-70b-versatile";

        [Range(0f, 2f), Tooltip("Controls randomness: 0 is deterministic, 2 is very creative.")]
        public float temperature = 0.9f;

        [Min(50), Tooltip("Maximum tokens the AI can generate per response.")]
        public int maxTokensPerResponse = 150;

        [Min(1f), Tooltip("Network timeout in seconds for API calls.")]
        public float apiTimeoutSeconds = 10f;
    }
}
