using System.Text;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Builds a system prompt from speaker data, state text, and card prompt seed.</summary>
    public static class LlmPersonaPromptBuilder
    {
        /// <summary>Builds a full system prompt for one LLM reaction request.</summary>
        public static string Build(SpeakerData speaker, string gameStateSnapshot, string situationalPrompt, string systemInstructionsTemplate = null)
        {
            StringBuilder promptBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(systemInstructionsTemplate))
            {
                promptBuilder.Append(systemInstructionsTemplate);
            }
            else
            {
                promptBuilder.Append("[SYSTEM INSTRUCTIONS]\n")
                             .Append("You are generating in-game character dialogue for a narrative card game.\n")
                             .Append("RULES:\n")
                             .Append("1. Output ONLY spoken dialogue or thoughts. NEVER include stage directions, action descriptions, or physical gestures in parentheses () or asterisks * *.\n")
                             .Append("2. Keep it simple, concise, and natural (1 to 2 short sentences max).\n")
                             .Append("3. Avoid melodramatic RP tropes, excessive ellipses (...), or theatrical AI phrasing.\n\n");
            }

            if (speaker != null && !string.IsNullOrWhiteSpace(speaker.llmPersonaPrompt))
            {
                promptBuilder.Append("[Persona]\n").Append(speaker.llmPersonaPrompt.Trim());
            }

            if (!string.IsNullOrWhiteSpace(situationalPrompt))
            {
                if (promptBuilder.Length > 0)
                {
                    promptBuilder.Append("\n\n");
                }

                promptBuilder.Append("[Situation]\n").Append(situationalPrompt.Trim());
            }

            if (promptBuilder.Length > 0)
            {
                promptBuilder.Append("\n\n");
            }

            promptBuilder.Append("[State]\n").Append(gameStateSnapshot);
            return promptBuilder.ToString();
        }
    }
}
