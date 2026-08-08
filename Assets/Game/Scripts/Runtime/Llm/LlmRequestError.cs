namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Describes failure reasons for LLM reaction requests.</summary>
    public enum LlmRequestError
    {
        NotConfigured,
        RateLimited,
        NetworkError,
        EmptyResponse,
        Cancelled
    }
}
