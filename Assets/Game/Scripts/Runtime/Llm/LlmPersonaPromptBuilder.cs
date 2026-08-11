using System.Text;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Llm
{
    public static class LlmPersonaPromptBuilder
    {
        public static string Build(SpeakerData speaker, string gameStateSnapshot, string situationalPrompt, string systemInstructionsTemplate = null)
        {
            StringBuilder promptBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(systemInstructionsTemplate))
            {
                promptBuilder.Append(systemInstructionsTemplate);
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
