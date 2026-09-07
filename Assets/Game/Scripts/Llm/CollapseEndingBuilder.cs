using System;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Narrative;
using Game.Scripts.UI;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Presents collapse endings: shows a runtime ending card with an LLM-generated epilogue,
    /// falling back to resource-provided text, the authored ending card, or a generic run-ended message.</summary>
    /// <remarks>Constructed and owned by <see cref="GameManager"/>; the runtime ending card it creates is
    /// not tracked by Unity's asset database and is destroyed via <see cref="Cleanup"/>.</remarks>
    public class CollapseEndingBuilder
    {
        private readonly CardView cardView;
        private readonly LlmReactionClient llmReactionClient;
        private readonly PlayerHistoryTracker historyTracker;
        private readonly NarrativeDatabase database;
        private readonly NarrativeRunner narrativeRunner;
        private readonly LlmSettings llmSettings;
        private readonly Func<GameLanguage> currentLanguage;

        private CardData runtimeEndingCard;

        public CollapseEndingBuilder(CardView cardView, LlmReactionClient llmReactionClient,
            PlayerHistoryTracker historyTracker, NarrativeDatabase database, NarrativeRunner narrativeRunner,
            LlmSettings llmSettings, Func<GameLanguage> currentLanguage)
        {
            this.cardView = cardView;
            this.llmReactionClient = llmReactionClient;
            this.historyTracker = historyTracker;
            this.database = database;
            this.narrativeRunner = narrativeRunner;
            this.llmSettings = llmSettings;
            this.currentLanguage = currentLanguage;
        }

        /// <summary>Destroys the runtime ending card instance; call from the owning MonoBehaviour's OnDestroy.</summary>
        public void Cleanup()
        {
            if (runtimeEndingCard != null)
            {
                UnityEngine.Object.Destroy(runtimeEndingCard);
                runtimeEndingCard = null;
            }
        }

        /// <summary>Presents a collapse ending: shows a generated epilogue card when the LLM client and
        /// history tracker are available, otherwise falls back to the static fallbacks.</summary>
        /// <param name="speaker">Speaker presenting the ending; may be null.</param>
        /// <param name="collapsedResource">The resource whose collapse triggered the ending.</param>
        /// <param name="fallbackCard">Authored ending card to show when generation is unavailable; may be null.</param>
        public void Show(SpeakerData speaker, ResourceData collapsedResource, CardData fallbackCard)
        {
            if (llmReactionClient == null || historyTracker == null ||
                database == null || database.resourceCatalog == null || collapsedResource == null)
            {
                Debug.LogWarning("[CollapseEndingBuilder] Missing components for LLM generation; showing fallback.");
                ShowFallback(fallbackCard, speaker, collapsedResource);
                return;
            }

            // Show the generated card immediately with a "generating..." placeholder;
            // the LLM callback will overwrite the description when the response arrives.
            CardData generatedCard = CreateGeneratedEndingCard(fallbackCard, speaker);
            cardView.ShowEnding(generatedCard, speaker);

            LlmPromptTemplates templates = database.promptTemplates;
            GameLanguage language = currentLanguage();
            string collapsedResourceName = collapsedResource.GetDisplayName(language);
            string finalResourceSummary = historyTracker.GetResourceSummary(language);
            string fullChoiceSummary = historyTracker.GetFullHistorySummary(language);

            // The collapse threshold is buffered below zero, so "collapsed" is accurate where
            // "fell to zero" would overstate the trigger.
            string epiloguePrompt = SpeakerPromptBuilder.BuildEpiloguePrompt(
                narrativeRunner.Day,
                $"Cause of Collapse: {collapsedResourceName} collapsed",
                finalResourceSummary, fullChoiceSummary, templates, language, speaker);

            int? epilogueMaxTokens = llmSettings != null ? llmSettings.epilogueMaxTokens : LlmSettings.DefaultEpilogueMaxTokens;

            llmReactionClient.RequestReaction(
                epiloguePrompt,
                templates != null ? templates.singleTurnUserMessage : null,
                language,
                line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        generatedCard.descriptionLocalized = new LocalizedText { english = line, arabic = line };
                        cardView.ShowEnding(generatedCard, speaker);
                        return;
                    }

                    Debug.LogWarning("[CollapseEndingBuilder] Epilogue generation returned an empty response; using the fallback ending card.");
                    ShowFallback(fallbackCard, speaker, collapsedResource);
                },
                error =>
                {
                    Debug.LogWarning($"[CollapseEndingBuilder] Epilogue generation failed: {error}");
                    ShowFallback(fallbackCard, speaker, collapsedResource);
                },
                maxTokensOverride: epilogueMaxTokens);
        }

        private void ShowFallback(CardData fallbackCard, SpeakerData speaker, ResourceData collapsedResource)
        {
            // First priority: inline fallback text defined directly on the collapsed resource
            string fallbackText = collapsedResource != null ? collapsedResource.GetCollapseFallbackText(currentLanguage()) : null;
            if (!string.IsNullOrWhiteSpace(fallbackText))
            {
                CardData resourceFallbackCard = CreateEndingCard(
                    $"{collapsedResource.AssetName}_Collapse_Fallback",
                    speaker,
                    new LocalizedText
                    {
                        english = !string.IsNullOrWhiteSpace(collapsedResource.collapseEndingFallbackEnglish)
                            ? collapsedResource.collapseEndingFallbackEnglish
                            : fallbackText,
                        arabic = !string.IsNullOrWhiteSpace(collapsedResource.collapseEndingFallbackArabic)
                            ? collapsedResource.collapseEndingFallbackArabic
                            : fallbackText
                    },
                    0,
                    CardArtMode.None);

                cardView.ShowEnding(resourceFallbackCard, speaker);
                return;
            }

            // Second priority: separate fallback card asset if provided
            if (fallbackCard != null)
            {
                cardView.ShowEnding(fallbackCard, speaker != null ? speaker : fallbackCard.speaker);
                return;
            }

            // Third priority: generic fallback string
            Debug.LogError("[CollapseEndingBuilder] Collapse ending generation failed and no fallback text or CardData is assigned.");
            string endMessage = FallbackStrings.RunEnded(currentLanguage());
            CardData genericCard = CreateEndingCard(
                "Collapse_Ending_Generated",
                speaker,
                new LocalizedText { english = endMessage, arabic = endMessage },
                0,
                CardArtMode.None);

            cardView.ShowEnding(genericCard, speaker);
        }

        private CardData CreateGeneratedEndingCard(CardData fallbackCard, SpeakerData speaker)
        {
            return CreateEndingCard(
                fallbackCard != null ? fallbackCard.AssetName + "_Generated" : "Collapse_Ending_Generated",
                speaker != null ? speaker : fallbackCard != null ? fallbackCard.speaker : null,
                new LocalizedText
                {
                    english = FallbackStrings.GeneratingFinalRecord(GameLanguage.English),
                    arabic = FallbackStrings.GeneratingFinalRecord(GameLanguage.Arabic)
                },
                fallbackCard != null ? fallbackCard.dayAdvance : 0,
                CardArtMode.SpeakerPortrait);
        }

        // Creates or reuses the runtime-only ending card so repeated endings don't leak ScriptableObjects.
        private CardData CreateEndingCard(string assetName, SpeakerData speaker, LocalizedText description, int dayAdvance, CardArtMode artMode)
        {
            if (runtimeEndingCard == null)
            {
                runtimeEndingCard = ScriptableObject.CreateInstance<CardData>();
            }

            runtimeEndingCard.assetName = assetName;
            runtimeEndingCard.speaker = speaker;
            runtimeEndingCard.descriptionLocalized = description;
            runtimeEndingCard.dayAdvance = dayAdvance;
            runtimeEndingCard.leftChoiceLocalized = default;
            runtimeEndingCard.rightChoiceLocalized = default;
            runtimeEndingCard.leftNextCard = null;
            runtimeEndingCard.rightNextCard = null;
            runtimeEndingCard.continueNextCard = null;
            runtimeEndingCard.isLlmReactionCard = false;
            runtimeEndingCard.isPetitionCard = false;
            runtimeEndingCard.artMode = artMode;
            return runtimeEndingCard;
        }
    }
}
