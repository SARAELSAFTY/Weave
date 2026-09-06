using System;
using System.Collections.Generic;

namespace Game.Scripts.Llm
{
    /// <summary>Response format for petition turns using Groq strict structured outputs (json_schema),
    /// which constrains decoding so the model can only emit a valid <see cref="PetitionResolution"/> object.</summary>
    /// <remarks>Reaction/epilogue requests deliberately send no response_format at all; only this class
    /// is serialized into petition requests.</remarks>
    [Serializable]
    public class GroqPetitionResponseFormat
    {
        /// <summary>Must be "json_schema" for strict structured outputs.</summary>
        public string type = "json_schema";
        /// <summary>The named, strict schema sent to Groq.</summary>
        public GroqJsonSchema json_schema = new GroqJsonSchema();
    }

    /// <summary>Named strict schema wrapper required by the Groq structured-outputs API.</summary>
    [Serializable]
    public class GroqJsonSchema
    {
        /// <summary>Schema name; lowercase letters, digits, and underscores only.</summary>
        public string name = "petition_resolution";
        /// <summary>True enables constrained decoding (guaranteed schema compliance).</summary>
        public bool strict = true;
        /// <summary>The actual JSON schema for a petition resolution.</summary>
        public GroqPetitionSchema schema = new GroqPetitionSchema();
    }

    // The petition schema shape is fixed, so each JSON property is a named C# field; JsonUtility
    // serializes field names as the property names Groq expects. These classes must stay in sync
    // with PetitionResolution and the contract text in LlmPromptTemplates.petitionSystemInstructions.
    /// <summary>JSON schema for a <see cref="PetitionResolution"/>: all fields required, no extra fields.</summary>
    [Serializable]
    public class GroqPetitionSchema
    {
        /// <summary>Schema node type.</summary>
        public string type = "object";
        /// <summary>The four petition resolution fields; names are part of the API contract.</summary>
        public GroqPetitionSchemaProperties properties = new GroqPetitionSchemaProperties();
        /// <summary>Strict mode requires every field to be listed here.</summary>
        public string[] required = { "phase", "reaction", "resourceChanges", "historyTag" };
        /// <summary>Strict mode requires additionalProperties to be false.</summary>
        public bool additionalProperties = false;
    }

    /// <summary>Property schemas for <see cref="GroqPetitionSchema"/>.</summary>
    [Serializable]
    public class GroqPetitionSchemaProperties
    {
        /// <summary>"deliberating" or "proposal".</summary>
        public GroqSchemaString phase = new GroqSchemaString();
        /// <summary>The petitioner's in-character spoken reaction.</summary>
        public GroqSchemaString reaction = new GroqSchemaString();
        /// <summary>Resource deltas; empty array while deliberating.</summary>
        public GroqSchemaDeltaArray resourceChanges = new GroqSchemaDeltaArray();
        /// <summary>Short snake_case history tag; empty string until a proposal.</summary>
        public GroqSchemaString historyTag = new GroqSchemaString();
    }

    /// <summary>JSON schema node for a string value.</summary>
    [Serializable]
    public class GroqSchemaString
    {
        /// <summary>Schema node type.</summary>
        public string type = "string";
    }

    /// <summary>JSON schema node for an integer value.</summary>
    [Serializable]
    public class GroqSchemaInteger
    {
        /// <summary>Schema node type.</summary>
        public string type = "integer";
    }

    /// <summary>JSON schema for the resourceChanges array of {resource, delta} objects.</summary>
    [Serializable]
    public class GroqSchemaDeltaArray
    {
        /// <summary>Schema node type.</summary>
        public string type = "array";
        /// <summary>Schema for each array element.</summary>
        public GroqSchemaDeltaItem items = new GroqSchemaDeltaItem();
    }

    /// <summary>JSON schema for a single {resource, delta} element.</summary>
    [Serializable]
    public class GroqSchemaDeltaItem
    {
        /// <summary>Schema node type.</summary>
        public string type = "object";
        /// <summary>Element property schemas; names are part of the API contract.</summary>
        public GroqSchemaDeltaProperties properties = new GroqSchemaDeltaProperties();
        /// <summary>Strict mode requires every field to be listed here.</summary>
        public string[] required = { "resource", "delta" };
        /// <summary>Strict mode requires additionalProperties to be false.</summary>
        public bool additionalProperties = false;
    }

    /// <summary>Property schemas for <see cref="GroqSchemaDeltaItem"/>.</summary>
    [Serializable]
    public class GroqSchemaDeltaProperties
    {
        /// <summary>Resource asset name from the valid resources list.</summary>
        public GroqSchemaString resource = new GroqSchemaString();
        /// <summary>Signed whole-number delta.</summary>
        public GroqSchemaInteger delta = new GroqSchemaInteger();
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
        /// <summary>Forces the model to respond with valid JSON matching the petition contract via strict json_schema.</summary>
        public GroqPetitionResponseFormat response_format = new GroqPetitionResponseFormat();
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
