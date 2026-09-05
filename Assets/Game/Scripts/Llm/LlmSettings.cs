using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Determines how the per-audience petition turn limit is resolved.</summary>
    public enum PetitionTurnLimitMode
    {
        /// <summary>Every audience uses the same fixed turn count from <see cref="LlmSettings.petitionTurnLimit"/>.</summary>
        Fixed,
        /// <summary>Each audience draws a random turn count between min and max bounds (inclusive).</summary>
        RandomRange
    }

    /// <summary>ScriptableObject holding all tunable LLM parameters shared across reaction, petition, and epilogue requests.</summary>
    /// <remarks>Referenced by <see cref="LlmReactionClient"/> at runtime. Petition turn limits are resolved per audience via <see cref="ResolvePetitionTurnLimit"/>.</remarks>
    [CreateAssetMenu(fileName = "LlmSettings", menuName = "Weave/LLM Settings", order = 10)]
    public class LlmSettings : ScriptableObject
    {
        [Tooltip("Groq model identifier sent in every API request (e.g. qwen/qwen3.8-27b).")]
        public string groqModel = "qwen/qwen3.8-27b";

        [Tooltip("Sampling temperature for all LLM requests; higher values increase variety at the cost of coherence.")]
        [Range(0f, 2f)]
        public float temperature = 0.65f;

        [Tooltip("Reasoning effort level for reasoning-capable models; set to 'none' to disable chain-of-thought.")]
        public string reasoningEffort = "none";

        [Header("Token Limits")]
        [Tooltip("Default maximum completion tokens for reaction and petition responses unless overridden per request.")]
        [Min(50)]
        public int maxTokensPerResponse = 300;

        [Tooltip("Maximum completion tokens reserved specifically for epilogue generation.")]
        [Min(20)]
        public int epilogueMaxTokens = 200;

        [Header("History Window")]
        [Tooltip("Number of recent ruler decisions included as context in reaction requests.")]
        [Min(0)]
        public int reactionHistoryCount = 3;

        [Tooltip("Number of prior petition turns carried forward in multi-turn petition conversations.")]
        [Min(0)]
        public int petitionHistoryCount = 3;

        [Tooltip("Number of completed past petitions whose chat history is appended as additional context.")]
        [Min(0)]
        public int pastPetitionChatCount = 0;

        [Header("Resource Clamping")]
        [Tooltip("Maximum absolute delta allowed per resource change in a petition resolution; values beyond this are clamped.")]
        [Min(0)]
        public int petitionResourceClampMagnitude = 20;

        [Header("Petition Turn Limit")]
        [Tooltip("How the per-audience turn budget is determined: fixed value or random range.")]
        public PetitionTurnLimitMode petitionTurnLimitMode = PetitionTurnLimitMode.Fixed;

        [Tooltip("Fixed number of turns per petition audience when mode is Fixed.")]
        [Min(1)]
        public int petitionTurnLimit = 3;

        [Tooltip("Minimum turn count (inclusive) when mode is RandomRange.")]
        [Min(1)]
        public int petitionMinTurnLimit = 2;

        [Tooltip("Maximum turn count (inclusive) when mode is RandomRange.")]
        [Min(1)]
        public int petitionMaxTurnLimit = 4;

        [Header("Timing")]
        [Tooltip("Seconds to wait before retrying a failed petition request.")]
        [Min(0f)]
        public float petitionRetryCooldownSeconds = 2f;

        [Tooltip("HTTP request timeout in seconds applied to every LLM proxy call.")]
        [Min(1f)]
        public float apiTimeoutSeconds = 10f;

        /// <summary>Returns the turn limit for a new petition audience, respecting the configured mode.</summary>
        /// <returns>A turn count >= 1 drawn from either the fixed value or the random range.</returns>
        public int ResolvePetitionTurnLimit()
        {
            if (petitionTurnLimitMode == PetitionTurnLimitMode.RandomRange)
            {
                int min = Mathf.Max(1, Mathf.Min(petitionMinTurnLimit, petitionMaxTurnLimit));
                int max = Mathf.Max(min, Mathf.Max(petitionMinTurnLimit, petitionMaxTurnLimit));
                return Random.Range(min, max + 1);
            }

            return Mathf.Max(1, petitionTurnLimit);
        }
    }
}
