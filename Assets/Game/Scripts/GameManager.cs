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
