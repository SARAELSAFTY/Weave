using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DemonChatPanel : MonoBehaviour
{
    [SerializeField] private DemonChatManager chatManager;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image demonPortrait;
    [SerializeField] private TMP_Text demonNameText;
    [SerializeField] private ScrollRect chatScrollRect;
    [SerializeField] private RectTransform messageContainer;
    [SerializeField] private TMP_Text messagePrefab;

    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject typingIndicator;
    [SerializeField] private TMP_Text exchangeHintText;

    private readonly Queue<TMP_Text> bubblePool = new();
    private readonly List<TMP_Text> activeBubbles = new();

    private void Awake()
    {
        // Hide panel initially on game start (but keep GameObject active so OnEnable() runs)
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        // DO NOT disable gameObject - we need OnEnable() to subscribe to events!
    }

    private void OnEnable()
    {
        if (chatManager != null)
        {
            chatManager.OnSessionStarted += HandleSessionStarted;
            chatManager.OnSessionEnded += HandleSessionEnded;
            chatManager.OnMessageAdded += AddMessage;
            chatManager.OnApiProcessingChanged += SetProcessingState;
            chatManager.OnExchangeHintTriggered += ShowHint;
        }

        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitClicked);
    }

    private void OnDisable()
    {
        if (chatManager != null)
        {
            chatManager.OnSessionStarted -= HandleSessionStarted;
            chatManager.OnSessionEnded -= HandleSessionEnded;
            chatManager.OnMessageAdded -= AddMessage;
            chatManager.OnApiProcessingChanged -= SetProcessingState;
            chatManager.OnExchangeHintTriggered -= ShowHint;
        }

        if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmitClicked);
    }

    private void HandleSessionStarted(DemonConfig config)
    {
        ClearActiveBubbles();
        if (demonNameText != null) demonNameText.text = config.demonName;
        if (demonPortrait != null) demonPortrait.sprite = config.demonPortrait;

        // gameObject is already active (never disabled), so just make it interactive and fade in
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        StartCoroutine(FadeRoutine(1f));
    }

    private void HandleSessionEnded()
    {
        StartCoroutine(FadeRoutine(0f, () =>
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            // Keep gameObject active for next session - just make it invisible & non-interactive
            ClearActiveBubbles();
        }));
    }

    private void AddMessage(string text, Speaker speaker, string name)
    {
        TMP_Text bubble = GetBubble();
        bubble.text = $"<b>{name}:</b> {text}";
        bubble.color = speaker == Speaker.DEMON ? Color.red : Color.cyan;
        bubble.gameObject.SetActive(true);

        StartCoroutine(ScrollToBottomNextFrame());
    }

    private void SetProcessingState(bool isProcessing)
    {
        if (typingIndicator != null) typingIndicator.SetActive(isProcessing);
        if (inputField != null) inputField.interactable = !isProcessing;
        if (submitButton != null) submitButton.interactable = !isProcessing;
    }

    private void ShowHint(string hint)
    {
        if (exchangeHintText == null) return;
        exchangeHintText.text = hint;
        exchangeHintText.gameObject.SetActive(true);
    }

    private void OnSubmitClicked()
    {
        if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) return;
        string text = inputField.text;
        inputField.text = string.Empty;
        chatManager.SubmitPlayerInput(text);
    }

    private TMP_Text GetBubble()
    {
        TMP_Text bubble = bubblePool.Count > 0 ? bubblePool.Dequeue() : Instantiate(messagePrefab, messageContainer);
        activeBubbles.Add(bubble);
        return bubble;
    }

    private void ClearActiveBubbles()
    {
        foreach (var b in activeBubbles)
        {
            b.gameObject.SetActive(false);
            bubblePool.Enqueue(b);
        }
        activeBubbles.Clear();
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        if (chatScrollRect != null) chatScrollRect.verticalNormalizedPosition = 0f;
    }

    private IEnumerator FadeRoutine(float targetAlpha, System.Action onComplete = null)
    {
        float elapsed = 0f;
        float start = canvasGroup.alpha;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, elapsed / 0.2f);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
        onComplete?.Invoke();
    }
}