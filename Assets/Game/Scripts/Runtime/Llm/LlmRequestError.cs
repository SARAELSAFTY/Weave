namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Error codes returned by <see cref="LlmReactionClient"/> when an LLM request fails.</summary>
    public enum LlmRequestError
    {
        /// <summary>LlmSettings reference or proxy URL is not assigned on the client.</summary>
        NotConfigured,
        /// <summary>The API returned HTTP 429 (rate limited).</summary>
        RateLimited,
        /// <summary>The HTTP request failed or payload construction threw an exception.</summary>
        NetworkError,
        /// <summary>The API returned a successful response but no usable content could be parsed.</summary>
        EmptyResponse
    }
}
