using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Runtime.Llm;
using Game.Scripts.Runtime.Narrative;
using Game.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Scripts
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Card UI component in scene.")] private CardView cardView;
        [SerializeField, Tooltip("Tracks kingdom resource values.")] private ResourceState resourceState;
        [SerializeField, Tooltip("Story database containing cards.")] private NarrativeDatabase narrativeDatabase;
        [SerializeField, Min(0.05f), Tooltip("Card exit animation duration in seconds.")] private float cardExitDuration = 0.25f;
        [SerializeField, Tooltip("UI text element showing the current day.")] private DayDisplay dayDisplay;
        [SerializeField, Tooltip("Start screen shown before a run begins.")] private StartScreenView startScreenView;
        [SerializeField, Tooltip("Pause overlay shown during a run.")] private PauseMenuView pauseMenuView;
        [SerializeField, Tooltip("Shared chat service for LLM reaction cards.")] private LlmReactionClient llmReactionClient;
        [SerializeField, Tooltip("Global LLM settings.")] private LlmSettings llmSettings;

        private bool inputEnabled;
        private bool runInProgress;
        private bool isPaused;
        private NarrativeRunner narrativeRunner;
        private ResourceWarningMonitor resourceWarningMonitor;
        private bool showingResourceWarning;
        private CardData warningCardInstance;
        private CardData generatedCollapseEndingCard;
        private PetitionSession currentPetitionSession;
        private SpeakerData currentPetitionSpeaker;
        private bool currentPetitionSpeakerIsTemp;

        // NarrativeDatabase.promptTemplates is the single source of truth for prompt text - see LlmPromptTemplates.
        private LlmPromptTemplates Templates => narrativeDatabase != null ? narrativeDatabase.promptTemplates : null;

        private GameLanguage CurrentLanguage => LanguageManager.Instance != null
            ? LanguageManager.Instance.CurrentLanguage
            : GameLanguage.English;

        public bool AcceptsChoiceInput => inputEnabled && !isPaused;
        private int CurrentDay => narrativeRunner?.Day ?? 1;
        private PlayerHistoryTracker historyTracker;

        private void Awake()
        {
            bool missingReference =
                InspectorValidation.RequireField(cardView, nameof(cardView), nameof(GameManager), this) |
                InspectorValidation.RequireField(resourceState, nameof(resourceState), nameof(GameManager), this) |
                InspectorValidation.RequireField(narrativeDatabase, nameof(narrativeDatabase), nameof(GameManager), this) |
                InspectorValidation.RequireField(dayDisplay, nameof(dayDisplay), nameof(GameManager), this) |
                InspectorValidation.RequireField(startScreenView, nameof(startScreenView), nameof(GameManager), this) |
                InspectorValidation.RequireField(pauseMenuView, nameof(pauseMenuView), nameof(GameManager), this);

            if (llmReactionClient == null)
            {
                Debug.LogWarning($"[GameManager] Optional Inspector reference '{nameof(llmReactionClient)}' is missing on '{gameObject.name}'. LLM reaction cards will show a fallback line.", this);
            }

            if (narrativeDatabase != null && narrativeDatabase.promptTemplates == null)
            {
                Debug.LogWarning($"[GameManager] '{narrativeDatabase.name}' has no LlmPromptTemplates assigned. LLM prompts will have no system instructions.", this);
            }

            if (llmSettings == null)
            {
                Debug.LogWarning($"[GameManager] Optional Inspector reference '{nameof(llmSettings)}' is missing on '{gameObject.name}'. Falling back to hardcoded LLM defaults.", this);
            }

            if (missingReference)
            {
                // Missing required references are unrecoverable; disabling permanently stops this
                // component (OnDisable also detaches the view event wiring).
                enabled = false;
            }
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            narrativeRunner = new NarrativeRunner(narrativeDatabase, resourceState);
            narrativeRunner.DayChanged += HandleDayChanged;
            historyTracker = new PlayerHistoryTracker(resourceState, narrativeRunner, narrativeDatabase.resourceCatalog);
            resourceWarningMonitor = new ResourceWarningMonitor(narrativeDatabase.resourceCatalog, resourceState);

            if (!narrativeRunner.StartRun(out string error))
            {
                Debug.LogError(error, this);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (narrativeRunner != null)
            {
                narrativeRunner.DayChanged -= HandleDayChanged;
            }

            if (warningCardInstance != null)
            {
                Destroy(warningCardInstance);
                warningCardInstance = null;
            }

            if (generatedCollapseEndingCard != null)
            {
                Destroy(generatedCollapseEndingCard);
                generatedCollapseEndingCard = null;
            }

            CleanupPetitionSpeaker();
        }

        private void Update()
        {
            if (!enabled || !runInProgress)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        private void OnEnable()
        {
            if (cardView != null)
            {
                cardView.RestartRequested += RestartRun;
                cardView.PetitionCommandSubmitted += HandlePetitionSubmitted;
                cardView.PetitionConfirmRequested += HandlePetitionConfirmed;
            }

            if (startScreenView != null)
            {
                startScreenView.PlayRequested += BeginRun;
            }

            if (pauseMenuView != null)
            {
                pauseMenuView.ResumeRequested += Resume;
            }
        }

        private void OnDisable()
        {
            if (cardView != null)
            {
                cardView.RestartRequested -= RestartRun;
                cardView.PetitionCommandSubmitted -= HandlePetitionSubmitted;
                cardView.PetitionConfirmRequested -= HandlePetitionConfirmed;
            }

            if (startScreenView != null)
            {
                startScreenView.PlayRequested -= BeginRun;
            }

            if (pauseMenuView != null)
            {
                pauseMenuView.ResumeRequested -= Resume;
            }
        }

        private void BeginRun()
        {
            if (!enabled)
            {
                return;
            }

            startScreenView.Hide();
            runInProgress = true;
            showingResourceWarning = false;
            resourceWarningMonitor?.Reset();
            RefreshDayDisplay();
            ShowCurrentCard();
        }

        private void TogglePause()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        private void Pause()
        {
            if (isPaused)
            {
                return;
            }

            isPaused = true;
            pauseMenuView.Show();
        }

        private void Resume()
        {
            if (!isPaused)
            {
                return;
            }

            isPaused = false;
            pauseMenuView.Hide();
        }

        public void ChooseSide(bool choseRight)
        {
            if (!AcceptsChoiceInput)
            {
                return;
            }

            if (showingResourceWarning)
            {
                StartCoroutine(DismissResourceWarningRoutine(choseRight));
                return;
            }

            StartCoroutine(ChooseRoutine(choseRight));
        }

        private IEnumerator DismissResourceWarningRoutine(bool choseRight)
        {
            inputEnabled = false;
            showingResourceWarning = false;
            cardView.PlayConfirmAnimation(choseRight);
            yield return cardView.AnimateCardExit(choseRight, cardExitDuration);
            ShowCurrentCard();
        }

        private IEnumerator ChooseRoutine(bool choseRight)
        {
            inputEnabled = false;
            cardView.PlayConfirmAnimation(choseRight);

            CardData currentCard = narrativeRunner.CurrentCard;
            bool isLlmCard = currentCard != null && currentCard.isLlmReactionCard;
            string chosenChoiceText = currentCard != null && !isLlmCard
                ? (choseRight ? currentCard.GetRightChoice(CurrentLanguage) : currentCard.GetLeftChoice(CurrentLanguage))
                : null;

            NarrativeStepResult result = narrativeRunner.Choose(choseRight);

            if (result.HasError)
            {
                Debug.LogError(result.error, this);
                enabled = false;
                yield break;
            }

            if (historyTracker != null && !string.IsNullOrEmpty(chosenChoiceText))
            {
                historyTracker.RecordChoice(chosenChoiceText);
            }

            yield return cardView.AnimateCardExit(choseRight, cardExitDuration);
            HandleStepResult(result);
        }

        private void EndRun(CardData endingCard, bool isCollapseEnding, ResourceData collapsedResource)
        {
            inputEnabled = false;
            runInProgress = false;

            SpeakerData speaker = isCollapseEnding && collapsedResource != null
                ? collapsedResource.warningSpeaker
                : (endingCard != null ? endingCard.speaker : null);

            if (!isCollapseEnding)
            {
                cardView.ShowEnding(endingCard, speaker);
                return;
            }

            if (endingCard == null || llmReactionClient == null || historyTracker == null ||
                narrativeDatabase.resourceCatalog == null || collapsedResource == null)
            {
                ShowCollapseFallback(endingCard, speaker);
                return;
            }

            CardData generatedCard = CreateGeneratedCollapseEndingCard(endingCard, speaker);
            cardView.ShowEnding(generatedCard, speaker);

            LlmPromptTemplates templates = Templates;
            string collapsedResourceName = collapsedResource.GetDisplayName(CurrentLanguage);
            string finalResourceSummary = historyTracker.GetResourceSummary(CurrentLanguage);
            string fullChoiceHistory = historyTracker.GetFullHistorySummary();

            string epiloguePrompt = SpeakerPromptBuilder.BuildEpiloguePrompt(
                narrativeRunner.Day, collapsedResourceName, finalResourceSummary, fullChoiceHistory, templates, CurrentLanguage);

            int? epilogueMaxTokens = llmSettings != null ? llmSettings.epilogueMaxTokens : 80;

            llmReactionClient.RequestReaction(
                epiloguePrompt,
                templates != null ? templates.singleTurnUserMessage : string.Empty,
                CurrentLanguage,
                line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        // Fill both languages: a language toggle mid-generation would otherwise
                        // leave the other language frozen on the "generating" placeholder.
                        generatedCard.descriptionLocalized = new LocalizedText { english = line, arabic = line };
                        cardView.ShowEnding(generatedCard, speaker);
                        return;
                    }

                    Debug.LogWarning("[GameManager] Epilogue generation returned an empty response; using the fallback ending card.");
                    ShowCollapseFallback(endingCard, speaker);
                },
                error =>
                {
                    Debug.LogWarning($"[GameManager] Epilogue generation failed: {error}");
                    ShowCollapseFallback(endingCard, speaker);
                },
                maxTokensOverride: epilogueMaxTokens);
        }

        private CardData CreateGeneratedCollapseEndingCard(CardData fallbackCard, SpeakerData speaker)
        {
            if (generatedCollapseEndingCard == null)
            {
                generatedCollapseEndingCard = ScriptableObject.CreateInstance<CardData>();
            }

            generatedCollapseEndingCard.assetName = fallbackCard.AssetName + "_Generated";
            generatedCollapseEndingCard.displayName = fallbackCard.DisplayName;
            generatedCollapseEndingCard.speaker = speaker != null ? speaker : fallbackCard.speaker;
            generatedCollapseEndingCard.descriptionLocalized = new LocalizedText
            {
                english = FallbackStrings.GeneratingFinalRecord(GameLanguage.English),
                arabic = FallbackStrings.GeneratingFinalRecord(GameLanguage.Arabic)
            };
            generatedCollapseEndingCard.dayAdvance = fallbackCard.dayAdvance;
            generatedCollapseEndingCard.leftChoiceLocalized = default;
            generatedCollapseEndingCard.rightChoiceLocalized = default;
            generatedCollapseEndingCard.leftNextCard = null;
            generatedCollapseEndingCard.rightNextCard = null;
            generatedCollapseEndingCard.continueNextCard = null;
            generatedCollapseEndingCard.isLlmReactionCard = false;
            generatedCollapseEndingCard.isPetitionCard = false;
            return generatedCollapseEndingCard;
        }

        private void ShowCollapseFallback(CardData fallbackCard, SpeakerData speaker)
        {
            if (fallbackCard == null)
            {
                Debug.LogError("[GameManager] Collapse ending generation failed and no fallback CardData is assigned.", this);
                return;
            }

            cardView.ShowEnding(fallbackCard, speaker != null ? speaker : fallbackCard.speaker);
        }

        private void ShowResourceWarning(ResourceData resource)
        {
            if (resource == null || resource.warningSpeaker == null)
            {
                ShowCurrentCard();
                return;
            }

            SpeakerData speaker = resource.warningSpeaker;
            if (string.IsNullOrWhiteSpace(speaker.llmPersonaPrompt))
            {
                ShowCurrentCard();
                return;
            }

            showingResourceWarning = true;
            inputEnabled = false;

            LlmPromptTemplates templates = Templates;

            if (warningCardInstance == null)
            {
                warningCardInstance = ScriptableObject.CreateInstance<CardData>();
            }
            warningCardInstance.isLlmReactionCard = true;
            warningCardInstance.speaker = resource.warningSpeaker;
            warningCardInstance.leftChoiceLocalized = default;
            warningCardInstance.rightChoiceLocalized = default;

            cardView.ShowLlmReaction(warningCardInstance, speaker);

            if (resourceWarningMonitor != null)
            {
                resourceWarningMonitor.OnWarningShown(resource);
            }

            string gameStateSnapshot = GetReactionSnapshotOrDefault();

            string seed = PromptTemplateUtility.Fill(
                templates != null ? templates.defaultWarningSeedPrompt : string.Empty,
                "resourceName", resource.GetDisplayName(CurrentLanguage));

            RequestSpeakerReaction(speaker, gameStateSnapshot, seed,
                line =>
                {
                    cardView.SetDescriptionText(line);
                    inputEnabled = true;
                },
                error =>
                {
                    Debug.LogWarning($"[GameManager] Resource warning LLM reaction failed: {error}");
                    cardView.SetDescriptionText(FallbackStrings.WarningUnavailable(CurrentLanguage));
                    inputEnabled = true;
                },
                () =>
                {
                    Debug.LogWarning("[GameManager] llmReactionClient is missing; showing fallback resource warning.", this);
                    cardView.SetDescriptionText(FallbackStrings.WarningUnavailable(CurrentLanguage));
                    inputEnabled = true;
                });
        }

        private void ShowCurrentCard()
        {
            CardData card = narrativeRunner.CurrentCard;
            if (card == null)
            {
                return;
            }

            SpeakerData speaker = card.speaker;
            if (card.isPetitionCard)
            {
                currentPetitionSpeaker = ResolvePetitioner(card);
                ShowPetitionCard(card, currentPetitionSpeaker);
                return;
            }

            if (card.isLlmReactionCard)
            {
                LlmPromptTemplates templates = Templates;
                cardView.ShowLlmReaction(card, speaker);
                inputEnabled = false;

                string gameStateSnapshot = GetReactionSnapshotOrDefault();

                string seed = card.EffectiveReactionSeed(templates);

                RequestSpeakerReaction(speaker, gameStateSnapshot, seed,
                    line =>
                    {
                        cardView.SetDescriptionText(line);
                        inputEnabled = !card.IsEnding;
                    },
                    error =>
                    {
                        Debug.LogWarning($"[GameManager] LLM reaction failed: {error}");
                        cardView.SetDescriptionText(FallbackStrings.ReactionUnavailable(CurrentLanguage));
                        inputEnabled = !card.IsEnding;
                    },
                    () =>
                    {
                        Debug.LogWarning("[GameManager] llmReactionClient is missing; showing fallback reaction text.", this);
                        cardView.SetDescriptionText(FallbackStrings.ReactionUnavailable(CurrentLanguage));
                        inputEnabled = !card.IsEnding;
                    });

                return;
            }

            cardView.Show(card, speaker);
            inputEnabled = !card.IsEnding;
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

        private SpeakerData BuildCommonerSpeaker()
        {
            SpeakerData commoner = ScriptableObject.CreateInstance<SpeakerData>();
            commoner.displayName = "A Common Subject";
            commoner.displayNameLocalized = new LocalizedText { english = "A Common Subject", arabic = "أحد رعايا التاج" };
            commoner.llmPersonaPrompt = Templates != null ? Templates.defaultCommonerPersona : string.Empty;
            return commoner;
        }

        private void CleanupPetitionSpeaker()
        {
            if (currentPetitionSpeakerIsTemp && currentPetitionSpeaker != null)
            {
                Destroy(currentPetitionSpeaker);
            }

            currentPetitionSpeaker = null;
            currentPetitionSpeakerIsTemp = false;
        }

        private void ShowPetitionCard(CardData card, SpeakerData speaker)
        {
            LlmPromptTemplates templates = Templates;
            cardView.ShowPetition(card, speaker);
            inputEnabled = false;
            currentPetitionSession = new PetitionSession(llmSettings != null ? llmSettings.petitionSpamDotBudget : 3);

            string gameStateSnapshot = GetPetitionSnapshotOrDefault();
            string seed = card.EffectivePetitionSeed(templates);

            RequestSpeakerReaction(speaker, gameStateSnapshot, seed,
                line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        Debug.LogWarning("[GameManager] Petition situation generation returned empty; using fallback opening.", this);
                        line = FallbackStrings.PetitionOpeningUnavailable(CurrentLanguage);
                    }

                    cardView.SetDescriptionText(line);
                    cardView.ShowPetitionInput();
                    cardView.UpdatePetitionDots(currentPetitionSession.DotsRemaining);
                },
                error =>
                {
                    Debug.LogWarning($"[GameManager] Petition situation generation failed: {error}");
                    cardView.SetDescriptionText(FallbackStrings.PetitionOpeningUnavailable(CurrentLanguage));
                    cardView.ShowPetitionInput();
                    cardView.UpdatePetitionDots(currentPetitionSession.DotsRemaining);
                },
                () =>
                {
                    Debug.LogWarning("[GameManager] llmReactionClient is missing; showing fallback petition opening.", this);
                    cardView.SetDescriptionText(FallbackStrings.PetitionOpeningUnavailable(CurrentLanguage));
                    cardView.ShowPetitionInput();
                    cardView.UpdatePetitionDots(currentPetitionSession.DotsRemaining);
                });
        }

        private void HandlePetitionSubmitted(string playerInput)
        {
            CardData card = narrativeRunner.CurrentCard;
            if (card == null || !card.isPetitionCard || currentPetitionSession == null)
            {
                return;
            }

            cardView.SetPetitionSubmitting(true);

            SpeakerData speaker = currentPetitionSpeaker != null ? currentPetitionSpeaker : card.speaker;
            string snapshot = GetPetitionSnapshotOrDefault();

            LlmPromptTemplates templates = Templates;
            string seed = card.EffectivePetitionSeed(templates);

            IReadOnlyList<ResourceData> validResources = narrativeDatabase != null && narrativeDatabase.resourceCatalog != null
                ? narrativeDatabase.resourceCatalog.resources
                : null;

            int clamp = llmSettings != null ? llmSettings.petitionResourceClampMagnitude : 20;

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[GameManager] llmReactionClient is missing; petition turn cannot be sent.", this);
                OnPetitionFailed(LlmRequestError.NotConfigured);
                return;
            }

            List<GroqApiMessage> messages = currentPetitionSession.BuildMessagesForSubmission(
                playerInput, speaker, snapshot, seed, validResources, clamp, templates, CurrentLanguage);

            llmReactionClient.RequestPetitionTurn(
                messages,
                CurrentLanguage,
                OnPetitionTurnResolved,
                OnPetitionFailed);
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

            if (currentPetitionSession != null && currentPetitionSession.DotsExhausted)
            {
                CardData currentCard = narrativeRunner.CurrentCard;
                SpeakerData speaker = currentPetitionSpeaker != null ? currentPetitionSpeaker : (currentCard != null ? currentCard.speaker : null);
                HandlePetitionDotsExhausted(currentCard, speaker);
                return;
            }

            if (result.IsProposal)
            {
                cardView.ShowPetitionProposal(result.reaction);
                cardView.UpdatePetitionDots(currentPetitionSession.DotsRemaining);
                return;
            }

            cardView.ShowPetitionDeliberation(result.reaction);
            cardView.UpdatePetitionDots(currentPetitionSession.DotsRemaining);
        }

        private void HandlePetitionDotsExhausted(CardData card, SpeakerData speaker)
        {
            cardView.UpdatePetitionDots(0);
            cardView.SetPetitionSubmitting(true);

            string seed = Templates != null ? Templates.defaultPetitionExhaustedSeedPrompt : string.Empty;
            string snapshot = GetPetitionSnapshotOrDefault();

            RequestSpeakerReaction(speaker, snapshot, seed,
                closingLine => FinalizePetitionExhaustion(card, string.IsNullOrWhiteSpace(closingLine) ? FallbackStrings.PetitionClosingLine(CurrentLanguage) : closingLine),
                error => FinalizePetitionExhaustion(card, FallbackStrings.PetitionClosingLine(CurrentLanguage)),
                () => FinalizePetitionExhaustion(card, FallbackStrings.PetitionClosingLine(CurrentLanguage)));
        }

        private void FinalizePetitionExhaustion(CardData card, string closingLine)
        {
            historyTracker?.RecordPetitionTranscript(currentPetitionSession?.GetTranscript());
            currentPetitionSession = null;
            CleanupPetitionSpeaker();
            cardView.SetPetitionSubmitting(false);
            cardView.ConvertPetitionToNormalChoices(card, closingLine);
            inputEnabled = true;
        }

        private void HandlePetitionConfirmed()
        {
            if (currentPetitionSession == null || !currentPetitionSession.AwaitingConfirmation)
            {
                return;
            }

            PetitionResolution proposal = currentPetitionSession.LastProposal;
            int clampMagnitude = llmSettings != null ? llmSettings.petitionResourceClampMagnitude : 20;
            PetitionApplyResult applyResult = PetitionResolutionApplier.Apply(proposal, narrativeDatabase?.resourceCatalog, clampMagnitude);

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
            StartCoroutine(ConfirmPetitionAdvanceRoutine());
        }

        private IEnumerator ConfirmPetitionAdvanceRoutine()
        {
            inputEnabled = false;

            NarrativeStepResult result = narrativeRunner.Choose(true);
            if (result.HasError)
            {
                Debug.LogError(result.error, this);
                enabled = false;
                yield break;
            }

            yield return cardView.AnimateCardExit(true, cardExitDuration);
            HandleStepResult(result);
        }

        private void OnPetitionFailed(LlmRequestError error)
        {
            Debug.LogWarning($"[GameManager] Petition resolution failed: {error}");
            bool isRateLimited = error == LlmRequestError.RateLimited;
            string message = isRateLimited
                ? FallbackStrings.PetitionRateLimited(CurrentLanguage)
                : FallbackStrings.PetitionSendFailed(CurrentLanguage);

            cardView.ShowPetitionSubmitFailed(message);
            float cooldown = llmSettings != null ? llmSettings.petitionRetryCooldownSeconds : 2f;
            StartCoroutine(ReenablePetitionSubmitAfterDelay(cooldown));
        }

        private IEnumerator ReenablePetitionSubmitAfterDelay(float delaySeconds)
        {
            if (delaySeconds > 0f)
            {
                yield return new WaitForSeconds(delaySeconds);
            }

            cardView.SetPetitionSubmitting(false);
        }

        private string GetReactionSnapshotOrDefault()
        {
            return GetSnapshotOrDefault(
                llmSettings != null ? llmSettings.reactionHistoryCount : 0,
                llmSettings != null ? llmSettings.pastPetitionChatCount : 0);
        }

        private string GetPetitionSnapshotOrDefault()
        {
            return GetSnapshotOrDefault(
                llmSettings != null ? llmSettings.petitionHistoryCount : 0,
                llmSettings != null ? llmSettings.pastPetitionChatCount : 0);
        }

        private string GetSnapshotOrDefault(int narrativeHistoryEntryCount, int petitionTranscriptCount)
        {
            return historyTracker != null
                ? historyTracker.GetSnapshot(narrativeHistoryEntryCount, petitionTranscriptCount, CurrentLanguage)
                : FallbackStrings.KingdomStatusUnknown(CurrentLanguage);
        }

        // Shared continuation once a step result is known to be error-free.
        private void HandleStepResult(NarrativeStepResult result)
        {
            if (result.HasEnded)
            {
                EndRun(result.endingCard, result.isCollapseEnding, result.collapsedResource);
                return;
            }

            if (resourceWarningMonitor != null)
            {
                resourceWarningMonitor.Tick();
                if (resourceWarningMonitor.TryGetTriggeredWarning(out ResourceData triggeredResource))
                {
                    ShowResourceWarning(triggeredResource);
                    return;
                }
            }

            ShowCurrentCard();
        }

        // Builds the persona system prompt and dispatches a reaction request. Callers own success/failure
        // behavior and their own fallback for a missing client.
        private void RequestSpeakerReaction(SpeakerData speaker, string gameStateSnapshot, string seed,
            Action<string> onSuccess, Action<LlmRequestError> onFailure, Action onMissingClient, int? maxTokensOverride = null)
        {
            if (llmReactionClient == null)
            {
                onMissingClient?.Invoke();
                return;
            }

            IReadOnlyList<ResourceData> resources = narrativeDatabase != null && narrativeDatabase.resourceCatalog != null
                ? narrativeDatabase.resourceCatalog.resources
                : null;

            LlmPromptTemplates templates = Templates;
            string fullSystemPrompt = SpeakerPromptBuilder.BuildPersonaPrompt(
                speaker, gameStateSnapshot, seed, templates, CurrentLanguage, resources);

            llmReactionClient.RequestReaction(fullSystemPrompt, templates != null ? templates.singleTurnUserMessage : string.Empty, CurrentLanguage, onSuccess, onFailure, maxTokensOverride);
        }

        private void HandleDayChanged()
        {
            RefreshDayDisplay();
        }

        private void RefreshDayDisplay()
        {
            dayDisplay.SetDay(CurrentDay);
        }

        private void RestartRun()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(scene.path))
            {
                SceneManager.LoadScene(scene.path);
            }
            else
            {
                SceneManager.LoadScene(scene.buildIndex);
            }
        }
    }
}
