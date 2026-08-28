using UnityEngine;

namespace Game.Scripts.Runtime.Llm
{
    [CreateAssetMenu(fileName = "LlmSettings", menuName = "Weave/LLM Settings", order = 10)]
    public class LlmSettings : ScriptableObject
    {
        [Header("API Settings")]
        [Tooltip("The Groq model used for every LLM request. Choose from the model list in this asset's Inspector.")]
        public string groqModel = "qwen/qwen3.6-27b";

        [Range(0f, 2f), Tooltip("Controls randomness: 0 is deterministic, 2 is very creative.")]
        public float temperature = 0.65f;

        [Tooltip("Only used by reasoning models (openai/gpt-oss-20b, openai/gpt-oss-120b, qwen3 models). " +
                 "Qwen fully disables hidden reasoning with 'none' - keep it there for this game so " +
                 "dialogue/JSON never gets a <think>...</think> block glued onto it. GPT-OSS models only " +
                 "support 'low'/'medium'/'high' (no true off switch) and are not currently used here. " +
                 "If left blank in the Inspector, the client falls back to 'none' in code - Unity's " +
                 "JsonUtility can't actually omit an empty string field, so this can't be skipped.")]
        public string reasoningEffort = "none";

        [Min(50), Tooltip("Maximum tokens the AI can generate per response. Reasoning models split this " +
                          "budget between hidden reasoning and the actual reply, so keep this generous.")]
        public int maxTokensPerResponse = 300;

        [Min(20), Tooltip("Maximum tokens generated for the epilogue summary.")]
        public int epilogueMaxTokens = 200;

        [Header("LLM History Context")]
        [Min(0), Tooltip("How many recent game events reactions can see. Set to 0 for none.")]
        public int reactionHistoryCount = 3;

        [Min(0), Tooltip("How many recent game events petitions can see. Set to 0 for none.")]
        public int petitionHistoryCount = 3;

        [Min(0), Tooltip("How many finished petition chats future LLM calls can see. Set to 0 for none.")]
        public int pastPetitionChatCount = 0;

        [Header("Petition Resolution")]
        [Min(0), Tooltip("Max absolute resource delta the AI may apply per resource on a Petition Card resolution.")]
        public int petitionResourceClampMagnitude = 20;

        [Min(1), Tooltip("How many off-topic, abusive, or spam submissions a petitioner will tolerate " +
                          "before withdrawing. Genuine negotiation does not spend this budget.")]
        public int petitionSpamDotBudget = 3;

        [Min(0f), Tooltip("Seconds to wait after a failed petition turn before the submit button is re-enabled.")]
        public float petitionRetryCooldownSeconds = 2f;

        [Min(1f), Tooltip("Network timeout in seconds for API calls.")]
        public float apiTimeoutSeconds = 10f;
    }
}
