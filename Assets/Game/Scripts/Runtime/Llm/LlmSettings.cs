using UnityEngine;

namespace Game.Scripts.Runtime.Llm
{
    [CreateAssetMenu(fileName = "LlmSettings", menuName = "Weave/LLM Settings", order = 10)]
    public class LlmSettings : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("The Groq model to use for completion. llama-3.3-70b-versatile is decommissioned " +
                 "2026-08-16 — do not switch back to it.")]
        public string groqModel = "qwen/qwen3.6-27b";

        [Range(0f, 2f), Tooltip("Controls randomness: 0 is deterministic, 2 is very creative.")]
        public float temperature = 0.9f;

        [Tooltip("Only used by reasoning models (openai/gpt-oss-20b, openai/gpt-oss-120b, qwen3 models). " +
                 "Qwen fully disables hidden reasoning with 'none' — keep it there for this game so " +
                 "dialogue/JSON never gets a <think>...</think> block glued onto it. GPT-OSS models only " +
                 "support 'low'/'medium'/'high' (no true off switch) and are not currently used here. " +
                 "If left blank in the Inspector, the client falls back to 'none' in code — Unity's " +
                 "JsonUtility can't actually omit an empty string field, so this can't be skipped.")]
        public string reasoningEffort = "none";

        [Min(50), Tooltip("Maximum tokens the AI can generate per response. Reasoning models split this " +
                          "budget between hidden reasoning and the actual reply, so keep this generous.")]
        public int maxTokensPerResponse = 300;

        [Min(20), Tooltip("Maximum tokens generated for the epilogue summary.")]
        public int epilogueMaxTokens = 200;

        [Min(0), Tooltip("Max absolute resource delta the AI may apply per resource on a Petition Card resolution.")]
        public int petitionResourceClampMagnitude = 20;

        [Min(1), Tooltip("Max player submissions allowed in one petition audience before the NPC is forced " +
                          "to deliver a final proposal. Deliberation turns (questions, back-and-forth) count " +
                          "toward this same budget — there's no separate cap.")]
        public int petitionMaxTurns = 6;

        [Min(1f), Tooltip("Network timeout in seconds for API calls.")]
        public float apiTimeoutSeconds = 10f;
    }
}
