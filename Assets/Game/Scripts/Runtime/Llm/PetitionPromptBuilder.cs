using System.Collections.Generic;
using System.Text;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Llm
{
    public static class PetitionPromptBuilder
    {
        public static string Build(SpeakerData speaker, string gameStateSnapshot, string situationalPrompt,
            IReadOnlyList<ResourceData> validResources, int clampMagnitude,
            string systemInstructionsTemplate, int turnNumber, int maxTurns, bool isFinalTurn)
        {
            StringBuilder promptBuilder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(systemInstructionsTemplate))
            {
                promptBuilder.Append(systemInstructionsTemplate);
            }

            if (speaker != null && !string.IsNullOrWhiteSpace(speaker.llmPersonaPrompt))
            {
                if (promptBuilder.Length > 0 && !promptBuilder.ToString().EndsWith("\n\n"))
                {
                    promptBuilder.Append("\n");
                }

                promptBuilder.Append("[Persona]\n").Append(speaker.llmPersonaPrompt.Trim()).Append("\n\n");
            }

            if (!string.IsNullOrWhiteSpace(situationalPrompt))
            {
                promptBuilder.Append("[Situation]\n").Append(situationalPrompt.Trim()).Append("\n\n");
            }

            if (!string.IsNullOrWhiteSpace(gameStateSnapshot))
            {
                promptBuilder.Append("[State]\n").Append(gameStateSnapshot.Trim()).Append("\n\n");
            }

            promptBuilder.Append("[Turn]\n").Append($"This is turn {turnNumber} of {maxTurns}.\n");
            if (isFinalTurn)
            {
                promptBuilder.Append(
                    "This is the FINAL turn. You must set \"phase\" to \"proposal\" now and deliver your " +
                    "decisive ruling — do not ask further questions, do not stay in \"deliberating\".\n");
            }
            promptBuilder.Append("\n");

            List<string> validNames = new List<string>();
            if (validResources != null)
            {
                foreach (ResourceData resource in validResources)
                {
                    if (resource != null && !string.IsNullOrWhiteSpace(resource.AssetName))
                    {
                        validNames.Add(resource.AssetName);
                    }
                }
            }

            string validResourceList = validNames.Count > 0 ? string.Join(", ", validNames) : "None";
            promptBuilder.Append("[Valid Resources]\n").Append(validResourceList).Append("\n\n");

            promptBuilder.Append("[Output Limits]\n")
                .Append($"Each delta must be a whole number between -{clampMagnitude} and +{clampMagnitude}.");

            return promptBuilder.ToString();
        }
    }
}
