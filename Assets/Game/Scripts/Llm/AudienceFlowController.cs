using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Narrative;
using Game.Scripts.UI;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Shared skeleton for the audience flow controllers: constructor dependencies, speaker lifecycle
    /// (defined or generated commoner), kingdom-state snapshots, resource-catalog access, failure cooldowns, and audience teardown.</summary>
    /// <remarks>Each subclass runs one LLM audience on a card: <see cref="PetitionFlowController"/> adds the
    /// resource state and the proposal/exhaustion logic, <see cref="ChatFlowController"/> adds plain chat turns.</remarks>
    public abstract class AudienceFlowController
    {
        protected readonly CardView cardView;
        protected readonly LlmReactionClient llmReactionClient;
        protected readonly LlmSettings llmSettings;
        protected readonly NarrativeDatabase database;
        protected readonly NarrativeRunner narrativeRunner;
        protected readonly PlayerHistoryTracker historyTracker;
        protected readonly Func<GameLanguage> currentLanguage;
        protected readonly Action<bool> setInputEnabled;
        protected readonly Action beginConfirmAdvance;
        protected readonly Action<float> scheduleSubmitReenable;

        /// <summary>The speaker currently presenting the audience; a runtime commoner when generated.</summary>
        protected SpeakerData currentSpeaker;
        /// <summary>True when <see cref="currentSpeaker"/> was generated at runtime and must be destroyed on teardown.</summary>
        protected bool currentSpeakerIsTemp;

        /// <summary>The active conversation session for the current audience; null when no audience is running.</summary>
        protected AudienceSession Session { get; set; }

        /// <summary>Prompt templates from the narrative database; null when the database or templates are unassigned.</summary>
        protected LlmPromptTemplates Templates => database != null ? database.promptTemplates : null;

        /// <summary>Maximum absolute resource delta communicated to the model in petition prompts.</summary>
        protected int PetitionResourceClampMagnitude =>
            llmSettings != null ? llmSettings.petitionResourceClampMagnitude : LlmSettings.DefaultPetitionResourceClampMagnitude;

        /// <summary>Recent ruler decisions included in reaction/chat snapshots.</summary>
        protected int ReactionHistoryCount => llmSettings != null ? llmSettings.reactionHistoryCount : LlmSettings.DefaultReactionHistoryCount;

        /// <summary>Prior petition turns carried forward in petition conversations.</summary>
        protected int PetitionHistoryCount => llmSettings != null ? llmSettings.petitionHistoryCount : LlmSettings.DefaultPetitionHistoryCount;

        /// <summary>Completed past petitions appended as additional context.</summary>
        protected int PastPetitionChatCount => llmSettings != null ? llmSettings.pastPetitionChatCount : LlmSettings.DefaultPastPetitionChatCount;

        /// <summary>Initializes the shared controller dependencies; called by subclass constructors.</summary>
        protected AudienceFlowController(
            CardView cardView,
            LlmReactionClient llmReactionClient,
            LlmSettings llmSettings,
            NarrativeDatabase database,
            NarrativeRunner narrativeRunner,
            PlayerHistoryTracker historyTracker,
            Func<GameLanguage> currentLanguage,
            Action<bool> setInputEnabled,
            Action beginConfirmAdvance,
            Action<float> scheduleSubmitReenable)
        {
            this.cardView = cardView;
            this.llmReactionClient = llmReactionClient;
            this.llmSettings = llmSettings;
            this.database = database;
            this.narrativeRunner = narrativeRunner;
            this.historyTracker = historyTracker;
            this.currentLanguage = currentLanguage;
            this.setInputEnabled = setInputEnabled;
            this.beginConfirmAdvance = beginConfirmAdvance;
            this.scheduleSubmitReenable = scheduleSubmitReenable;
        }

        /// <summary>Returns the card's assigned speaker, or creates a runtime commoner; sets <see cref="currentSpeakerIsTemp"/>.</summary>
        /// <param name="card">Card being presented.</param>
        /// <remarks>The caller must destroy generated speakers via <see cref="CleanupAudienceSpeaker"/>.</remarks>
        protected SpeakerData ResolveSpeaker(CardData card)
        {
            if (card.petitionerSource == PetitionerSource.DefinedSpeaker && card.speaker != null)
            {
                currentSpeakerIsTemp = false;
                return card.speaker;
            }

            currentSpeakerIsTemp = true;
            return TempSpeakerFactory.CreateCommoner(Templates);
        }

        /// <summary>Destroys the temporary audience speaker, if one was generated.</summary>
        protected void CleanupAudienceSpeaker()
        {
            if (currentSpeakerIsTemp && currentSpeaker != null)
            {
                UnityEngine.Object.Destroy(currentSpeaker);
            }

            currentSpeaker = null;
            currentSpeakerIsTemp = false;
        }

        /// <summary>Ends the active audience, dropping the session and any temporary speaker.</summary>
        protected void EndAudience()
        {
            Session = null;
            CleanupAudienceSpeaker();
        }

        /// <summary>Returns the kingdom-state snapshot for prompts, or the fallback status line when no tracker exists.</summary>
        /// <param name="narrativeHistoryEntryCount">Recent narrative history entries to include.</param>
        /// <param name="petitionTranscriptCount">Recent petition transcripts to include.</param>
        protected string GetSnapshotOrDefault(int narrativeHistoryEntryCount, int petitionTranscriptCount)
        {
            return historyTracker != null
                ? historyTracker.GetSnapshot(narrativeHistoryEntryCount, petitionTranscriptCount, currentLanguage())
                : FallbackStrings.KingdomStatusUnknown(currentLanguage());
        }

        // Resource catalog used by prompt building; null when the database or catalog is unassigned.
        protected IReadOnlyList<ResourceData> GetResourceCatalog()
        {
            return database != null && database.resourceCatalog != null
                ? database.resourceCatalog.resources
                : null;
        }

        // Applies a brief cooldown before re-enabling the submit button to prevent rapid-fire retries
        // that could compound rate-limiting or error states.
        protected void OnAudienceFailed(string failureDescription, LlmRequestError error)
        {
            Debug.LogWarning($"[{GetType().Name}] {failureDescription}: {error}");
            bool isRateLimited = error == LlmRequestError.RateLimited;
            LlmPromptTemplates templates = Templates;
            string message = isRateLimited
                ? LlmFallbackText.PetitionRateLimited(templates, currentLanguage())
                : LlmFallbackText.PetitionSendFailed(templates, currentLanguage());

            cardView.ShowPetitionSubmitFailed(message);
            float cooldown = llmSettings != null ? llmSettings.petitionRetryCooldownSeconds : LlmSettings.DefaultPetitionRetryCooldownSeconds;
            scheduleSubmitReenable(cooldown);
        }
    }
}
