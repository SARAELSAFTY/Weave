using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Llm
{
    /// <summary>Assembles complete system prompts for persona, petition, and epilogue LLM requests from templates and runtime data.</summary>
    /// <remarks>All static prompt text comes from <see cref="LlmPromptTemplates"/>; this class only composes sections.
    /// The sole exception is <see cref="SingleTurnUserMessage"/>, which is the only hardcoded prompt string in the codebase.</remarks>
    public static class SpeakerPromptBuilder
    {
        /// <summary>Hardcoded user message sent with single-turn reaction requests; the only prompt string not stored on LlmPromptTemplates.</summary>
        public const string SingleTurnUserMessage = "Respond to the situation above.";

        /// <summary>Builds a complete system prompt for a persona/reaction request combining voice, situation, state, and terminology.</summary>
        /// <param name="speaker">Speaker data providing the persona prompt section.</param>
        /// <param name="gameStateSnapshot">Serialized kingdom state injected as the State section.</param>
        /// <param name="situationalPrompt">Situational context injected as the Situation section.</param>
        /// <param name="templates">Prompt templates providing system instructions and language strings.</param>
        /// <param name="language">Target language controlling which language instruction is selected.</param>
        /// <param name="resources">Optional resource list for Arabic terminology mapping.</param>
        /// <returns>The assembled system prompt string.</returns>
        public static string BuildPersonaPrompt(
            SpeakerData speaker,
            string gameStateSnapshot,
            string situationalPrompt,
            LlmPromptTemplates templates,
            GameLanguage language = GameLanguage.English,
            IReadOnlyList<ResourceData> resources = null)
        {
            return new PromptComposer()
                .AddRaw(templates != null ? templates.personaSystemInstructions : null)
                .AddSection("Language Requirement", GetLanguageInstruction(templates, language))
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("Situation", situationalPrompt)
                .AddSection("State", gameStateSnapshot)
                .AddSection("Terminology", BuildTerminologySection(resources, language))
                .ToString();
        }

        /// <summary>Builds a complete system prompt for a petition turn including resource constraints and delta bounds.</summary>
        /// <param name="speaker">Speaker data providing the persona prompt section.</param>
        /// <param name="gameStateSnapshot">Serialized kingdom state injected as the State section.</param>
        /// <param name="situationalPrompt">Situational context injected as the Situation section.</param>
        /// <param name="validResources">Resources the model may reference in resourceChanges deltas.</param>
        /// <param name="clampMagnitude">Maximum absolute delta value communicated to the model.</param>
        /// <param name="templates">Prompt templates providing petition system instructions and language strings.</param>
        /// <param name="language">Target language controlling which language instruction is selected.</param>
        /// <returns>The assembled system prompt string.</returns>
        public static string BuildPetitionTurnPrompt(
            SpeakerData speaker,
            string gameStateSnapshot,
            string situationalPrompt,
            IReadOnlyList<ResourceData> validResources,
            int clampMagnitude,
            LlmPromptTemplates templates,
            GameLanguage language = GameLanguage.English)
        {
            return new PromptComposer()
                .AddRaw(templates != null ? templates.petitionSystemInstructions : null)
                .AddSection("Language Requirement", GetLanguageInstruction(templates, language))
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("Situation", situationalPrompt)
                .AddSection("State", gameStateSnapshot)
                .AddSection("Resources", BuildResourcesSection(validResources, clampMagnitude, language))
                .ToString();
        }

        // Composes the Resources section listing valid resource names and delta bounds, plus Arabic terminology if applicable.
        private static string BuildResourcesSection(IReadOnlyList<ResourceData> resources, int clampMagnitude, GameLanguage language)
        {
            string section = $"Valid resources: {BuildValidResourceList(resources)}. " +
                             $"Deltas are whole numbers between -{clampMagnitude} and +{clampMagnitude}.";
            string terminology = BuildTerminologySection(resources, language);
            return terminology != null ? section + "\n" + terminology : section;
        }

        /// <summary>Builds a complete system prompt for the end-of-reign epilogue narration.</summary>
        /// <param name="dayCount">Length of the reign in days.</param>
        /// <param name="collapsedResourceName">Name of the resource that reached zero and ended the reign.</param>
        /// <param name="finalResourceSummary">Formatted summary of all resource values at collapse.</param>
        /// <param name="fullChoiceHistory">Ordered list of ruler decisions throughout the reign.</param>
        /// <param name="templates">Prompt templates providing epilogue system instructions and language strings.</param>
        /// <param name="language">Target language controlling which language instruction is selected.</param>
        /// <returns>The assembled system prompt string.</returns>
        public static string BuildEpiloguePrompt(
            int dayCount,
            string collapsedResourceName,
            string finalResourceSummary,
            string fullChoiceHistory,
            LlmPromptTemplates templates,
            GameLanguage language = GameLanguage.English)
        {
            string reignRecord =
                $"Reign Length: {dayCount} day{(dayCount == 1 ? "" : "s")}\n" +
                $"Cause of Collapse: {collapsedResourceName} fell to zero\n" +
                $"Final Kingdom State: {finalResourceSummary}\n" +
                $"Full Decision History (in order): {fullChoiceHistory}";

            return new PromptComposer()
                .AddRaw(templates != null ? templates.epilogueSystemInstructions : null)
                .AddSection("Language Requirement", GetLanguageInstruction(templates, language))
                .AddSection("REIGN RECORD", reignRecord)
                .ToString();
        }

        // Selects the appropriate language instruction template based on the current game language.
        private static string GetLanguageInstruction(LlmPromptTemplates templates, GameLanguage language)
        {
            if (templates == null)
            {
                return null;
            }

            return language == GameLanguage.Arabic
                ? templates.arabicInstruction
                : templates.englishInstruction;
        }

        // Builds an Arabic terminology mapping section listing approved Arabic terms for each resource; returns null for non-Arabic languages.
        private static string BuildTerminologySection(IReadOnlyList<ResourceData> resources, GameLanguage language)
        {
            if (language != GameLanguage.Arabic || resources == null)
            {
                return null;
            }

            List<string> lines = new List<string>();
            foreach (ResourceData resource in resources)
            {
                if (resource == null || string.IsNullOrWhiteSpace(resource.AssetName))
                {
                    continue;
                }

                lines.Add($"- {resource.AssetName} -> {resource.GetDisplayName(GameLanguage.Arabic)}");
            }

            if (lines.Count == 0)
            {
                return null;
            }

            return "Approved Arabic terms for kingdom resources (use these exact Arabic terms in narration when referring to resources):\n" +
                   string.Join("\n", lines);
        }

        // Joins valid resource asset names into a comma-separated list for the prompt; returns "None" if empty.
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
