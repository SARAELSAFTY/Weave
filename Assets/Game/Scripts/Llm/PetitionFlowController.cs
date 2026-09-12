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
    /// proposal and global choice input remain with GameManager, reached via the delegates passed to the constructor.
    /// The opening line is requested through the same petition JSON contract as later turns and recorded as the
    /// first assistant turn, so the model always sees how the audience began.</remarks>
    public class PetitionFlowController : AudienceFlowController
    {
        private readonly ResourceState resourceState;

        // Typed view of the shared session holder; only this class ever assigns a PetitionSession.
        private PetitionSession currentPetitionSession
        {
            get => (PetitionSession)Session;
            set => Session = value;
        }

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
            : base(cardView, llmReactionClient, llmSettings, database, narrativeRunner, historyTracker,
                currentLanguage, setInputEnabled, beginConfirmAdvance, scheduleSubmitReenable)
        {
            this.resourceState = resourceState;
        }

        /// <summary>Presents the petition card: resolves the petitioner, opens a new session and requests the opening line.</summary>
        public void ShowPetitionCard(CardData card)
        {
            currentSpeaker = ResolveSpeaker(card);
            LlmPromptTemplates templates = Templates;
            cardView.ShowPetition(card, currentSpeaker);
            cardView.SetPetitionConfirmButtonLabel(LlmFallbackText.PetitionConfirmLabel(templates, currentLanguage()));
            setInputEnabled(false);
            currentPetitionSession = new PetitionSession(
                llmSettings != null ? llmSettings.ResolvePetitionTurnLimit() : LlmSettings.DefaultPetitionTurnLimit);

            string snapshot = GetSnapshotOrDefault(PetitionHistoryCount, PastPetitionChatCount);
            string seed = card.EffectivePetitionSeed(templates);
            string systemPrompt = SpeakerPromptBuilder.BuildPetitionTurnPrompt(
                currentSpeaker,
                snapshot,
                situationalPrompt: null,
                GetResourceCatalog(),
                PetitionResourceClampMagnitude,
                templates,
                currentLanguage());
            currentPetitionSession.Initialize(systemPrompt);

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[PetitionFlowController] llmReactionClient is missing; showing fallback opening line.", cardView);
                DeliverPetitionOpening(LlmFallbackText.PetitionOpening(templates, currentLanguage()), null, null);
                return;
            }

            llmReactionClient.RequestPetitionTurn(
                currentPetitionSession.BuildOpeningMessages(seed),
                currentLanguage(),
                (resolution, rawContent) => DeliverPetitionOpening(resolution.reaction, rawContent, resolution.speakerName),
                error =>
                {
                    Debug.LogWarning($"[PetitionFlowController] Petition opening request failed: {error}");
                    DeliverPetitionOpening(LlmFallbackText.PetitionOpening(templates, currentLanguage()), null, null);
                });
        }

        /// <summary>Handles a ruler input submission during an active petition audience.</summary>
        public void HandlePetitionSubmitted(string playerInput)
        {
            CardData card = narrativeRunner.CurrentCard;
            if (card == null || !card.isPetitionCard || currentPetitionSession == null
                || !currentPetitionSession.HasOpening || currentPetitionSession.TurnsExhausted)
            {
                return;
            }

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[PetitionFlowController] llmReactionClient is missing; petition turn cannot be sent.", cardView);
                OnPetitionFailed(LlmRequestError.NotConfigured);
                return;
            }

            cardView.SetPetitionSubmitting(true);

            List<GroqApiMessage> messages = currentPetitionSession.BuildMessagesForSubmission(playerInput);
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
                historyTracker.RecordPetitionTranscript(currentPetitionSession.GetTranscript());
            }

            EndAudience();
            cardView.SetPetitionSubmitting(true);
            beginConfirmAdvance();
        }

        /// <summary>Destroys the temporary petitioner speaker, if one was generated; call from the owner's OnDestroy.</summary>
        public void CleanupPetitionSpeaker()
        {
            CleanupAudienceSpeaker();
        }

        // Shows the petitioner's opening line, records it as the first conversation turn (so later
        // requests know how the audience began), names a generated commoner, and opens the input.
        private void DeliverPetitionOpening(string openingLine, string rawHistoryContent, string speakerName)
        {
            if (currentPetitionSession == null)
            {
                return;
            }

            if (TempSpeakerFactory.TryApplyGeneratedName(currentSpeaker, speakerName))
            {
                cardView.RefreshSpeakerName();
            }

            string line = !string.IsNullOrWhiteSpace(openingLine) ? openingLine : string.Empty;
            currentPetitionSession.RecordOpening(rawHistoryContent ?? line, line);
            cardView.SetDescriptionText(line);
            cardView.ShowPetitionInput();
            cardView.UpdatePetitionDots(currentPetitionSession.TurnsRemaining);
        }

        private bool IsDetectSpamActive => llmSettings != null ? llmSettings.IsDetectSpamEnabled() : LlmKeyStore.HasActiveKey;

        private void OnPetitionTurnResolved(PetitionResolution result, string rawContent)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.reaction))
            {
                OnPetitionFailed(LlmRequestError.EmptyResponse);
                return;
            }

            currentPetitionSession?.RecordReply(result, rawContent, IsDetectSpamActive);
            cardView.SetPetitionSubmitting(false);

            if (currentPetitionSession != null && currentPetitionSession.TurnsExhausted)
            {
                CardData currentCard = narrativeRunner.CurrentCard;
                SpeakerData speaker = currentSpeaker != null ? currentSpeaker : (currentCard != null ? currentCard.speaker : null);
                HandlePetitionTurnsExhausted(currentCard, speaker);
                return;
            }

            if (result.IsProposal)
            {
                cardView.ShowPetitionProposal(result.reaction);
                cardView.UpdatePetitionDots(currentPetitionSession.TurnsRemaining);
                return;
            }

            cardView.ShowPetitionDeliberation(result.reaction);
            cardView.UpdatePetitionDots(currentPetitionSession.TurnsRemaining);
        }

        private void HandlePetitionTurnsExhausted(CardData card, SpeakerData speaker)
        {
            cardView.UpdatePetitionDots(0);
            cardView.SetPetitionSubmitting(true);

            LlmPromptTemplates templates = Templates;
            string seed = templates != null ? templates.petitionClosingSeedPrompt : string.Empty;

            LlmFallbackText.RequestSpeakerLine(llmReactionClient, templates, GetResourceCatalog(),
                speaker, GetSnapshotOrDefault(PetitionHistoryCount, PastPetitionChatCount), seed, currentLanguage(),
                LlmFallbackText.PetitionClosing(templates, currentLanguage()),
                closingLine => FinalizePetitionExhaustion(card, closingLine));
        }

        private void FinalizePetitionExhaustion(CardData card, string closingLine)
        {
            historyTracker?.RecordPetitionTranscript(currentPetitionSession?.GetTranscript());
            EndAudience();
            cardView.SetPetitionSubmitting(false);
            cardView.ConvertPetitionToNormalChoices(card, closingLine);
            setInputEnabled(true);
        }

        private void OnPetitionFailed(LlmRequestError error)
        {
            OnAudienceFailed("Petition resolution failed", error);
        }
    }
}
