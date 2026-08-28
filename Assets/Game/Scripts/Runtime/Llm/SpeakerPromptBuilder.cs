using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>
    /// Builds every system prompt sent to the LLM. This is the one place prompt sections get assembled -
    /// see LlmPromptTemplates for the editable text that feeds into these, CardData for per-card seed
    /// overrides, and GameManager for which call site uses which method below.
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
            string systemInstructionsTemplate,
            GameLanguage language = GameLanguage.English,
            IReadOnlyList<ResourceData> resources = null)
        {
            return new PromptComposer()
                .AddRaw(systemInstructionsTemplate)
                .AddSection("Language Requirement", BuildPlainTextLanguageInstruction(language))
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
            string systemInstructionsTemplate,
            GameLanguage language = GameLanguage.English)
        {
            string outputLimits = $"Each delta must be a whole number between -{clampMagnitude} and +{clampMagnitude}.";

            return new PromptComposer()
                .AddRaw(systemInstructionsTemplate)
                .AddSection("Language Requirement", BuildJsonLanguageInstruction(language))
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("Situation", situationalPrompt)
                .AddSection("State", gameStateSnapshot)
                .AddSection("Valid Resources", BuildValidResourceList(validResources))
                .AddSection("Output Limits", outputLimits)
                .AddSection("Terminology", BuildTerminologySection(validResources, language))
                .ToString();
        }

        /// <summary>End-of-run epilogue summary prompt (collapse endings only).</summary>
        public static string BuildEpiloguePrompt(
            int dayCount,
            string collapsedResourceName,
            string finalResourceSummary,
            string fullChoiceHistory,
            string systemInstructionsTemplate,
            GameLanguage language = GameLanguage.English)
        {
            string reignRecord =
                $"Reign Length: {dayCount} day{(dayCount == 1 ? "" : "s")}\n" +
                $"Cause of Collapse: {collapsedResourceName} fell to zero\n" +
                $"Final Kingdom State: {finalResourceSummary}\n" +
                $"Full Decision History (in order): {fullChoiceHistory}";

            return new PromptComposer()
                .AddRaw(systemInstructionsTemplate)
                .AddSection("Language Requirement", BuildPlainTextLanguageInstruction(language))
                .AddSection("REIGN RECORD", reignRecord)
                .ToString();
        }

        private static string BuildPlainTextLanguageInstruction(GameLanguage language)
        {
            if (language == GameLanguage.Arabic)
            {
                return
                    "Respond with a single plain-text spoken line entirely in natural Modern Standard Arabic (الفصحى). " +
                    "Use ONLY Arabic script characters (Unicode range U+0600–U+06FF) and standard punctuation - " +
                    "no Thai, Latin, Cyrillic, or any other script under any circumstances. " +
                    "Address the ruler with formal court honorifics. " +
                    "Do not use brackets or field names.";
            }

            return "Respond with a single plain-text spoken line entirely in natural English.";
        }

        private static string BuildJsonLanguageInstruction(GameLanguage language)
        {
            if (language == GameLanguage.Arabic)
            {
                return
                    "Respond with valid JSON as specified. JSON structure, field names, and resource asset names " +
                    "must stay in English. The value of the \"reaction\" field must be written entirely in " +
                    "natural Modern Standard Arabic (الفصحى), using formal court honorifics. " +
                    "Use ONLY Arabic script characters (Unicode range U+0600–U+06FF) in the reaction field - " +
                    "no Thai, Latin, Cyrillic, or any other script under any circumstances.";
            }

            return "Respond entirely in natural English. Output valid JSON as specified.";
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
