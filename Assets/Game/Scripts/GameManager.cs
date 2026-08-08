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
        [SerializeField, Tooltip("Shared chat service for LLM reaction cards.")] private LlmReactionClient llmReactionClient;

        private bool inputEnabled;
        private NarrativeRunner narrativeRunner;

        /// <summary>Gets whether card choice input is currently accepted.</summary>
        public bool AcceptsChoiceInput => inputEnabled;

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

            if (llmReactionClient == null)
            {
                Debug.LogWarning($"[GameManager] Optional Inspector reference '{nameof(llmReactionClient)}' is missing on '{gameObject.name}'. LLM reaction cards will auto-advance.", this);
            }

            if (cardView == null || resourceState == null || narrativeDatabase == null || dayDisplay == null)
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

            narrativeRunner = new NarrativeRunner(narrativeDatabase, resourceState);
            narrativeRunner.DayChanged += HandleDayChanged;
            HistoryTracker = new PlayerHistoryTracker(resourceState, narrativeRunner, narrativeDatabase.resourceCatalog);

            HistoryTracker.Reset();
            if (!narrativeRunner.StartRun(out string error))
            {
                Debug.LogError(error, this);
                enabled = false;
                return;
            }

            RefreshDayDisplay();
            ShowCurrentCard();
        }

        private void OnEnable()
        {
            if (cardView != null)
            {
                cardView.RestartRequested += RestartRun;
            }
        }

        private void OnDisable()
        {
            if (cardView != null)
            {
                cardView.RestartRequested -= RestartRun;
            }

            if (narrativeRunner != null)
            {
                narrativeRunner.DayChanged -= HandleDayChanged;
            }
        }

        /// <summary>Applies the selected side choice for the current card.</summary>
        public void ChooseSide(bool choseRight)
        {
            if (!inputEnabled)
            {
                return;
            }

            StartCoroutine(ChooseRoutine(choseRight));
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
                EndRun(result.endingCard);
                yield break;
            }

            ShowCurrentCard();
        }

        private void EndRun(CardData endingCard)
        {
            inputEnabled = false;
            cardView.ShowEnding(endingCard, narrativeRunner.GetSpeaker(endingCard.speakerId));
        }

        private void ShowCurrentCard()
        {
            CardData card = narrativeRunner.CurrentCard;
            if (card == null)
            {
                return;
            }

            CouncilMemberData speaker = narrativeRunner.GetSpeaker(card.speakerId);
            if (card.isLlmReactionCard)
            {
                cardView.ShowLlmReaction(card, speaker);
                inputEnabled = false;

                string gameStateSnapshot = HistoryTracker != null
                    ? HistoryTracker.GetSnapshot()
                    : "Kingdom Status: Unknown";

                string seed = string.IsNullOrWhiteSpace(card.llmPromptSeed)
                    ? "React briefly to the player's recent decisions in character."
                    : card.llmPromptSeed;

                if (llmReactionClient == null)
                {
                    Debug.LogWarning("[GameManager] llmReactionClient is missing; auto-advancing LLM reaction card.", this);
                    AutoAdvanceReactionCard(card);
                    return;
                }

                string fullSystemPrompt = LlmPersonaPromptBuilder.Build(speaker, gameStateSnapshot, seed);

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
                EndRun(result.endingCard);
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
