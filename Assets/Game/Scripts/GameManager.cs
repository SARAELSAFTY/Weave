using System;
using System.Collections;
using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Llm;
using Game.Scripts.Runtime.Narrative;
using Game.Scripts.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts
{
    /// <summary>Coordinates narrative flow, card UI updates, and run lifecycle.</summary>
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

        private LlmPromptTemplates Templates => promptTemplates != null ? promptTemplates : (narrativeDatabase != null ? narrativeDatabase.promptTemplates : null);

        /// <summary>Gets whether card choice input is currently accepted.</summary>
        public bool AcceptsChoiceInput => inputEnabled && !isPaused;

        /// <summary>Gets whether the run is currently paused.</summary>
        public bool IsPaused => isPaused;

        /// <summary>Gets the current in-game day.</summary>
        public int CurrentDay => narrativeRunner != null ? narrativeRunner.Day : 1;

        /// <summary>Gets the tracker for recent player choices.</summary>
        public PlayerHistoryTracker HistoryTracker { get; private set; }

        /// <summary>Raised after the current day value changes.</summary>
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

            if (cardView == null || resourceState == null || narrativeDatabase == null || dayDisplay == null || startScreenView == null || pauseMenuView == null)
            {
                enabled = false;
                return;
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
                return;
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
        }

        private void OnDisable()
        {
            if (cardView != null)
            {
                cardView.RestartRequested -= RestartRun;
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

        /// <summary>Hides the start screen and shows the first card.</summary>
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

        /// <summary>Toggles the pause overlay on or off.</summary>
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

        /// <summary>Quits the application (or exits play mode in the editor).</summary>
        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Applies the selected side choice for the current card.</summary>
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
            cardView.ShowEnding(endingCard, endingCard != null ? endingCard.speaker : null);

            if (!isCollapseEnding)
            {
                return; // Authored ending — its own description stands as written. No LLM overwrite.
            }

            if (llmReactionClient == null || HistoryTracker == null || narrativeDatabase.resourceCatalog == null)
            {
                return;
            }

            LlmPromptTemplates templates = Templates;
            ResourceData collapsedResource = FindCollapsedResource();
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

            string gameStateSnapshot = HistoryTracker != null
                ? HistoryTracker.GetSnapshot()
                : "Kingdom Status: Unknown";

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
            if (card.isLlmReactionCard)
            {
                LlmPromptTemplates templates = Templates;
                cardView.ShowLlmReaction(card, speaker);
                inputEnabled = false;

                string gameStateSnapshot = HistoryTracker != null
                    ? HistoryTracker.GetSnapshot()
                    : "Kingdom Status: Unknown";

                string defaultReactionSeed = templates != null
                    ? templates.defaultReactionSeedPrompt
                    : string.Empty;

                string seed = string.IsNullOrWhiteSpace(card.llmPromptSeed)
                    ? defaultReactionSeed
                    : card.llmPromptSeed;

                if (llmReactionClient == null)
                {
                    Debug.LogWarning("[GameManager] llmReactionClient is missing; auto-advancing LLM reaction card.", this);
                    AutoAdvanceReactionCard(card);
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
                        AutoAdvanceReactionCard(card);
                    });

                return;
            }

            cardView.Show(card, speaker);
            inputEnabled = !card.IsEnding;
        }

        private void AutoAdvanceReactionCard(CardData card)
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