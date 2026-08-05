using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Main controller for game flow, UI updates, and turn progression.
public class GameManager : MonoBehaviour
{
    [SerializeField, Tooltip("Card UI component in scene.")] private CardView cardView;
    [SerializeField, Tooltip("Tracks kingdom resource values.")] private ResourceState resourceState;
    [SerializeField, Tooltip("Story database containing cards.")] private NarrativeDatabase narrativeDatabase;
    [SerializeField, Min(0.05f), Tooltip("Card exit animation duration in seconds.")] private float cardExitDuration = 0.25f;
    [SerializeField, Tooltip("UI text element showing the current day.")] private DayDisplay dayDisplay;
    [SerializeField, Tooltip("Manages demon conversation sessions.")] private DemonChatManager demonChatManager;
    [SerializeField, Min(1f), Tooltip("Safety ceiling (seconds) to wait on a demon chat session before forcing the game to continue, in case it never reports completion.")]
    private float demonSessionWatchdogSeconds = 30f;

    private bool inputEnabled;
    private NarrativeRunner narrativeRunner;

    public bool AcceptsChoiceInput => inputEnabled;
    public int CurrentDay => narrativeRunner != null ? narrativeRunner.Day : 1;

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

        if (cardView == null || resourceState == null || narrativeDatabase == null || dayDisplay == null)
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

        narrativeRunner = new NarrativeRunner(narrativeDatabase, resourceState);
        narrativeRunner.DayChanged += HandleDayChanged;

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

    // Called by input system when player chooses left (false) or right (true).
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

        CardData card = narrativeRunner.CurrentCard;

        // Trigger demon chat session if current card is flagged
        if (card != null && card.isDemonCard && demonChatManager != null)
        {
            bool demonSessionFinished = false;

            try
            {
                demonChatManager.StartSession(card, choseRight, resourceState, () => demonSessionFinished = true);
            }
            catch (Exception e)
            {
                // If StartSession itself throws (e.g. GroqService's GameObject was
                // inactive), don't let it kill this coroutine - that's exactly what
                // used to freeze the game with no way to recover.
                Debug.LogError($"[GameManager] DemonChatManager.StartSession threw: {e.Message}. Continuing without demon session.", this);
                demonSessionFinished = true;
            }

            // Watchdog: even if StartSession reports success, never wait forever.
            // Something going wrong deep in the API/coroutine chain should degrade
            // to "continue the game" rather than a permanent freeze.
            float elapsed = 0f;
            while (!demonSessionFinished && elapsed < demonSessionWatchdogSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!demonSessionFinished)
            {
                Debug.LogError(
                    $"[GameManager] Demon chat session did not complete within {demonSessionWatchdogSeconds}s. " +
                    "Forcing the game to continue instead of staying frozen.", this);
            }
        }

        NarrativeStepResult result = narrativeRunner.Choose(choseRight);
        if (result.HasError)
        {
            Debug.LogError(result.Error, this);
            enabled = false;
            yield break;
        }

        yield return cardView.AnimateCardExit(choseRight, cardExitDuration);

        if (result.HasEnded)
        {
            EndRun(result.EndingCard);
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

        cardView.Show(
            card,
            narrativeRunner.GetSpeaker(card.speakerId));

        inputEnabled = !card.isEnding;
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