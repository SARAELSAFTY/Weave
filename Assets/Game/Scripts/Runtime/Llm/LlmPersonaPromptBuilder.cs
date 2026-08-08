using System.Text;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Builds a system prompt from speaker data, state text, and card prompt seed.</summary>
    public static class LlmPersonaPromptBuilder
    {
        /// <summary>Builds a full system prompt for one LLM reaction request.</summary>
        public static string Build(CouncilMemberData speaker, string gameStateSnapshot, string situationalPrompt)
        {
            StringBuilder promptBuilder = new StringBuilder();

            if (speaker != null && speaker.isLlmSpeaker && !string.IsNullOrWhiteSpace(speaker.llmPersonaPrompt))
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
