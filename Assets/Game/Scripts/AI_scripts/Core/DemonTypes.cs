using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DeceptionMode
{
    TRUTH,
    LIE,
    WARNING,
    CONFUSE
}

public enum Speaker
{
    PLAYER,
    DEMON
}

[Serializable]
public class ConversationTurn
{
    public Speaker speaker;
    [TextArea(1, 4)] public string message;
    public float timestamp;
    public float trustDelta;

    public ConversationTurn(Speaker speaker, string message)
    {
        this.speaker = speaker;
        this.message = message;
        timestamp = Time.time;
        trustDelta = 0f;
    }
}

[Serializable]
public class GameConsequence
{
    public string resourceType;
    public int trueValue;
    public int claimedValue;

    public GameConsequence(string resourceType, int trueValue, int claimedValue)
    {
        this.resourceType = resourceType;
        this.trueValue = trueValue;
        this.claimedValue = claimedValue;
    }
}

[Serializable]
public class DemonChatSession
{
    public string sessionId;
    public CardData currentCard;
    public bool choseRight;
    public List<ConversationTurn> conversationHistory = new List<ConversationTurn>();
    public int exchangeCount;
    public float beliefScore;
    public DeceptionMode demonMode;
    public List<GameConsequence> demonClaimedConsequences = new List<GameConsequence>();
    public List<GameConsequence> trueConsequences = new List<GameConsequence>();

    public DemonChatSession(CardData card, bool choseRight, DeceptionMode mode)
    {
        sessionId = Guid.NewGuid().ToString();
        currentCard = card;
        this.choseRight = choseRight;
        demonMode = mode;
        exchangeCount = 0;
        beliefScore = 0f;
    }
}

// ─── API Serialization Models ─────────────────────────────────────────────

[Serializable]
public class GroqApiRequest
{
    public string model;
    public List<GroqApiMessage> messages = new List<GroqApiMessage>();
    public int max_tokens;
    public float temperature = 0.9f;
}

[Serializable]
public class GroqApiMessage
{
    public string role;
    public string content;
}

[Serializable]
public class GroqResponse
{
    public GroqChoice[] choices;
}

[Serializable]
public class GroqChoice
{
    public GroqMessageContent message;
}

[Serializable]
public class GroqMessageContent
{
    public string content;
}