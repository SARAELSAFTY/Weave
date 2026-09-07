using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Narrative;
using Game.Scripts.UI;
using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Runs a free chat audience on a chat card: opening line, unlimited ruler replies, and an
    /// end-audience button that advances the narrative.</summary>
    /// <remarks>Constructed and owned by <see cref="GameManager"/>. Chat is talk only - no JSON contract,
    /// no resource changes, and nothing recorded to kingdom history. Conversation history (including the
    /// speaker's opening line) is carried in <see cref="ChatSession"/> so the model always knows what was said.
    /// Generated commoners get their opening through the petition JSON contract so the model names the persona;
    /// every other turn is plain chat text.</remarks>
    public class ChatFlowController : AudienceFlowController
    {
        // Typed view of the shared session holder; only this class ever assigns a ChatSession.
        private ChatSession currentSession
        {
            get => (ChatSession)Session;
            set => Session = value;
        }

        public ChatFlowController(
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
            : base(cardView, llmReactionClient, llmSettings, database, narrativeRunner, historyTracker,
                currentLanguage, setInputEnabled, beginConfirmAdvance, scheduleSubmitReenable)
        {
        }

        /// <summary>Presents the chat card: resolves the speaker, opens a new session and requests the opening line.</summary>
        public void ShowChatCard(CardData card)
        {
            currentSpeaker = ResolveSpeaker(card);
            LlmPromptTemplates templates = Templates;
            cardView.ShowChat(card, currentSpeaker);
            cardView.SetPetitionConfirmButtonLabel(LlmFallbackText.ChatEndLabel(templates, currentLanguage()));
            setInputEnabled(false);
            currentSession = new ChatSession();
            currentSession.Initialize(SpeakerPromptBuilder.BuildChatTurnPrompt(
                currentSpeaker, GetSnapshotOrDefault(ReactionHistoryCount, 0), templates, currentLanguage()));

            string seed = card.EffectiveChatSeed(templates);

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[ChatFlowController] llmReactionClient is missing; showing fallback opening line.", cardView);
                DeliverChatOpening(LlmFallbackText.Reaction(templates, currentLanguage()), null);
                return;
            }

            if (currentSpeakerIsTemp)
            {
                RequestGeneratedOpening(templates, seed);
                return;
            }

            llmReactionClient.RequestChatTurn(
                currentSession.BuildOpeningMessages(seed),
                currentLanguage(),
                line => DeliverChatOpening(line, line),
                error =>
                {
                    Debug.LogWarning($"[ChatFlowController] Chat opening request failed: {error}");
                    DeliverChatOpening(LlmFallbackText.Reaction(templates, currentLanguage()), null);
                });
        }

        /// <summary>Handles a ruler input submission during an active chat audience. Returns false when no chat
        /// is active, letting the caller route the input to the petition flow instead.</summary>
        public bool HandleChatSubmitted(string playerInput)
        {
            CardData card = narrativeRunner.CurrentCard;
            if (card == null || !card.isChatCard || currentSession == null || !currentSession.HasOpening)
            {
                return false;
            }

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[ChatFlowController] llmReactionClient is missing; chat turn cannot be sent.", cardView);
                OnChatFailed(LlmRequestError.NotConfigured);
                return true;
            }

            cardView.SetPetitionSubmitting(true);

            List<GroqApiMessage> messages = currentSession.BuildTurnMessages(playerInput);
            llmReactionClient.RequestChatTurn(messages, currentLanguage(), reply =>
            {
                currentSession?.RecordTurn(playerInput, reply);
                cardView.SetPetitionSubmitting(false);
                cardView.ShowChatReply(reply);
            }, OnChatFailed);
            return true;
        }

        /// <summary>Ends the active chat audience and advances the narrative. Returns false when no chat is active,
        /// letting the caller route the confirm event to the petition flow instead.</summary>
        public bool HandleChatEndRequested()
        {
            if (currentSession == null)
            {
                return false;
            }

            EndAudience();
            cardView.SetPetitionSubmitting(true);
            beginConfirmAdvance();
            return true;
        }

        /// <summary>Destroys the temporary chat speaker, if one was generated; call from the owner's OnDestroy.</summary>
        public void CleanupChatSpeaker()
        {
            CleanupAudienceSpeaker();
        }

        // Generated commoners have no display name yet, so the opening goes through the petition JSON
        // contract (which returns speakerName) before the conversation switches to plain chat text.
        private void RequestGeneratedOpening(LlmPromptTemplates templates, string seed)
        {
            string openingSystemPrompt = SpeakerPromptBuilder.BuildPetitionTurnPrompt(
                currentSpeaker,
                GetSnapshotOrDefault(ReactionHistoryCount, 0),
                situationalPrompt: null,
                GetResourceCatalog(),
                PetitionResourceClampMagnitude,
                templates,
                currentLanguage());

            List<GroqApiMessage> openingMessages = new List<GroqApiMessage>
            {
                new GroqApiMessage { role = "system", content = openingSystemPrompt },
                new GroqApiMessage { role = "user", content = seed }
            };

            llmReactionClient.RequestPetitionTurn(openingMessages, currentLanguage(),
                (resolution, rawContent) =>
                {
                    if (TempSpeakerFactory.TryApplyGeneratedName(currentSpeaker, resolution.speakerName))
                    {
                        cardView.RefreshSpeakerName();
                    }
                    DeliverChatOpening(resolution.reaction, rawContent);
                },
                error =>
                {
                    Debug.LogWarning($"[ChatFlowController] Generated chat opening request failed: {error}");
                    DeliverChatOpening(LlmFallbackText.Reaction(templates, currentLanguage()), null);
                });
        }

        // Shows the speaker's opening line and records it as the first conversation turn so later
        // requests know how the audience began.
        private void DeliverChatOpening(string openingLine, string rawHistoryContent)
        {
            if (currentSession == null)
            {
                return;
            }

            string line = openingLine ?? string.Empty;
            currentSession.RecordOpening(rawHistoryContent ?? line);
            cardView.ShowChatReply(line);
        }

        private void OnChatFailed(LlmRequestError error)
        {
            OnAudienceFailed("Chat turn failed", error);
        }
    }
}
