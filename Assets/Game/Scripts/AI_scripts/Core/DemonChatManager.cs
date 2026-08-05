using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemonChatManager : MonoBehaviour
{
    [SerializeField] private GroqService groqService;
    [SerializeField] private DemonConfig config;
    [SerializeField] private TextAsset systemPromptAsset;

    private DemonChatSession currentSession;
    private ResourceState resourceState;
    private Action onSessionComplete;
    private string systemPromptTemplate;

    public event Action<DemonConfig> OnSessionStarted;
    public event Action OnSessionEnded;
    public event Action<string, Speaker, string> OnMessageAdded;
    public event Action<float> OnBeliefScoreChanged;
    public event Action<bool> OnApiProcessingChanged;
    public event Action<string> OnExchangeHintTriggered;

    private static readonly string[] ContradictionKeywords = { "but", "lie", "lying", "wrong", "false", "doubt", "nonsense" };

    private void Awake()
    {
        InitializePrompt();
        EnsureConfigLoaded();
    }

    private void EnsureConfigLoaded()
    {
        // Auto-load fallback from Resources folder if Inspector reference is unassigned
        if (config == null)
        {
            config = Resources.Load<DemonConfig>("DemonConfig");
            if (config != null)
            {
                Debug.Log("[DemonChatManager] DemonConfig auto-loaded from Resources folder.");
            }
        }
    }

    private void InitializePrompt()
    {
        if (systemPromptAsset != null)
        {
            systemPromptTemplate = systemPromptAsset.text;
        }
        else
        {
            TextAsset loaded = Resources.Load<TextAsset>("demon_system_prompt");
            systemPromptTemplate = loaded != null 
                ? loaded.text 
                : "You are {demonName}. Mode: {deceptionModeInstructions}. Tone: {initialTone}. Choice: {playerChoice}.";
        }
    }

    public void StartSession(CardData card, bool choseRight, ResourceState state, Action onComplete)
    {
        // Safeguard: If a session is already running, avoid overwriting and unblock the game
        if (currentSession != null)
        {
            Debug.LogWarning("[DemonChatManager] Session requested while another is already active. Bypassing.");
            onComplete?.Invoke();
            return;
        }

        EnsureConfigLoaded();

        if (config == null)
        {
            Debug.LogError("[DemonChatManager] CRITICAL: DemonConfig is missing! Aborting chat session.");
            onComplete?.Invoke();
            return;
        }

        if (groqService == null)
        {
            Debug.LogError("[DemonChatManager] CRITICAL: GroqService reference is missing on DemonChatManager! Aborting session.");
            onComplete?.Invoke();
            return;
        }

        // Safeguard: StartCoroutine (used inside GroqService.GetDemonResponse) throws
        // immediately if GroqService's GameObject is inactive in the hierarchy. If that
        // throw isn't caught here, it kills GameManager's ChooseRoutine coroutine mid-flight
        // (onComplete never fires) and the whole game freezes waiting on WaitUntil forever.
        if (!groqService.isActiveAndEnabled)
        {
            Debug.LogError(
                $"[DemonChatManager] CRITICAL: GroqService ('{groqService.gameObject.name}') is inactive/disabled in the hierarchy. " +
                "It (and DemonChatManager) must live on a GameObject that is never SetActive(false) - " +
                "only the visual chat Panel should be hidden via CanvasGroup. Aborting session instead of freezing.",
                this);
            onComplete?.Invoke();
            return;
        }

        resourceState = state;
        onSessionComplete = onComplete;

        DeceptionMode mode = DemonPersonaManager.RollDeceptionMode(config);
        currentSession = new DemonChatSession(card, choseRight, mode);
        DemonPersonaManager.PopulateConsequences(currentSession, card, choseRight);

        OnSessionStarted?.Invoke(config);
        OnApiProcessingChanged?.Invoke(true);

        string prompt = BuildSystemPrompt(card, choseRight);
        string initialChoiceText = choseRight ? card.rightChoiceText : card.leftChoiceText;

        // Guard: if GetDemonResponse throws for ANY reason (network layer weirdness,
        // GroqService being torn down mid-call, etc.) never let it silently kill the
        // caller's coroutine. Always resolve the session so the game can't freeze.
        try
        {
            groqService.GetDemonResponse(prompt, null, $"I chose: \"{initialChoiceText}\". Speak demon!", mode, config, (reply, fallback) =>
            {
                if (currentSession == null)
                {
                    onSessionComplete?.Invoke();
                    return;
                }

                OnApiProcessingChanged?.Invoke(false);
                currentSession.conversationHistory.Add(new ConversationTurn(Speaker.DEMON, reply));
                currentSession.exchangeCount++;
                OnMessageAdded?.Invoke(reply, Speaker.DEMON, config.demonName);

                // Auto-dismiss if already at max exchanges after first message
                if (currentSession.exchangeCount >= config.maxExchanges)
                {
                    EndSession();
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[DemonChatManager] Failed to start Groq request: {e.Message}. Ending session to avoid freezing the game.", this);
            EndSession();
        }
    }

    public void SubmitPlayerInput(string text)
    {
        if (currentSession == null || string.IsNullOrWhiteSpace(text)) return;

        if (text.Length > config.characterPerTurnLimit)
            text = text.Substring(0, config.characterPerTurnLimit);

        var playerTurn = new ConversationTurn(Speaker.PLAYER, text);
        currentSession.conversationHistory.Add(playerTurn);
        OnMessageAdded?.Invoke(text, Speaker.PLAYER, "You");

        float delta = CalculateTrustDelta(text);

        if (currentSession.exchangeCount >= config.maxExchanges)
        {
            currentSession.beliefScore = Mathf.Clamp(currentSession.beliefScore + delta, -100f, 100f);
            OnBeliefScoreChanged?.Invoke(currentSession.beliefScore);
            EndSession();
            return;
        }

        OnApiProcessingChanged?.Invoke(true);
        StartCoroutine(ProcessDemonTurn(delta, playerTurn));
    }

    private IEnumerator ProcessDemonTurn(float delta, ConversationTurn playerTurn)
    {
        if (config.demonTypingDelay > 0f)
            yield return new WaitForSeconds(config.demonTypingDelay);

        if (currentSession == null) yield break;

        string prompt = BuildSystemPrompt(currentSession.currentCard, currentSession.choseRight);

        // Guard: same reasoning as StartSession - never let an exception here strand the
        // player mid-conversation with the typing indicator stuck on forever.
        try
        {
            groqService.GetDemonResponse(prompt, currentSession.conversationHistory, null, currentSession.demonMode, config, (reply, fallback) =>
            {
                if (currentSession == null) return;

                OnApiProcessingChanged?.Invoke(false);
                playerTurn.trustDelta = delta;
                currentSession.beliefScore = Mathf.Clamp(currentSession.beliefScore + delta, -100f, 100f);

                currentSession.conversationHistory.Add(new ConversationTurn(Speaker.DEMON, reply) { trustDelta = delta });
                currentSession.exchangeCount++;

                OnMessageAdded?.Invoke(reply, Speaker.DEMON, config.demonName);
                OnBeliefScoreChanged?.Invoke(currentSession.beliefScore);

                if (currentSession.exchangeCount >= config.maxExchanges)
                {
                    EndSession();
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"[DemonChatManager] Failed to continue Groq request: {e.Message}. Ending session to avoid freezing the game.", this);
            OnApiProcessingChanged?.Invoke(false);
            EndSession();
        }
    }

    public void RequestDismissal()
    {
        if (currentSession == null) return;

        if (currentSession.exchangeCount < config.minExchangesBeforeDismiss)
        {
            OnExchangeHintTriggered?.Invoke($"Hear the demon out... ({currentSession.exchangeCount}/{config.minExchangesBeforeDismiss} turns)");
            return;
        }

        EndSession();
    }

    private void EndSession()
    {
        if (currentSession == null) return;

        try
        {
            ResolveConsequences();
        }
        catch (Exception e)
        {
            // Even a failure while applying resource consequences must not prevent the
            // session from closing out and handing control back to GameManager.
            Debug.LogError($"[DemonChatManager] ResolveConsequences threw: {e.Message}. Ending session anyway.", this);
        }

        currentSession = null;

        OnSessionEnded?.Invoke();
        onSessionComplete?.Invoke();
    }

    private void ResolveConsequences()
    {
        if (currentSession == null || resourceState == null) return;

        bool trusted = currentSession.beliefScore > 0f;
        var values = new List<ResourceValue>();
        var targetList = trusted ? currentSession.demonClaimedConsequences : currentSession.trueConsequences;

        foreach (var c in targetList)
        {
            if (c != null)
            {
                values.Add(new ResourceValue { id = c.resourceType, value = trusted ? c.claimedValue : c.trueValue });
            }
        }

        resourceState.Apply(new ResourceChange { values = values.ToArray() });
    }

    private float CalculateTrustDelta(string text)
    {
        if (string.IsNullOrEmpty(text) || config == null) return 0f;

        string lower = text.ToLowerInvariant();
        foreach (string word in ContradictionKeywords)
        {
            if (lower.Contains(word)) return -config.beliefBonusForContradiction;
        }
        return 0f;
    }

    private string BuildSystemPrompt(CardData card, bool choseRight)
    {
        if (currentSession == null || config == null) return string.Empty;

        string instructions = currentSession.demonMode switch
        {
            DeceptionMode.TRUTH => "In TRUTH mode. Reveal actual outcomes seductively.",
            DeceptionMode.LIE => "In LIE mode. Reverse facts and promise unearned gains.",
            DeceptionMode.WARNING => "In WARNING mode. Frighten player with fake catastrophes.",
            _ => "In CONFUSE mode. Speak strictly in riddles."
        };

        string tone = card != null ? card.demonInitialTone : "mocking";
        string desc = card != null ? card.description : "";
        string choiceText = card != null ? (choseRight ? card.rightChoiceText : card.leftChoiceText) : "";

        return systemPromptTemplate
            .Replace("{demonName}", config.demonName ?? "Demon")
            .Replace("{deceptionModeInstructions}", instructions)
            .Replace("{initialTone}", tone ?? "mocking")
            .Replace("{cardDescription}", desc ?? "")
            .Replace("{playerChoice}", choiceText ?? "");
    }
}