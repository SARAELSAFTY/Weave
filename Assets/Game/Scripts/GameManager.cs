using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Llm;
using Game.Scripts.Runtime.Narrative;
using Game.Scripts.UI;
using UnityEngine;
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
        [SerializeField, Tooltip("Prompt templates and label defaults.")] private LlmPromptTemplates promptTemplates;
        [SerializeField, Tooltip("Global LLM settings.")] private LlmSettings llmSettings;

        private bool inputEnabled;
        private bool runInProgress;
        private bool isPaused;
        private NarrativeRunner narrativeRunner;
        private ResourceWarningMonitor resourceWarningMonitor;
        private bool showingResourceWarning;
        private CardData warningCardInstance;
        private PetitionSession currentPetitionSession;

        private LlmPromptTemplates Templates => promptTemplates != null ? promptTemplates : (narrativeDatabase != null ? narrativeDatabase.promptTemplates : null);

        public bool AcceptsChoiceInput => inputEnabled && !isPaused;
        public bool IsPaused => isPaused;
        public int CurrentDay => narrativeRunner?.Day ?? 1;
        public PlayerHistoryTracker HistoryTracker { get; private set; }
        public event Action DayChanged;

        private void Awake()
        {
            if (cardView == null)
            {
                Debug.LogError($"[GameManager] Missing required Inspector reference '{nameof(cardView)}' on '{gameObject.name}'.", this);
            }

            if (resourceState == null)
            {
                Debug.LogError($"[GameManager] Missing required Inspector reference '{nameof(resourceState)}' on '{gameObject.name}'.", this);
            }

            if (narrativeDatabase == null)
            {
                Debug.LogError($"[GameManager] Missing required Inspector reference '{nameof(narrativeDatabase)}' on '{gameObject.name}'.", this);
            }

            if (dayDisplay == null)
            {
                Debug.LogError($"[GameManager] Missing required Inspector reference '{nameof(dayDisplay)}' on '{gameObject.name}'.", this);
            }

            if (startScreenView == null)
            {
                Debug.LogError($"[GameManager] Missing required Inspector reference '{nameof(startScreenView)}' on '{gameObject.name}'.", this);
            }

            if (pauseMenuView == null)
            {
                Debug.LogError($"[GameManager] Missing required Inspector reference '{nameof(pauseMenuView)}' on '{gameObject.name}'.", this);
            }

            if (llmReactionClient == null)
            {
                Debug.LogWarning($"[GameManager] Optional Inspector reference '{nameof(llmReactionClient)}' is missing on '{gameObject.name}'. LLM reaction cards will auto-advance.", this);
            }

            if (promptTemplates == null && (narrativeDatabase == null || narrativeDatabase.promptTemplates == null))
            {
                Debug.LogWarning($"[GameManager] No LlmPromptTemplates assigned on '{gameObject.name}'. LLM prompts will have no system instructions.", this);
            }

            if (llmSettings == null)
            {
                Debug.LogWarning($"[GameManager] Optional Inspector reference '{nameof(llmSettings)}' is missing on '{gameObject.name}'. Falling back to hardcoded LLM defaults.", this);
            }

            if (cardView == null || resourceState == null || narrativeDatabase == null || dayDisplay == null || startScreenView == null || pauseMenuView == null)
            {
                enabled = false;
            }
        }

        private void Start()
        {
            if (!enabled)
            {
                return;
            }

            narrativeRunner = new NarrativeRunner(narrativeDatabase, resourceState, narrativeDatabase.resourceCatalog);
            narrativeRunner.DayChanged += HandleDayChanged;
            HistoryTracker = new PlayerHistoryTracker(resourceState, narrativeRunner, narrativeDatabase.resourceCatalog);
            resourceWarningMonitor = new ResourceWarningMonitor(narrativeDatabase.resourceCatalog, resourceState);
            resourceWarningMonitor.Reset();

            HistoryTracker.Reset();
            if (!narrativeRunner.StartRun(out string error))
            {
                Debug.LogError(error, this);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (warningCardInstance != null)
            {
                Destroy(warningCardInstance);
                warningCardInstance = null;
            }
        }

        private void Update()
        {
            if (!enabled || !runInProgress)
            {
                return;
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        private void OnEnable()
        {
            if (cardView != null)
            {
                cardView.RestartRequested += RestartRun;
            }

            if (startScreenView != null)
            {
                startScreenView.PlayRequested += BeginRun;
                startScreenView.QuitRequested += QuitGame;
            }

            if (pauseMenuView != null)
            {
                pauseMenuView.ResumeRequested += Resume;
                pauseMenuView.QuitRequested += QuitGame;
            }

            if (cardView != null)
            {
                cardView.PetitionCommandSubmitted += HandlePetitionSubmitted;
                cardView.PetitionConfirmRequested += HandlePetitionConfirmed;
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
                startScreenView.QuitRequested -= QuitGame;
            }

            if (pauseMenuView != null)
            {
                pauseMenuView.ResumeRequested -= Resume;
                pauseMenuView.QuitRequested -= QuitGame;
            }

            if (narrativeRunner != null)
            {
                narrativeRunner.DayChanged -= HandleDayChanged;
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

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
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
            yield return cardView.AnimateCardExit(choseRight, cardExitDuration);
            ShowCurrentCard();
        }

        private IEnumerator ChooseRoutine(bool choseRight)
        {
            inputEnabled = false;

            CardData currentCard = narrativeRunner.CurrentCard;
            bool isLlmCard = currentCard != null && currentCard.isLlmReactionCard;
            string chosenChoiceText = currentCard != null && !isLlmCard
                ? (choseRight ? currentCard.rightChoiceText : currentCard.leftChoiceText)
                : null;

            NarrativeStepResult result = narrativeRunner.Choose(choseRight);
            if (result.HasError)
            {
                Debug.LogError(result.error, this);
                enabled = false;
                yield break;
            }

            if (HistoryTracker != null && !string.IsNullOrEmpty(chosenChoiceText))
            {
                HistoryTracker.RecordChoice(chosenChoiceText);
            }

            yield return cardView.AnimateCardExit(choseRight, cardExitDuration);

            if (result.HasEnded)
            {
                EndRun(result.endingCard, result.isCollapseEnding);
                yield break;
            }

            if (resourceWarningMonitor != null)
            {
                resourceWarningMonitor.Tick();
                if (resourceWarningMonitor.TryGetTriggeredWarning(out ResourceData triggeredResource))
                {
                    ShowResourceWarning(triggeredResource);
                    yield break;
                }
            }

            ShowCurrentCard();
        }

        private void EndRun(CardData endingCard, bool isCollapseEnding)
        {
            inputEnabled = false;
            runInProgress = false;

            ResourceData collapsedResource = isCollapseEnding ? FindCollapsedResource() : null;
            SpeakerData speaker = isCollapseEnding && collapsedResource != null
                ? collapsedResource.warningSpeaker
                : (endingCard != null ? endingCard.speaker : null);

            cardView.ShowEnding(endingCard, speaker);

            if (!isCollapseEnding)
            {
                return; // Authored endings keep their written description; only collapse endings get an LLM epilogue.
            }

            if (llmReactionClient == null || HistoryTracker == null || narrativeDatabase.resourceCatalog == null)
            {
                return;
            }

            LlmPromptTemplates templates = Templates;
            string unknownCause = templates != null ? templates.unknownCollapseCauseLabel : string.Empty;
            string collapsedResourceName = collapsedResource != null ? collapsedResource.DisplayName : unknownCause;
            string finalResourceSummary = HistoryTracker.GetResourceSummary();
            string fullChoiceHistory = HistoryTracker.GetFullHistorySummary();

            string epilogueInstructions = templates != null ? templates.epilogueSystemInstructions : string.Empty;
            string epiloguePrompt = EpiloguePromptBuilder.Build(
                narrativeRunner.Day, collapsedResourceName, finalResourceSummary, fullChoiceHistory, epilogueInstructions);

            int? epilogueMaxTokens = llmSettings != null ? llmSettings.epilogueMaxTokens : 80;

            llmReactionClient.RequestReaction(
                epiloguePrompt,
                line =>
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        cardView.SetDescriptionText(line);
                    }
                },
                error =>
                {
                    Debug.LogWarning($"[GameManager] Epilogue generation failed: {error}");
                },
                maxTokensOverride: epilogueMaxTokens);
        }

        private ResourceData FindCollapsedResource()
        {
            foreach (ResourceData resource in narrativeDatabase.resourceCatalog.resources)
            {
                if (resource != null && resourceState.Get(resource) <= 0)
                {
                    return resource;
                }
            }
            return null;
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
            warningCardInstance.leftChoiceText = string.Empty;
            warningCardInstance.rightChoiceText = string.Empty;

            cardView.ShowLlmReaction(warningCardInstance, speaker);

            if (resourceWarningMonitor != null)
            {
                resourceWarningMonitor.OnWarningShown(resource);
            }

            string gameStateSnapshot = GetSnapshotOrDefault();

            string defaultWarningSeed = templates != null
                ? templates.defaultWarningSeedPrompt
                : string.Empty;

            string rawSeed = string.IsNullOrWhiteSpace(resource.warningSeedPrompt)
                ? defaultWarningSeed
                : resource.warningSeedPrompt;

            string seed = rawSeed.Replace("{resourceName}", resource.DisplayName);

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[GameManager] llmReactionClient is missing; skipping resource warning LLM reaction.", this);
                showingResourceWarning = false;
                ShowCurrentCard();
                return;
            }

            string personaInstructions = templates != null ? templates.personaSystemInstructions : string.Empty;
            string fullSystemPrompt = LlmPersonaPromptBuilder.Build(speaker, gameStateSnapshot, seed, personaInstructions);

            llmReactionClient.RequestReaction(
                fullSystemPrompt,
                line =>
                {
                    cardView.SetDescriptionText(line);
                    inputEnabled = true;
                },
                error =>
                {
                    Debug.LogWarning($"[GameManager] Resource warning LLM reaction failed: {error}");
                    showingResourceWarning = false;
                    ShowCurrentCard();
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
                ShowPetitionCard(card, speaker);
                return;
            }

            if (card.isLlmReactionCard)
            {
                LlmPromptTemplates templates = Templates;
                cardView.ShowLlmReaction(card, speaker);
                inputEnabled = false;

                string gameStateSnapshot = GetSnapshotOrDefault();

                string defaultReactionSeed = templates != null
                    ? templates.defaultReactionSeedPrompt
                    : string.Empty;

                string seed = string.IsNullOrWhiteSpace(card.llmPromptSeed)
                    ? defaultReactionSeed
                    : card.llmPromptSeed;

                if (llmReactionClient == null)
                {
                    Debug.LogWarning("[GameManager] llmReactionClient is missing; auto-advancing LLM reaction card.", this);
                    AutoAdvanceReactionCard();
                    return;
                }

                string personaInstructions = templates != null ? templates.personaSystemInstructions : string.Empty;
                string fullSystemPrompt = LlmPersonaPromptBuilder.Build(speaker, gameStateSnapshot: gameStateSnapshot, situationalPrompt: seed, systemInstructionsTemplate: personaInstructions);

                llmReactionClient.RequestReaction(
                    fullSystemPrompt,
                    line =>
                    {
                        cardView.SetDescriptionText(line);
                        inputEnabled = !card.IsEnding;
                    },
                    error =>
                    {
                        Debug.LogWarning($"[GameManager] LLM reaction failed: {error}");
                        AutoAdvanceReactionCard();
                    });

                return;
            }

            cardView.Show(card, speaker);
            inputEnabled = !card.IsEnding;
        }

        private void ShowPetitionCard(CardData card, SpeakerData speaker)
        {
            LlmPromptTemplates templates = Templates;
            cardView.ShowPetition(card, speaker);
            inputEnabled = false;
            currentPetitionSession = new PetitionSession(llmSettings != null ? llmSettings.petitionMaxTurns : 1);

            string gameStateSnapshot = GetSnapshotOrDefault();
            string defaultSeed = templates != null ? templates.defaultPetitionSeedPrompt : string.Empty;
            string seed = string.IsNullOrWhiteSpace(card.petitionSeedPrompt) ? defaultSeed : card.petitionSeedPrompt;

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[GameManager] llmReactionClient is missing; auto-advancing petition card.", this);
                AutoAdvancePetitionCard();
                return;
            }

            string personaInstructions = templates != null ? templates.personaSystemInstructions : string.Empty;
            string fullSystemPrompt = LlmPersonaPromptBuilder.Build(speaker, gameStateSnapshot, seed, personaInstructions);

            llmReactionClient.RequestReaction(
                fullSystemPrompt,
                line =>
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        Debug.LogWarning("[GameManager] Petition situation generation returned empty; auto-advancing.", this);
                        AutoAdvancePetitionCard();
                        return;
                    }

                    cardView.SetDescriptionText(line);
                    cardView.RevealPetitionInput();
                    cardView.UpdatePetitionPatience(0, currentPetitionSession.MaxTurns);
                },
                error =>
                {
                    Debug.LogWarning($"[GameManager] Petition situation generation failed: {error}");
                    AutoAdvancePetitionCard();
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

            SpeakerData speaker = card.speaker;
            string snapshot = GetSnapshotOrDefault();

            LlmPromptTemplates templates = Templates;
            string defaultSeed = templates != null ? templates.defaultPetitionSeedPrompt : string.Empty;
            string seed = string.IsNullOrWhiteSpace(card.petitionSeedPrompt) ? defaultSeed : card.petitionSeedPrompt;

            IReadOnlyList<ResourceData> validResources = narrativeDatabase != null && narrativeDatabase.resourceCatalog != null
                ? narrativeDatabase.resourceCatalog.resources
                : null;

            int clamp = llmSettings != null ? llmSettings.petitionResourceClampMagnitude : 20;
            string systemInstructions = templates != null ? templates.petitionSystemInstructions : string.Empty;

            if (llmReactionClient == null)
            {
                Debug.LogWarning("[GameManager] llmReactionClient is missing; auto-advancing petition card.", this);
                AutoAdvancePetitionCard();
                return;
            }

            // Snapshot before BuildMessagesForSubmission increments TurnsUsed.
            bool wasFinalTurn = currentPetitionSession.NextTurnIsFinal;

            List<GroqApiMessage> messages = currentPetitionSession.BuildMessagesForSubmission(
                playerInput, speaker, snapshot, seed, validResources, clamp, systemInstructions);

            llmReactionClient.RequestPetitionTurn(
                messages,
                (result, rawContent) => OnPetitionTurnResolved(result, rawContent, wasFinalTurn),
                OnPetitionFailed);
        }

        private void OnPetitionTurnResolved(PetitionResolution result, string rawContent, bool wasFinalTurn)
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
                return;
            }

            if (wasFinalTurn)
            {
                // Model ignored the final-turn instruction and kept deliberating — don't stall the run.
                Debug.LogWarning("[GameManager] Petition exceeded max turns without a proposal; auto-advancing.", this);
                AutoAdvancePetitionCard();
                return;
            }

            cardView.ShowPetitionDeliberation(result.reaction);
            cardView.UpdatePetitionPatience(currentPetitionSession.TurnsUsed, currentPetitionSession.MaxTurns);
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

            if (applyResult.historyTag != null && HistoryTracker != null)
            {
                HistoryTracker.RecordHistoryTag(applyResult.historyTag);
            }

            currentPetitionSession = null;
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

            if (result.HasEnded)
            {
                EndRun(result.endingCard, result.isCollapseEnding);
                yield break;
            }

            if (resourceWarningMonitor != null)
            {
                resourceWarningMonitor.Tick();
                if (resourceWarningMonitor.TryGetTriggeredWarning(out ResourceData triggeredResource))
                {
                    ShowResourceWarning(triggeredResource);
                    yield break;
                }
            }

            ShowCurrentCard();
        }

        private void OnPetitionFailed(LlmRequestError error)
        {
            Debug.LogWarning($"[GameManager] Petition resolution failed: {error}");
            AutoAdvancePetitionCard();
        }

        private IEnumerator ResolvePetitionAdvanceRoutine()
        {
            NarrativeStepResult result = narrativeRunner.Choose(false);
            if (result.HasError)
            {
                Debug.LogError(result.error, this);
                enabled = false;
                yield break;
            }

            if (result.HasEnded)
            {
                EndRun(result.endingCard, result.isCollapseEnding);
                yield break;
            }

            if (resourceWarningMonitor != null)
            {
                resourceWarningMonitor.Tick();
                if (resourceWarningMonitor.TryGetTriggeredWarning(out ResourceData triggeredResource))
                {
                    ShowResourceWarning(triggeredResource);
                    yield break;
                }
            }

            ShowCurrentCard();
        }

        private void AutoAdvancePetitionCard()
        {
            currentPetitionSession = null;
            StartCoroutine(ResolvePetitionAdvanceRoutine());
        }

        private string GetSnapshotOrDefault()
        {
            return HistoryTracker != null ? HistoryTracker.GetSnapshot() : "Kingdom Status: Unknown";
        }

        private void AutoAdvanceReactionCard()
        {
            NarrativeStepResult result = narrativeRunner.Choose(false);
            if (result.HasError)
            {
                Debug.LogError(result.error, this);
                enabled = false;
                return;
            }

            if (result.HasEnded)
            {
                EndRun(result.endingCard, result.isCollapseEnding);
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

        private void HandleDayChanged()
        {
            RefreshDayDisplay();
            DayChanged?.Invoke();
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