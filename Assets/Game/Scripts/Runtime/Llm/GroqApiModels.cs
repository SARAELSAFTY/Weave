using System;
using System.Collections.Generic;

namespace Game.Scripts.Runtime.Llm
{
    [Serializable]
    public class GroqResponseFormat
    {
        public string type = "json_object";
    }

    /// <summary>
    /// Plain (non-JSON-mode) Groq chat request. Intentionally omits <c>response_format</c>:
    /// JsonUtility cannot omit a null reference field, so a null <see cref="GroqResponseFormat"/>
    /// would still serialize as <c>{"type":"json_object"}</c> and fail Groq's
    /// "messages must contain 'json'" check.
    /// </summary>
    [Serializable]
    public class GroqApiRequest
    {
        public string model;
        public List<GroqApiMessage> messages = new List<GroqApiMessage>();
        public int max_completion_tokens;
        public float temperature = 0f;
        public string reasoning_effort;
    }

    /// <summary>JSON-mode Groq request used by petition turns.</summary>
    [Serializable]
    public class GroqPetitionApiRequest
    {
        public string model;
        public List<GroqApiMessage> messages = new List<GroqApiMessage>();
        public int max_completion_tokens;
        public float temperature = 0f;
        public GroqResponseFormat response_format = new GroqResponseFormat();
        public string reasoning_effort;
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
}
