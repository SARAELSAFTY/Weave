using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Narrative;
using Game.Scripts.UI;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Runs the multi-turn petition audience state machine: opening line, ruler submissions,
    /// proposal confirmation, turn exhaustion with a closing line, and failure cooldowns.</summary>
    /// <remarks>Constructed and owned by <see cref="GameManager"/>; narrative advancement after a confirmed
    /// proposal and global choice input remain with GameManager, reached via the delegates passed to the constructor.</remarks>
    public class PetitionFlowController
    {
        private readonly CardView cardView;
        private readonly LlmReactionClient llmReactionClient;
        private readonly LlmSettings llmSettings;
        private readonly NarrativeDatabase database;
        private readonly NarrativeRunner narrativeRunner;
        private readonly ResourceState resourceState;
        private readonly PlayerHistoryTracker historyTracker;
        private readonly Func<GameLanguage> currentLanguage;
        private readonly Action<bool> setInputEnabled;
        private readonly Action beginConfirmAdvance;
        private readonly Action<float> scheduleSubmitReenable;

        private PetitionSession currentPetitionSession;
        private SpeakerData currentPetitionSpeaker;
        private bool currentPetitionSpeakerIsTemp;

        public PetitionFlowController(
            CardView cardView,
            LlmReactionClient llmReactionClient,
            LlmSettings llmSettings,
            NarrativeDatabase database,
            NarrativeRunner narrativeRunner,
            ResourceState resourceState,
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
            this.resourceState = resourceState;
            this.historyTracker = historyTracker;
            this.currentLanguage = currentLanguage;
            this.setInputEnabled = setInputEnabled;
            this.beginConfirmAdvance = beginConfirmAdvance;
            this.scheduleSubmitReenable = scheduleSubmitReenable;
        }

        /// <summary>Presents the petition card: resolves the petitioner, opens a new session and requests the opening line.</summary>
        public void ShowPetitionCard(CardData card)
        {
            currentPetitionSpeaker = ResolvePetitioner(card);
            LlmPromptTemplates templates = database != null ? database.promptTemplates : null;
            cardView.ShowPetition(card, currentPetitionSpeaker);
            setInputEnabled(false);
            currentPetitionSession = new PetitionSession(llmSettings != null ? llmSettings.ResolvePetitionTurnLimit() : 3);

            string snapshot = GetPetitionSnapshotOrDefault();
            string seed = card.EffectivePetitionSeed(templates);

            RequestSpeakerLineWithFallback(currentPetitionSpeaker, snapshot, seed,
                FallbackStrings.PetitionOpeningUnavailable(currentLanguage()),
                line =>
                {
                    cardView.SetDescriptionText(line);
                    cardView.ShowPetitionInput();
                    cardView.UpdatePetitionDots(currentPetitionSession.TurnsRemaining);
                });
        }

        /// <summary>Handles a ruler input submission during an active petition audience.</summary>
        public void HandlePetitionSubmitted(string playerInput)
        {
            CardData card = narrativeRunner.CurrentCard;
            if (card == null || !card.isPetitionCard || currentPetitionSession == null || currentPetitionSession.TurnsExhausted)
            {
                return;
            }

            cardView.SetPetitionSubmitting(true);

            SpeakerData speaker = currentPetitionSpeaker != null ? currentPetitionSpeaker : card.speaker;
            string snapshot = GetPetitionSnapshotOrDefault();
            LlmPromptTemplates templates = database != null ? database.promptTemplates : null;
            string seed = card.EffectivePetitionSeed(templates);

            IReadOnlyList<ResourceData> validResources = database != null && database.resourceCatalog != null
                ? database.resourceCatalog.resources
                : null;

            int clamp = llmSettings != null ? llmSettings.petitionResourceClampMagnitude : 20;

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[PetitionFlowController] llmReactionClient is missing; petition turn cannot be sent.", cardView);
                OnPetitionFailed(LlmRequestError.NotConfigured);
                return;
            }

            List<GroqApiMessage> messages = currentPetitionSession.BuildMessagesForSubmission(
                playerInput, speaker, snapshot, seed, validResources, clamp, templates, currentLanguage());

            llmReactionClient.RequestPetitionTurn(messages, currentLanguage(), OnPetitionTurnResolved, OnPetitionFailed);
        }

        /// <summary>Handles the ruler confirming a proposal: applies resource deltas, records history and advances the narrative.</summary>
        public void HandlePetitionConfirmed()
        {
            if (currentPetitionSession == null || !currentPetitionSession.AwaitingConfirmation)
            {
                return;
            }

            PetitionResolution proposal = currentPetitionSession.LastProposal;
            int clampMagnitude = llmSettings != null ? llmSettings.petitionResourceClampMagnitude : 20;
            PetitionApplyResult applyResult = PetitionResolutionApplier.Apply(proposal, database?.resourceCatalog, clampMagnitude);

            if (applyResult.resourceChange.HasValue)
            {
                resourceState.Apply(applyResult.resourceChange.Value);
            }

            if (applyResult.historyTag != null && historyTracker != null)
            {
                historyTracker.RecordHistoryTag(applyResult.historyTag);
            }

            if (historyTracker != null)
            {
                historyTracker.RecordPetitionTranscript(currentPetitionSession.GetTranscript());
            }

            currentPetitionSession = null;
            CleanupPetitionSpeaker();
            cardView.SetPetitionSubmitting(true);
            beginConfirmAdvance();
        }

        /// <summary>Destroys the temporary petitioner speaker, if one was generated; call from the owner's OnDestroy.</summary>
        public void CleanupPetitionSpeaker()
        {
            if (currentPetitionSpeakerIsTemp && currentPetitionSpeaker != null)
            {
                UnityEngine.Object.Destroy(currentPetitionSpeaker);
            }

            currentPetitionSpeaker = null;
            currentPetitionSpeakerIsTemp = false;
        }

        private void OnPetitionTurnResolved(PetitionResolution result, string rawContent)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.reaction))
            {
                OnPetitionFailed(LlmRequestError.EmptyResponse);
                return;
            }

            currentPetitionSession?.RecordReply(result, rawContent);
            cardView.SetPetitionSubmitting(false);

            if (result.IsProposal)
            {
                cardView.ShowPetitionProposal(result.reaction);
                if (currentPetitionSession != null && currentPetitionSession.TurnsExhausted)
                {
                    cardView.DisablePetitionFurtherInput();
                    cardView.UpdatePetitionDots(0);
                }
                else
                {
                    cardView.UpdatePetitionDots(currentPetitionSession.TurnsRemaining);
                }
                return;
            }

            if (currentPetitionSession != null && currentPetitionSession.TurnsExhausted)
            {
                CardData currentCard = narrativeRunner.CurrentCard;
                SpeakerData speaker = currentPetitionSpeaker != null ? currentPetitionSpeaker : (currentCard != null ? currentCard.speaker : null);
                HandlePetitionTurnsExhausted(currentCard, speaker);
                return;
            }

            cardView.ShowPetitionDeliberation(result.reaction);
            cardView.UpdatePetitionDots(currentPetitionSession.TurnsRemaining);
        }

        private void HandlePetitionTurnsExhausted(CardData card, SpeakerData speaker)
        {
            cardView.UpdatePetitionDots(0);
            cardView.SetPetitionSubmitting(true);

            LlmPromptTemplates templates = database != null ? database.promptTemplates : null;
            string seed = templates != null ? templates.petitionClosingSeedPrompt : string.Empty;

            RequestSpeakerLineWithFallback(speaker, GetPetitionSnapshotOrDefault(), seed,
                FallbackStrings.PetitionClosingLine(currentLanguage()),
                closingLine => FinalizePetitionExhaustion(card, closingLine));
        }

        private void FinalizePetitionExhaustion(CardData card, string closingLine)
        {
            historyTracker?.RecordPetitionTranscript(currentPetitionSession?.GetTranscript());
            currentPetitionSession = null;
            CleanupPetitionSpeaker();
            cardView.SetPetitionSubmitting(false);
            cardView.ConvertPetitionToNormalChoices(card, closingLine);
            setInputEnabled(true);
        }

        // Applies a brief cooldown before re-enabling the submit button to prevent rapid-fire retries
        // that could compound rate-limiting or error states.
        private void OnPetitionFailed(LlmRequestError error)
        {
            Debug.LogWarning($"[PetitionFlowController] Petition resolution failed: {error}");
            bool isRateLimited = error == LlmRequestError.RateLimited;
            string message = isRateLimited
                ? FallbackStrings.PetitionRateLimited(currentLanguage())
                : FallbackStrings.PetitionSendFailed(currentLanguage());

            cardView.ShowPetitionSubmitFailed(message);
            float cooldown = llmSettings != null ? llmSettings.petitionRetryCooldownSeconds : 2f;
            scheduleSubmitReenable(cooldown);
        }

        private SpeakerData ResolvePetitioner(CardData card)
        {
            if (card.petitionerSource == PetitionerSource.DefinedSpeaker && card.speaker != null)
            {
                currentPetitionSpeakerIsTemp = false;
                return card.speaker;
            }

            currentPetitionSpeakerIsTemp = true;
            return BuildCommonerSpeaker();
        }

        // Creates a runtime-only SpeakerData; caller must track currentPetitionSpeakerIsTemp
        // so CleanupPetitionSpeaker can destroy it later.
        private SpeakerData BuildCommonerSpeaker()
        {
            LlmPromptTemplates templates = database != null ? database.promptTemplates : null;
            SpeakerData commoner = ScriptableObject.CreateInstance<SpeakerData>();
            commoner.displayNameLocalized = new LocalizedText { english = "A Common Subject", arabic = "أحد رعايا التاج" };
            commoner.llmPersonaPrompt = templates != null ? templates.defaultCommonerPersona : string.Empty;
            return commoner;
        }

        private string GetPetitionSnapshotOrDefault()
        {
            int narrativeHistoryEntryCount = llmSettings != null ? llmSettings.petitionHistoryCount : 0;
            int petitionTranscriptCount = llmSettings != null ? llmSettings.pastPetitionChatCount : 0;
            return historyTracker != null
                ? historyTracker.GetSnapshot(narrativeHistoryEntryCount, petitionTranscriptCount, currentLanguage())
                : FallbackStrings.KingdomStatusUnknown(currentLanguage());
        }

        // Requests a single-turn speaker line, routing request failures and a missing client to the same
        // continuation with a fallback line so each caller handles one success path instead of three callbacks.
        private void RequestSpeakerLineWithFallback(SpeakerData speaker, string gameStateSnapshot, string seed,
            string fallbackText, Action<string> onLine)
        {
            if (llmReactionClient == null)
            {
                Debug.LogWarning("[PetitionFlowController] llmReactionClient is missing; showing fallback line.", cardView);
                onLine(fallbackText);
                return;
            }

            LlmPromptTemplates templates = database != null ? database.promptTemplates : null;
            IReadOnlyList<ResourceData> resources = database != null && database.resourceCatalog != null
                ? database.resourceCatalog.resources
                : null;

            GameLanguage language = currentLanguage();
            string fullSystemPrompt = SpeakerPromptBuilder.BuildPersonaPrompt(speaker, gameStateSnapshot, seed, templates, language, resources);

            llmReactionClient.RequestReaction(fullSystemPrompt, SpeakerPromptBuilder.SingleTurnUserMessage, language,
                onLine,
                error =>
                {
                    Debug.LogWarning($"[PetitionFlowController] Petition speaker line request failed: {error}");
                    onLine(fallbackText);
                });
        }
    }
}
