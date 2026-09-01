using System;
using System.Collections.Generic;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Requests JSON-only output from the Groq chat-completions API.</summary>
    [Serializable]
    public class GroqResponseFormat
    {
        /// <summary>Response format type; defaults to "json_object" for structured petition replies.</summary>
        public string type = "json_object";
    }

    /// <summary>Request payload for single-turn (reaction/epilogue) Groq chat-completions calls.</summary>
    [Serializable]
    public class GroqApiRequest
    {
        /// <summary>Groq model identifier sent in the request body.</summary>
        public string model;
        /// <summary>Conversation messages (system + optional user turn).</summary>
        public List<GroqApiMessage> messages = new List<GroqApiMessage>();
        /// <summary>Maximum completion tokens the model may generate.</summary>
        public int max_completion_tokens;
        /// <summary>Sampling temperature; 0 for deterministic output.</summary>
        public float temperature = 0f;
        /// <summary>Reasoning effort level ("none", "low", "medium", "high") for reasoning-capable models.</summary>
        public string reasoning_effort;
    }

    /// <summary>Request payload for petition turns that require structured JSON output.</summary>
    /// <remarks>Identical to <see cref="GroqApiRequest"/> but includes <see cref="response_format"/>
    /// to force the model to return a <see cref="PetitionResolution"/> JSON object.</remarks>
    [Serializable]
    public class GroqPetitionApiRequest
    {
        /// <summary>Groq model identifier sent in the request body.</summary>
        public string model;
        /// <summary>Full conversation history including system prompt, prior turns, and current user input.</summary>
        public List<GroqApiMessage> messages = new List<GroqApiMessage>();
        /// <summary>Maximum completion tokens the model may generate.</summary>
        public int max_completion_tokens;
        /// <summary>Sampling temperature; 0 for deterministic output.</summary>
        public float temperature = 0f;
        /// <summary>Forces the model to respond with valid JSON matching the petition contract.</summary>
        public GroqResponseFormat response_format = new GroqResponseFormat();
        /// <summary>Reasoning effort level ("none", "low", "medium", "high") for reasoning-capable models.</summary>
        public string reasoning_effort;
    }

    /// <summary>A single message in a Groq chat-completions conversation.</summary>
    [Serializable]
    public class GroqApiMessage
    {
        /// <summary>Message role: "system", "user", or "assistant".</summary>
        public string role;
        /// <summary>Message text content.</summary>
        public string content;
    }

    /// <summary>Top-level response envelope from the Groq chat-completions API.</summary>
    [Serializable]
    public class GroqResponse
    {
        /// <summary>Array of completion choices returned by the model.</summary>
        public GroqChoice[] choices;
    }

    /// <summary>A single completion choice within a <see cref="GroqResponse"/>.</summary>
    [Serializable]
    public class GroqChoice
    {
        /// <summary>The generated message content for this choice.</summary>
        public GroqMessageContent message;
    }

    /// <summary>Wraps the text content of a model-generated message.</summary>
    [Serializable]
    public class GroqMessageContent
    {
        /// <summary>Raw text produced by the model.</summary>
        public string content;
    }
}
