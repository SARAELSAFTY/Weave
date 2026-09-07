using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;

namespace Game.Scripts.Llm
{
    /// <summary>Assembles complete system prompts for persona, petition, and epilogue LLM requests from templates and runtime data.</summary>
    /// <remarks>All static prompt text comes from <see cref="LlmPromptTemplates"/>; this class only composes sections.</remarks>
    public static class SpeakerPromptBuilder
    {
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

        /// <summary>Builds a complete system prompt for a chat card turn: talk only, no resource contract.</summary>
        /// <param name="speaker">Speaker data providing the persona prompt section.</param>
        /// <param name="gameStateSnapshot">Serialized kingdom state injected as the State section.</param>
        /// <param name="templates">Prompt templates providing chat system instructions and language strings.</param>
        /// <param name="language">Target language controlling which language instruction is selected.</param>
        /// <returns>The assembled system prompt string.</returns>
        public static string BuildChatTurnPrompt(
            SpeakerData speaker,
            string gameStateSnapshot,
            LlmPromptTemplates templates,
            GameLanguage language = GameLanguage.English)
        {
            return new PromptComposer()
                .AddRaw(templates != null ? templates.chatSystemInstructions : null)
                .AddSection("Language Requirement", GetLanguageInstruction(templates, language))
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("State", gameStateSnapshot)
                .ToString();
        }

        /// <summary>Builds a complete system prompt for the end-of-reign epilogue narration.</summary>
        /// <param name="dayCount">Length of the reign in days.</param>
        /// <param name="endingCauseLine">Full record line stating how the reign ended,
        /// e.g. "Cause of Collapse: Crown collapsed".</param>
        /// <param name="finalResourceSummary">Formatted summary of all resource values at the reign's end.</param>
        /// <param name="fullChoiceHistory">Ordered list of ruler decisions throughout the reign.</param>
        /// <param name="templates">Prompt templates providing epilogue system instructions and language strings.</param>
        /// <param name="language">Target language controlling which language instruction is selected.</param>
        /// <param name="speaker">Optional speaker whose persona supplies the epilogue's voice; may be null.</param>
        /// <returns>The assembled system prompt string.</returns>
        public static string BuildEpiloguePrompt(
            int dayCount,
            string endingCauseLine,
            string finalResourceSummary,
            string fullChoiceHistory,
            LlmPromptTemplates templates,
            GameLanguage language = GameLanguage.English,
            SpeakerData speaker = null)
        {
            string reignRecord =
                $"Reign Length: {dayCount} day{(dayCount == 1 ? "" : "s")}\n" +
                $"{endingCauseLine}\n" +
                $"Final Kingdom State: {finalResourceSummary}\n" +
                $"Full Decision History (in order): {fullChoiceHistory}";

            return new PromptComposer()
                .AddRaw(templates != null ? templates.epilogueSystemInstructions : null)
                .AddSection("Language Requirement", GetLanguageInstruction(templates, language))
                .AddSection("Persona", speaker != null ? speaker.llmPersonaPrompt : null)
                .AddSection("REIGN RECORD", reignRecord)
                .ToString();
        }

        // The model never sees the card text the player just read; prepending it to the seed keeps the
        // generated reaction from contradicting the on-card reveal.
        private const string ReactionScenePrefix = "Scene just shown to the ruler: ";

        /// <summary>Composes the situation seed for a reaction request: the scene description shown on the card,
        /// prepended to the reaction seed so the model cannot contradict the on-card reveal.</summary>
        /// <param name="sceneDescription">Card description the ruler just read; may be empty.</param>
        /// <param name="reactionSeed">Reaction seed from the card override or the global default.</param>
        /// <returns>The combined seed, or the bare reaction seed when no scene description exists.</returns>
        public static string BuildReactionSituation(string sceneDescription, string reactionSeed)
        {
            return string.IsNullOrWhiteSpace(sceneDescription)
                ? reactionSeed
                : $"{ReactionScenePrefix}{sceneDescription.Trim()}\n{reactionSeed}";
        }

        /// <summary>Fills the default warning seed template with the resource that is running low.</summary>
        /// <param name="templates">Prompt templates providing the warning seed; may be null.</param>
        /// <param name="resourceName">Localized display name of the low resource.</param>
        /// <returns>The filled warning seed.</returns>
        public static string BuildWarningSituation(LlmPromptTemplates templates, string resourceName)
        {
            return PromptTemplateUtility.Fill(
                templates != null ? templates.defaultWarningSeedPrompt : string.Empty,
                "resourceName", resourceName);
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
