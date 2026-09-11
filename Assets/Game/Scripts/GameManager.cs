using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using Game.Scripts.Llm;
using Game.Scripts.Narrative;
using Game.Scripts.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Scripts
{
    /// <summary>Orchestrates the core game flow: card presentation, player choices, resource warnings, and run endings.</summary>
    /// <remarks>
    /// Coordinates CardView, NarrativeRunner, ResourceWarningMonitor, PlayerHistoryTracker, PetitionFlowController,
    /// and CollapseEndingBuilder to drive a day-based card game where each choice advances a branching narrative.
    /// Supports LLM-generated speaker reactions and petition dialogues with turn limits.
    /// </remarks>
    public class GameManager : MonoBehaviour
    {
        [Tooltip("Card view responsible for displaying cards, animations, and petition UI.")]
        [SerializeField] private CardView cardView;

        [Tooltip("Tracks the current values of all kingdom resources.")]
        [SerializeField] private ResourceState resourceState;

        [Tooltip("Database of narrative cards, speakers, resource catalog, and LLM prompt templates.")]
        [SerializeField] private NarrativeDatabase narrativeDatabase;

        [Tooltip("Duration in seconds for the card-exit animation after a choice is made.")]
        [SerializeField, Min(0.05f)] private float cardExitDuration = 0.25f;

        [Tooltip("UI element that displays the current day number.")]
        [SerializeField] private DayDisplay dayDisplay;

        [Tooltip("Start screen shown before the run begins.")]
        [SerializeField] private StartScreenView startScreenView;

        [Tooltip("Pause menu overlay shown when the player presses Escape.")]
        [SerializeField] private PauseMenuView pauseMenuView;

        [Tooltip("Optional panel for storing and validating the player's own Groq API key.")]
        [SerializeField] private ByokPanelView byokPanelView;

        [Tooltip("Optional client used to request LLM-generated speaker reactions and petition dialogue.")]
        [SerializeField] private LlmReactionClient llmReactionClient;

        [Tooltip("Optional settings controlling LLM token limits, history counts, and petition parameters.")]
        [SerializeField] private LlmSettings llmSettings;

        private bool inputEnabled;
        private bool runInProgress;
        private bool isPaused;
        private NarrativeRunner narrativeRunner;
        private ResourceWarningMonitor resourceWarningMonitor;
        private bool showingResourceWarning;
        private CardData warningCardInstance;
        private PetitionFlowController petitionFlow;
        private ChatFlowController chatFlow;
        private CollapseEndingBuilder collapseEndingBuilder;

        private LlmPromptTemplates Templates => narrativeDatabase != null ? narrativeDatabase.promptTemplates : null;

        private GameLanguage CurrentLanguage => LanguageManager.CurrentLanguageOrDefault;

        /// <summary>True when the game is accepting left/right choice input (not paused, not mid-animation).</summary>
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

            if (byokPanelView == null)
            {
                Debug.LogWarning($"[GameManager] Optional Inspector reference '{nameof(byokPanelView)}' is missing on '{gameObject.name}'. Players will not be able to set their own API key.", this);
            }

            if (missingReference)
            {
                enabled = false;
            }
        }

        // Deferred from Awake: these objects depend on validated Inspector references and should only
        // be constructed when all required fields are confirmed present.
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
            petitionFlow = new PetitionFlowController(
                cardView,
                llmReactionClient,
                llmSettings,
                narrativeDatabase,
                narrativeRunner,
                resourceState,
                historyTracker,
                () => CurrentLanguage,
                value => inputEnabled = value,
                BeginPetitionConfirmAdvance,
                delay => StartCoroutine(ReenablePetitionSubmitAfterDelay(delay)));
            chatFlow = new ChatFlowController(
                cardView,
                llmReactionClient,
                llmSettings,
                narrativeDatabase,
                narrativeRunner,
                historyTracker,
                () => CurrentLanguage,
                value => inputEnabled = value,
                BeginPetitionConfirmAdvance,
                delay => StartCoroutine(ReenablePetitionSubmitAfterDelay(delay)));
            collapseEndingBuilder = new CollapseEndingBuilder(
                cardView,
                llmReactionClient,
                historyTracker,
                narrativeDatabase,
                narrativeRunner,
                llmSettings,
                () => CurrentLanguage);

            if (!narrativeRunner.StartRun(out string error))
            {
                Debug.LogError(error, this);
                enabled = false;
                return;
            }

            // Fresh boot (first load or after a restart): show the start screen until the player presses Play.
            // The scene saves StartPanel inactive, so activation must happen here rather than rely on authoring.
            startScreenView.Show();

            // First launch: the AI-line screen comes before the start screen until the player picks a service.
            // The scene saves the panel active, so returning players need it hidden here rather than by authoring.
            if (!LlmKeyStore.ServiceChosen)
            {
                byokPanelView?.Show();
            }
            else
            {
                byokPanelView?.Hide();
            }

            // Probe the shared line only when it is actually in use: players on their own key bypass the
            // proxy entirely and should not spend its quota. On first launch the Show() above already probed.
            if (!LlmKeyStore.HasActiveKey)
            {
                llmReactionClient?.ProbeSharedService();
            }
        }

        // Runtime-created ScriptableObjects (warning cards, generated endings, temp speakers) are not
        // tracked by Unity's asset database and must be explicitly destroyed to avoid leaks.
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

            petitionFlow?.CleanupPetitionSpeaker();
            chatFlow?.CleanupChatSpeaker();
            collapseEndingBuilder?.Cleanup();
        }

        private void Update()
        {
            if (!enabled || !runInProgress)
            {
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (byokPanelView != null && byokPanelView.IsVisible)
                {
                    byokPanelView.Hide();
                    return;
                }

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
                startScreenView.ApiKeyRequested += ShowByokPanel;
            }

            if (pauseMenuView != null)
            {
                pauseMenuView.ResumeRequested += Resume;
                pauseMenuView.RestartRequested += RestartRun;
                pauseMenuView.ApiKeyRequested += ShowByokPanel;
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
                startScreenView.ApiKeyRequested -= ShowByokPanel;
            }

            if (pauseMenuView != null)
            {
                pauseMenuView.ResumeRequested -= Resume;
                pauseMenuView.RestartRequested -= RestartRun;
                pauseMenuView.ApiKeyRequested -= ShowByokPanel;
            }
        }

        private void BeginRun()
        {
            if (!enabled)
            {
                return;
            }

            startScreenView?.Hide();

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

        /// <summary>Handles a player's left/right choice, routing through resource-warning dismissal or normal card resolution.</summary>
        /// <param name="choseRight">True for right choice, false for left.</param>
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
            yield return cardView.WaitForChoiceFlight();
            ShowCurrentCard();
        }

        private IEnumerator ChooseRoutine(bool choseRight)
        {
            inputEnabled = false;
            cardView.PlayConfirmAnimation(choseRight);

            // Capture choice text before Choose() advances the narrative, since CurrentCard will change.
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
            yield return cardView.WaitForChoiceFlight();
            HandleStepResult(result);
        }

        private void EndRun(CardData endingCard, bool isCollapseEnding, ResourceData collapsedResource)
        {
            inputEnabled = false;
            runInProgress = false;

            // If no valid ending card exists, fall back to a generic run ended state
            if (endingCard == null && !isCollapseEnding)
            {
                Debug.LogError("[GameManager] Ending card is null and not a collapse ending. Show generic end.", this);
                cardView.ShowEnding(null, null);
                return;
            }

            SpeakerData speaker;
            if (isCollapseEnding)
            {
                speaker = collapsedResource != null ? collapsedResource.speaker : null;
            }
            else
            {
                speaker = endingCard != null ? endingCard.speaker : null;
            }

            if (isCollapseEnding)
            {
                collapseEndingBuilder.Show(speaker, collapsedResource, endingCard);
                return;
            }

            cardView.ShowEnding(endingCard, speaker);
        }

        private void ShowResourceWarning(ResourceData resource)
        {
            if (resource == null || resource.speaker == null)
            {
                ShowCurrentCard();
                return;
            }

            SpeakerData speaker = resource.speaker;
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
            warningCardInstance.speaker = resource.speaker;
            warningCardInstance.leftChoiceLocalized = default;
            warningCardInstance.rightChoiceLocalized = default;

            cardView.ShowLlmReaction(warningCardInstance, speaker);

            if (resourceWarningMonitor != null)
            {
                resourceWarningMonitor.OnWarningShown(resource);
            }

            string gameStateSnapshot = GetReactionSnapshotOrDefault();

            string seed = SpeakerPromptBuilder.BuildWarningSituation(templates, resource.GetDisplayName(CurrentLanguage));

            LlmFallbackText.RequestSpeakerLine(llmReactionClient, templates, GetResourceCatalog(),
                speaker, gameStateSnapshot, seed, CurrentLanguage,
                LlmFallbackText.Warning(templates, CurrentLanguage),
                line =>
                {
                    cardView.SetDescriptionText(line);
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
            if (card.isChatCard)
            {
                chatFlow?.ShowChatCard(card);
                return;
            }

            if (card.isPetitionCard)
            {
                petitionFlow.ShowPetitionCard(card);
                return;
            }

            if (card.isLlmReactionCard)
            {
                LlmPromptTemplates templates = Templates;
                cardView.ShowLlmReaction(card, speaker);
                inputEnabled = false;

                string gameStateSnapshot = GetReactionSnapshotOrDefault();
                string sceneDescription = card.GetDescription(CurrentLanguage);
                string reactionSeed = card.EffectiveReactionSeed(templates);
                string seed = SpeakerPromptBuilder.BuildReactionSituation(sceneDescription, reactionSeed);

                LlmFallbackText.RequestSpeakerLine(llmReactionClient, templates, GetResourceCatalog(),
                    speaker, gameStateSnapshot, seed, CurrentLanguage,
                    LlmFallbackText.Reaction(templates, CurrentLanguage),
                    line =>
                    {
                        cardView.SetDescriptionText(line);
                        inputEnabled = !card.IsEnding;
                    });

                return;
            }

            cardView.Show(card, speaker);
            inputEnabled = !card.IsEnding;
        }

        // Thin forwarders to the audience flow controllers; the card view can raise these events
        // before Start() has constructed the controllers. The petition input field is shared by
        // petition and chat audiences, so submissions route to whichever flow is active.
        private void HandlePetitionSubmitted(string playerInput)
        {
            if (chatFlow != null && chatFlow.HandleChatSubmitted(playerInput))
            {
                return;
            }

            petitionFlow?.HandlePetitionSubmitted(playerInput);
        }

        private void ShowByokPanel()
        {
            byokPanelView?.Show();
        }

        private void HandlePetitionConfirmed()
        {
            // The confirm button doubles as the chat end-audience button; route it to the active flow.
            if (chatFlow != null && chatFlow.HandleChatEndRequested())
            {
                return;
            }

            petitionFlow?.HandlePetitionConfirmed();
        }

        // Invoked by the petition flow after a confirmed proposal; advances the narrative like a normal choice.
        private void BeginPetitionConfirmAdvance()
        {
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

        // Applies a brief cooldown before re-enabling the submit button to prevent rapid-fire retries
        // that could compound rate-limiting or error states.
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
                llmSettings != null ? llmSettings.reactionHistoryCount : LlmSettings.DefaultReactionHistoryCount,
                llmSettings != null ? llmSettings.pastPetitionChatCount : LlmSettings.DefaultPastPetitionChatCount);
        }

        private string GetSnapshotOrDefault(int narrativeHistoryEntryCount, int petitionTranscriptCount)
        {
            return historyTracker != null
                ? historyTracker.GetSnapshot(narrativeHistoryEntryCount, petitionTranscriptCount, CurrentLanguage)
                : FallbackStrings.KingdomStatusUnknown(CurrentLanguage);
        }

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

        // Resource catalog used by prompt building; null when the database or catalog is unassigned.
        private IReadOnlyList<ResourceData> GetResourceCatalog()
        {
            return narrativeDatabase != null && narrativeDatabase.resourceCatalog != null
                ? narrativeDatabase.resourceCatalog.resources
                : null;
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
            isPaused = false;
            inputEnabled = false;
            runInProgress = false;

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
