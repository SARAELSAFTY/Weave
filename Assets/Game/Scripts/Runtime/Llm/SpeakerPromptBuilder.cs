using System.Collections.Generic;
using Game.Scripts.Definitions;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>
    /// Builds every system prompt sent to the LLM. This is the one place prompt sections get assembled -
    /// see LlmPromptTemplates for the editable text that feeds into these, CardData for per-card seed
    /// overrides, and GameManager for which call site uses which method below.
    /// Replaces the former LlmPersonaPromptBuilder / PetitionPromptBuilder / EpiloguePromptBuilder split.
    /// </summary>
    public static class SpeakerPromptBuilder
    {
        /// <summary>
        /// Plain in-character line, no JSON. Used for reaction cards, resource warnings, and a petition
        /// card's opening announcement - anywhere a speaker just talks.
        /// </summary>
        public static string BuildPersonaPrompt(SpeakerData speaker, string gameStateSnapshot, string situationalPrompt, string systemInstructionsTemplate)
        {
            return new PromptComposer()
                .AddRaw(systemInstructionsTemplate)
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("Situation", situationalPrompt)
                .AddSection("State", gameStateSnapshot)
                .ToString();
        }

        /// <summary>
        /// JSON-mode prompt for a single petition turn. The schema described in
        /// LlmPromptTemplates.petitionSystemInstructions must match <see cref="PetitionResolution"/> exactly.
        /// </summary>
        public static string BuildPetitionTurnPrompt(SpeakerData speaker, string gameStateSnapshot, string situationalPrompt,
            IReadOnlyList<ResourceData> validResources, int clampMagnitude,
            string systemInstructionsTemplate, int turnNumber, int maxTurns, bool isFinalTurn)
        {
            string turnSection = $"This is turn {turnNumber} of {maxTurns}.";
            if (isFinalTurn)
            {
                turnSection +=
                    "\nThis is the FINAL turn. You must set \"phase\" to \"proposal\" now and deliver your " +
                    "decisive ruling - do not ask further questions, do not stay in \"deliberating\".";
            }

            string outputLimits = $"Each delta must be a whole number between -{clampMagnitude} and +{clampMagnitude}.";

            return new PromptComposer()
                .AddRaw(systemInstructionsTemplate)
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("Situation", situationalPrompt)
                .AddSection("State", gameStateSnapshot)
                .AddSection("Turn", turnSection)
                .AddSection("Valid Resources", BuildValidResourceList(validResources))
                .AddSection("Output Limits", outputLimits)
                .ToString();
        }

        /// <summary>End-of-run epilogue summary prompt (collapse endings only).</summary>
        public static string BuildEpiloguePrompt(int dayCount, string collapsedResourceName, string finalResourceSummary, string fullChoiceHistory, string systemInstructionsTemplate)
        {
            string reignRecord =
                $"Reign Length: {dayCount} day{(dayCount == 1 ? "" : "s")}\n" +
                $"Cause of Collapse: {collapsedResourceName} fell to zero\n" +
                $"Final Kingdom State: {finalResourceSummary}\n" +
                $"Full Decision History (in order): {fullChoiceHistory}";

            return new PromptComposer()
                .AddRaw(systemInstructionsTemplate)
                .AddSection("REIGN RECORD", reignRecord)
                .ToString();
        }

        private static string BuildValidResourceList(IReadOnlyList<ResourceData> validResources)
        {
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

            return validNames.Count > 0 ? string.Join(", ", validNames) : "None";
        }
    }
}
