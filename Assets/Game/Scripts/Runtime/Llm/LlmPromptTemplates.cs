using UnityEngine;

namespace Game.Scripts.Runtime.Llm
{
    [CreateAssetMenu(fileName = "LlmPromptTemplates", menuName = "Weave/LLM Prompt Templates", order = 11)]
    public class LlmPromptTemplates : ScriptableObject
    {
        [Header("System Instruction Blocks")]
        [TextArea(8, 20), Tooltip("Full system instruction block prepended to every persona/reaction LLM call.")]
        public string personaSystemInstructions =
            "[SYSTEM INSTRUCTIONS]\n" +
            "You are generating in-game character dialogue for a narrative card game.\n" +
            "RULES:\n" +
            "1. Output ONLY spoken dialogue or thoughts. NEVER include stage directions, action descriptions, or physical gestures in parentheses () or asterisks * *.\n" +
            "2. Keep it simple, concise, and natural (1 to 2 short sentences max).\n" +
            "3. Avoid melodramatic RP tropes, excessive ellipses (...), or theatrical AI phrasing.\n\n";

        [TextArea(8, 20), Tooltip("Full system instruction block prepended to the end-of-run epilogue call.")]
        public string epilogueSystemInstructions =
            "[SYSTEM INSTRUCTIONS]\n" +
            "You are a Royal Chronicler writing a concise epilogue for a narrative strategy game.\n" +
            "RULES:\n" +
            "1. Write a short 2 to 3 sentence historical epilogue summarizing how this ruler's reign ended.\n" +
            "2. Mention the length of the reign, the collapsed resource (or cause of collapse), and reference their actual decisions.\n" +
            "3. Output ONLY the spoken or written epilogue text. No titles, headers, quotation marks, parentheses (), or asterisks * *.\n" +
            "4. Keep the tone calm, sober, and direct.\n" +
            "5. Hard limit: no more than 60 words total. Do not pad.\n\n";

        [Header("Fallback Seed Prompts")]
        [Tooltip("Used when a reaction card has no authored llmPromptSeed.")]
        public string defaultReactionSeedPrompt = "React briefly to the player's recent decisions in character.";

        [Tooltip("Used when a resource has no authored warningSeedPrompt. {resourceName} is replaced at runtime.")]
        public string defaultWarningSeedPrompt = "{resourceName} is running low. React with mild concern, in character, and suggest the player pay attention soon.";

        [Header("LLM Turn Framing")]
        [Tooltip("User-turn message sent alongside the system prompt on every reaction request.")]
        public string reactionUserTurnPrompt = "React now, in character, following your instructions above.";

        [Header("Fallback Copy")]
        [Tooltip("Used when a run ends in collapse but no specific resource can be identified.")]
        public string unknownCollapseCauseLabel = "the throne itself";
    }
}
