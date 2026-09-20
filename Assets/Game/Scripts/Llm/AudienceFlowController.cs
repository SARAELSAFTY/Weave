using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Narrative;
using Game.Scripts.UI;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Runs one LLM audience on a petition or chat card: opening line, ruler submissions,
    /// proposal confirmation or plain-chat end, turn exhaustion with a closing line, and failure convert.</summary>
    /// <remarks>Constructed and owned by <see cref="GameManager"/>; narrative advancement after confirmation
    /// and global choice input remain with GameManager, reached via the delegates passed to the constructor.
    /// Card mode flags are read once in <see cref="ShowAudienceCard"/> and stored on the session;
    /// nothing below re-reads them.</remarks>
    public class AudienceFlowController
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

        private SpeakerData currentSpeaker;
        private bool isGeneratedCommoner;
        private AudienceSession currentSession;

        private int generation;

        public AudienceFlowController(
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

        /// <summary>Presents the audience card: resolves the speaker, opens a new session and requests the opening line.</summary>
        /// <param name="card">Petition or chat card being presented; chat wins when both flags are set.</param>
        public void ShowAudienceCard(CardData card)
        {
            NextAudience();
            bool isChat = card != null && card.isChatCard;
            currentSpeaker = ResolveSpeaker(card);
            LlmPromptTemplates templates = Templates;

            if (isChat)
            {
                cardView.ShowChat(card, currentSpeaker);
                cardView.SetPetitionConfirmButtonLabel(LlmFallbackText.ChatEndLabel(templates, currentLanguage()));
            }
            else
            {
                cardView.ShowPetition(card, currentSpeaker);
                cardView.SetPetitionConfirmButtonLabel(LlmFallbackText.PetitionConfirmLabel(templates, currentLanguage()));
            }

            setInputEnabled(false);
            currentSession = new AudienceSession(
                isChat ? 0 : (llmSettings != null ? llmSettings.ResolvePetitionTurnLimit() : LlmSettings.DefaultPetitionTurnLimit),
                unlimitedTurns: isChat,
                expectsProposal: !isChat);
            string snapshot = GetSnapshotOrDefault(isChat ? ReactionHistoryCount : PetitionHistoryCount, isChat ? 0 : PastPetitionChatCount);
            string seed = isChat ? card.EffectiveChatSeed(templates) : card.EffectivePetitionSeed(templates);

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[AudienceFlowController] llmReactionClient is missing; showing fallback opening line.", cardView);
                DeliverAudienceOpening(isChat ? LlmFallbackText.Reaction(templates, currentLanguage()) : LlmFallbackText.PetitionOpening(templates, currentLanguage()), null, null);
                return;
            }

            int openingGeneration = CurrentGeneration;
            if (!isChat || isGeneratedCommoner)
            {
                string systemPrompt = isGeneratedCommoner
                    ? SpeakerPromptBuilder.BuildPetitionTurnPrompt(
                        Templates != null ? Templates.defaultCommonerPersona : string.Empty,
                        snapshot,
                        situationalPrompt: null,
                        GetResourceCatalog(),
                        PetitionResourceClampMagnitude,
                        templates,
                        currentLanguage())
                    : SpeakerPromptBuilder.BuildPetitionTurnPrompt(
                        currentSpeaker,
                        snapshot,
                        situationalPrompt: null,
                        GetResourceCatalog(),
                        PetitionResourceClampMagnitude,
                        templates,
                        currentLanguage());
                currentSession.Initialize(systemPrompt);
                List<GroqApiMessage> openingMessages = currentSession.BuildOpeningMessages(seed);
                string fallbackLine = isChat
                    ? LlmFallbackText.Reaction(templates, currentLanguage())
                    : LlmFallbackText.PetitionOpening(templates, currentLanguage());
                llmReactionClient.RequestPetitionTurn(
                    openingMessages,
                    currentLanguage(),
                    (resolution, rawContent) =>
                    {
                        if (openingGeneration != CurrentGeneration)
                        {
                            return;
                        }

                        DeliverAudienceOpening(resolution.reaction, rawContent, resolution.speakerName);
                    },
                    error =>
                    {
                        if (openingGeneration != CurrentGeneration)
                        {
                            return;
                        }

                        Debug.LogWarning($"[AudienceFlowController] Audience opening request failed: {error}");
                        DeliverAudienceOpening(fallbackLine, null, null);
                    });
                return;
            }

            string chatPrompt = SpeakerPromptBuilder.BuildChatTurnPrompt(currentSpeaker, snapshot, templates, currentLanguage());
            currentSession.Initialize(chatPrompt);
            llmReactionClient.RequestChatTurn(
                currentSession.BuildOpeningMessages(seed),
                currentLanguage(),
                line =>
                {
                    if (openingGeneration != CurrentGeneration)
                    {
                        return;
                    }

                    DeliverAudienceOpening(line, line, null);
                },
                error =>
                {
                    if (openingGeneration != CurrentGeneration)
                    {
                        return;
                    }

                    Debug.LogWarning($"[AudienceFlowController] Audience opening request failed: {error}");
                    DeliverAudienceOpening(LlmFallbackText.Reaction(templates, currentLanguage()), null, null);
                });
        }

        public void HandleAudienceSubmitted(string playerInput)
        {
            if (currentSession == null || !currentSession.HasOpening)
            {
                return;
            }

            if (currentSession.ExpectsProposal && currentSession.TurnsExhausted)
            {
                return;
            }

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[AudienceFlowController] llmReactionClient is missing; audience turn cannot be sent.", cardView);
                OnAudienceTurnFailed(LlmRequestError.NotConfigured, CurrentGeneration);
                return;
            }

            cardView.SetPetitionSubmitting(true);

            int requestGeneration = CurrentGeneration;
            if (currentSession.ExpectsProposal)
            {
                List<GroqApiMessage> messages = currentSession.BuildMessagesForSubmission(playerInput);
                llmReactionClient.RequestPetitionTurn(messages, currentLanguage(),
                    (result, rawContent) => OnPetitionTurnResolved(result, rawContent, requestGeneration),
                    error => OnAudienceTurnFailed(error, requestGeneration));
                return;
            }

            List<GroqApiMessage> chatMessages = currentSession.BuildTurnMessages(playerInput);
            llmReactionClient.RequestChatTurn(chatMessages, currentLanguage(), reply =>
            {
                if (requestGeneration != CurrentGeneration || currentSession == null)
                {
                    return;
                }

                currentSession.RecordTurn(playerInput, reply);
                cardView.SetPetitionSubmitting(false);
                cardView.ShowChatReply(reply);
            }, error => OnAudienceTurnFailed(error, requestGeneration));
        }

        /// <summary>Handles the ruler confirming a proposal or ending a chat: applies resource deltas for proposals, then advances the narrative.</summary>
        public void HandleAudienceConfirmed()
        {
            if (currentSession == null)
            {
                return;
            }

            if (currentSession.ExpectsProposal)
            {
                if (!currentSession.AwaitingConfirmation)
                {
                    return;
                }

                PetitionResolution proposal = currentSession.LastProposal;
                PetitionApplyResult applyResult = PetitionResolutionApplier.Apply(
                    proposal, database?.resourceCatalog, PetitionResourceClampMagnitude);

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
                    historyTracker.RecordPetitionTranscript(currentSession.GetTranscript());
                }
            }

            EndAudience();
            cardView.SetPetitionSubmitting(true);
            beginConfirmAdvance();
        }

        /// <summary>Cancels the active audience so late callbacks are ignored; call on restart or card change.</summary>
        public void Cancel()
        {
            generation++;
            EndAudience();
        }

        public void CleanupSpeaker()
        {
            CleanupAudienceSpeaker();
        }

        /// <summary>Identity of the current audience; request callbacks capture it and drop stale responses.</summary>
        private int CurrentGeneration => generation;

        /// <summary>Starts a new audience identity; call when presenting a card so prior callbacks are ignored.</summary>
        private void NextAudience()
        {
            generation++;
        }

        private LlmPromptTemplates Templates => database != null ? database.promptTemplates : null;

        private int PetitionResourceClampMagnitude =>
            llmSettings != null ? llmSettings.petitionResourceClampMagnitude : LlmSettings.DefaultPetitionResourceClampMagnitude;

        private int ReactionHistoryCount => llmSettings != null ? llmSettings.reactionHistoryCount : LlmSettings.DefaultReactionHistoryCount;

        private int PetitionHistoryCount => llmSettings != null ? llmSettings.petitionHistoryCount : LlmSettings.DefaultPetitionHistoryCount;

        private int PastPetitionChatCount => llmSettings != null ? llmSettings.pastPetitionChatCount : LlmSettings.DefaultPastPetitionChatCount;

        /// <remarks>Generated commoners need no SpeakerData: null is returned and the default
        /// commoner persona plus the model's speakerName string are used instead.</remarks>
        private SpeakerData ResolveSpeaker(CardData card)
        {
            if (card != null && (card.isPetitionCard || card.isChatCard) &&
                card.petitionerSource == PetitionerSource.GeneratedCommoner)
            {
                isGeneratedCommoner = true;
                return null;
            }

            isGeneratedCommoner = false;
            return card != null ? card.speaker : null;
        }

        private void CleanupAudienceSpeaker()
        {
            currentSpeaker = null;
            isGeneratedCommoner = false;
        }

        private void EndAudience()
        {
            currentSession = null;
            CleanupAudienceSpeaker();
        }

        public string GetSnapshotOrDefault(int narrativeHistoryEntryCount, int petitionTranscriptCount)
        {
            return historyTracker != null
                ? historyTracker.GetSnapshot(narrativeHistoryEntryCount, petitionTranscriptCount, currentLanguage())
                : FallbackStrings.KingdomStatusUnknown(currentLanguage());
        }

        /// <summary>Snapshot for single-turn reaction and warning prompts: recent decisions plus past petitions.</summary>
        public string GetReactionSnapshot()
        {
            return GetSnapshotOrDefault(ReactionHistoryCount, PastPetitionChatCount);
        }

        public IReadOnlyList<ResourceData> GetResourceCatalog()
        {
            return database != null && database.resourceCatalog != null
                ? database.resourceCatalog.resources
                : null;
        }

        // Defined speakers keep their curated localized display names; the model's speakerName
        // must never clobber them (it once replaced e.g. "التاجر" with "Silvio, the Guild Master").
        // Generated commoners have no SpeakerData, so the name is shown directly via CardView.
        private void DeliverAudienceOpening(string openingLine, string rawHistoryContent, string speakerName)
        {
            if (currentSession == null)
            {
                return;
            }

            if (isGeneratedCommoner)
            {
                cardView.SetGeneratedSpeakerName(speakerName);
            }

            if (currentSession.ExpectsProposal)
            {
                string line = !string.IsNullOrWhiteSpace(openingLine) ? openingLine : string.Empty;
                currentSession.RecordOpening(rawHistoryContent ?? line, line);
                cardView.SetDescriptionText(line);
                cardView.ShowPetitionInput();
                cardView.UpdatePetitionDots(currentSession.TurnsRemaining);
                return;
            }

            string chatLine = openingLine ?? string.Empty;
            currentSession.RecordOpening(rawHistoryContent ?? chatLine);
            cardView.ShowChatReply(chatLine);
        }

        private bool IsDetectSpamActive => llmSettings != null ? llmSettings.IsDetectSpamEnabled() : LlmKeyStore.HasActiveKey;

        private void OnPetitionTurnResolved(PetitionResolution result, string rawContent, int requestGeneration)
        {
            if (requestGeneration != CurrentGeneration || currentSession == null)
            {
                return;
            }

            if (result == null || string.IsNullOrWhiteSpace(result.reaction))
            {
                OnAudienceTurnFailed(LlmRequestError.EmptyResponse, requestGeneration);
                return;
            }

            currentSession.RecordReply(result, rawContent, IsDetectSpamActive);
            cardView.SetPetitionSubmitting(false);

            if (result.IsProposal)
            {
                cardView.ShowPetitionProposal(result.reaction);
                cardView.UpdatePetitionDots(currentSession.TurnsRemaining);
                return;
            }

            if (currentSession.TurnsExhausted)
            {
                CardData currentCard = narrativeRunner.CurrentCard;
                SpeakerData speaker = currentSpeaker != null ? currentSpeaker : (currentCard != null ? currentCard.speaker : null);
                HandlePetitionTurnsExhausted(currentCard, speaker, isGeneratedCommoner);
                return;
            }

            cardView.ShowPetitionDeliberation(result.reaction);
            cardView.UpdatePetitionDots(currentSession.TurnsRemaining);
        }

        private void HandlePetitionTurnsExhausted(CardData card, SpeakerData speaker, bool generatedCommoner = false)
        {
            cardView.UpdatePetitionDots(0);
            cardView.SetPetitionSubmitting(true);

            LlmPromptTemplates templates = Templates;
            string seed = templates != null ? templates.petitionClosingSeedPrompt : string.Empty;

            if (generatedCommoner)
            {
                LlmFallbackText.RequestSpeakerLine(llmReactionClient, templates, GetResourceCatalog(),
                    Templates != null ? Templates.defaultCommonerPersona : string.Empty,
                    GetSnapshotOrDefault(PetitionHistoryCount, PastPetitionChatCount), seed, currentLanguage(),
                    LlmFallbackText.PetitionClosing(templates, currentLanguage()),
                    closingLine => FinalizePetitionExhaustion(card, closingLine));
                return;
            }

            LlmFallbackText.RequestSpeakerLine(llmReactionClient, templates, GetResourceCatalog(),
                speaker, GetSnapshotOrDefault(PetitionHistoryCount, PastPetitionChatCount), seed, currentLanguage(),
                LlmFallbackText.PetitionClosing(templates, currentLanguage()),
                closingLine => FinalizePetitionExhaustion(card, closingLine));
        }

        private void FinalizePetitionExhaustion(CardData card, string closingLine)
        {
            historyTracker?.RecordPetitionTranscript(currentSession?.GetTranscript());
            EndAudience();
            cardView.SetPetitionSubmitting(false);
            cardView.ConvertPetitionToNormalChoices(card, closingLine);
            setInputEnabled(true);
        }

        private void OnAudienceTurnFailed(LlmRequestError error, int requestGeneration)
        {
            if (requestGeneration != CurrentGeneration || currentSession == null)
            {
                return;
            }

            if (!currentSession.ExpectsProposal)
            {
                OnAudienceFailed("Chat turn failed", error);
                return;
            }

            Debug.LogWarning($"[AudienceFlowController] Petition turn failed, converting to choice: {error}");
            CardData card = narrativeRunner.CurrentCard;
            historyTracker?.RecordPetitionTranscript(currentSession.GetTranscript());
            EndAudience();
            cardView.SetPetitionSubmitting(false);
            cardView.ConvertPetitionToNormalChoices(card, LlmFallbackText.PetitionClosing(Templates, currentLanguage()));
            setInputEnabled(true);
        }

        // Applies a brief cooldown before re-enabling the submit button to prevent rapid-fire retries
        // that could compound rate-limiting or error states.
        private void OnAudienceFailed(string failureDescription, LlmRequestError error)
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
