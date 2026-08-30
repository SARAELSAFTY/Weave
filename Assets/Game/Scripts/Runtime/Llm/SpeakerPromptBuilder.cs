using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>
    /// Builds every system prompt sent to the LLM. All static text comes from LlmPromptTemplates -
    /// this class only selects the right template fields and assembles sections. See CardData for
    /// per-card seed overrides and GameManager for which call site uses which method below.
    /// </summary>
    public static class SpeakerPromptBuilder
    {
        /// <summary>
        /// Plain in-character line, no JSON. Used for reaction cards, resource warnings, and a petition
        /// card's opening announcement - anywhere a speaker just talks.
        /// </summary>
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
                .AddSection("Language Requirement", GetPlainTextLanguageInstruction(templates, language))
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("Situation", situationalPrompt)
                .AddSection("State", gameStateSnapshot)
                .AddSection("Terminology", BuildTerminologySection(resources, language))
                .ToString();
        }

        /// <summary>
        /// JSON-mode prompt for a single petition turn. The schema described in
        /// LlmPromptTemplates.petitionSystemInstructions must match <see cref="PetitionResolution"/> exactly.
        /// </summary>
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
                .AddSection("Language Requirement", GetJsonLanguageInstruction(templates, language))
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("Situation", situationalPrompt)
                .AddSection("State", gameStateSnapshot)
                .AddSection("Resources", BuildResourcesSection(validResources, clampMagnitude, language))
                .ToString();
        }

        private static string BuildResourcesSection(IReadOnlyList<ResourceData> resources, int clampMagnitude, GameLanguage language)
        {
            string section = $"Valid resources: {BuildValidResourceList(resources)}. " +
                             $"Deltas are whole numbers between -{clampMagnitude} and +{clampMagnitude}.";
            string terminology = BuildTerminologySection(resources, language);
            return terminology != null ? section + "\n" + terminology : section;
        }

        /// <summary>End-of-run epilogue summary prompt (collapse endings only).</summary>
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
                .AddSection("Language Requirement", GetPlainTextLanguageInstruction(templates, language))
                .AddSection("REIGN RECORD", reignRecord)
                .ToString();
        }

        private static string GetPlainTextLanguageInstruction(LlmPromptTemplates templates, GameLanguage language)
        {
            if (templates == null)
            {
                return null;
            }

            return language == GameLanguage.Arabic
                ? templates.plainTextArabicInstruction
                : templates.plainTextEnglishInstruction;
        }

        private static string GetJsonLanguageInstruction(LlmPromptTemplates templates, GameLanguage language)
        {
            if (templates == null)
            {
                return null;
            }

            return language == GameLanguage.Arabic
                ? templates.jsonArabicInstruction
                : templates.jsonEnglishInstruction;
        }

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
