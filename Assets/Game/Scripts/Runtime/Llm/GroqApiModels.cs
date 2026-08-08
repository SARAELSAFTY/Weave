using System;
using System.Collections.Generic;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Represents a Groq chat completion request payload.</summary>
    [Serializable]
    public class GroqApiRequest
    {
        public string model;
        public List<GroqApiMessage> messages = new List<GroqApiMessage>();
        public int max_tokens;
        public float temperature = 0f;
    }

    /// <summary>Represents one message entry in a Groq chat request.</summary>
    [Serializable]
    public class GroqApiMessage
    {
        public string role;
        public string content;
    }

    /// <summary>Represents a Groq chat completion response.</summary>
    [Serializable]
    public class GroqResponse
    {
        public GroqChoice[] choices;
    }

    /// <summary>Represents one choice in a Groq response.</summary>
    [Serializable]
    public class GroqChoice
    {
        public GroqMessageContent message;
    }

    /// <summary>Represents message content data in a Groq response choice.</summary>
    [Serializable]
    public class GroqMessageContent
    {
        public string content;
    }
}