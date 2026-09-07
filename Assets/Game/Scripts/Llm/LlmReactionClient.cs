using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Localization;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.Llm
{
    /// <summary>Outcome of the shared-service probe, as displayed on the AI settings panel.</summary>
    public enum SharedServiceState
    {
        /// <summary>No probe has finished this session.</summary>
        Unknown,
        /// <summary>A probe is in flight.</summary>
        Checking,
        /// <summary>The proxy completed a request end to end (HTTP 200).</summary>
        Online,
        /// <summary>The proxy is unreachable, misconfigured, or its shared Groq key no longer works.</summary>
        Unavailable
    }

    /// <summary>MonoBehaviour that sends chat-completions requests either directly to Groq (player-supplied key) or through a Groq-compatible proxy, and parses the responses.</summary>
    /// <remarks>Handles two request types: single-turn reactions (plain text) and multi-turn petition turns (structured JSON).
    /// All HTTP work runs as coroutines via <see cref="UnityWebRequest"/>.</remarks>
    public class LlmReactionClient : MonoBehaviour
    {
        [Tooltip("Reference to the LLM settings asset providing model name, token limits, temperature, and timeout values.")]
        [SerializeField]
        private LlmSettings settings;

        [Tooltip("URL of the Cloudflare Worker (or similar) proxy that forwards requests to the Groq API with authentication.")]
        [SerializeField]
        private string proxyUrl = "https://your-proxy.workers.dev";

        [Tooltip("Groq chat-completions endpoint used when the player supplies their own API key (BYOK).")]
        [SerializeField]
        private string directApiUrl = "https://api.groq.com/openai/v1/chat/completions";

        [Tooltip("Groq models endpoint used to validate a candidate player key without spending tokens.")]
        [SerializeField]
        private string modelsUrl = "https://api.groq.com/openai/v1/models";

        [Tooltip("Timeout for the shared-service probe. Shorter than the gameplay timeout so the status line resolves quickly when the service is unreachable.")]
        [SerializeField]
        private float probeTimeoutSeconds = 10f;

        [Tooltip("How long a shared-service probe result stays fresh; further probes inside this window are skipped.")]
        [SerializeField]
        private float probeRefreshSeconds = 60f;

        /// <summary>User message used when a caller has no message of its own and no template provides one;
        /// keeps single-turn requests to a valid system+user shape even on a misconfigured project.</summary>
        /// <remarks>Must stay in sync with the default text of <see cref="LlmPromptTemplates.singleTurnUserMessage"/>:
        /// single-sourcing the runtime const from the ScriptableObject would couple them, so both sides carry the note.</remarks>
        internal const string DefaultSingleTurnUserMessage = "Respond to the situation above.";

        /// <summary>Sends a single-turn reaction request and returns the cleaned response text via callback.</summary>
        /// <param name="systemPrompt">System message defining character voice and format constraints.</param>
        /// <param name="singleTurnUserMessage">User message describing the situation to react to; may be null or empty.</param>
        /// <param name="language">Target language for post-processing sanitization.</param>
        /// <param name="onSuccess">Called with the sanitized response text on success.</param>
        /// <param name="onFailure">Called with an error code when validation, network, or parsing fails.</param>
        /// <param name="maxTokensOverride">Optional per-request token limit overriding <see cref="LlmSettings.maxTokensPerResponse"/>.</param>
        public void RequestReaction(string systemPrompt, string singleTurnUserMessage, GameLanguage language, Action<string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            if (!TryValidateConfig(onFailure))
            {
                return;
            }

            StartCoroutine(RequestRoutine(systemPrompt, singleTurnUserMessage, language, onSuccess, onFailure, maxTokensOverride));
        }

        /// <summary>Sends a multi-turn petition request with full conversation history and returns the parsed resolution.</summary>
        /// <param name="messages">Complete message list including system prompt, prior turns, and current user input.</param>
        /// <param name="language">Target language for post-processing sanitization of the reaction field.</param>
        /// <param name="onSuccess">Called with the parsed <see cref="PetitionResolution"/> and raw assistant content on success.</param>
        /// <param name="onFailure">Called with an error code when validation, network, or parsing fails.</param>
        /// <param name="maxTokensOverride">Optional per-request token limit overriding <see cref="LlmSettings.maxTokensPerResponse"/>.</param>
        public void RequestPetitionTurn(List<GroqApiMessage> messages, GameLanguage language, Action<PetitionResolution, string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            if (!TryValidateConfig(onFailure))
            {
                return;
            }

            StartCoroutine(RequestPetitionRoutine(messages, language, onSuccess, onFailure, maxTokensOverride));
        }

        /// <summary>Sends a multi-turn chat request with full conversation history and returns the cleaned plain-text reply.</summary>
        /// <param name="messages">Complete message list including system prompt, prior turns, and current user input.</param>
        /// <param name="language">Target language for post-processing sanitization.</param>
        /// <param name="onSuccess">Called with the sanitized reply text on success.</param>
        /// <param name="onFailure">Called with an error code when validation, network, or parsing fails.</param>
        /// <param name="maxTokensOverride">Optional per-request token limit overriding <see cref="LlmSettings.maxTokensPerResponse"/>.</param>
        public void RequestChatTurn(List<GroqApiMessage> messages, GameLanguage language, Action<string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            if (!TryValidateConfig(onFailure))
            {
                return;
            }

            StartCoroutine(RequestChatRoutine(messages, language, onSuccess, onFailure, maxTokensOverride));
        }

        private bool TryValidateConfig(Action<LlmRequestError> onFailure)
        {
            if (settings == null)
            {
                Debug.LogError("[LlmReactionClient] LlmSettings reference is missing!");
                onFailure?.Invoke(LlmRequestError.NotConfigured);
                return false;
            }

            if (string.IsNullOrWhiteSpace(proxyUrl))
            {
                Debug.LogError("[LlmReactionClient] Proxy URL is missing. Assign a valid Proxy URL in the Inspector.");
                onFailure?.Invoke(LlmRequestError.NotConfigured);
                return false;
            }

            return true;
        }

        private IEnumerator RequestPetitionRoutine(List<GroqApiMessage> messages, GameLanguage language, Action<PetitionResolution, string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            yield return SendPetitionTurn(messages, language, onSuccess, onFailure, maxTokensOverride, strictSchema: true);
        }

        private IEnumerator RequestChatRoutine(List<GroqApiMessage> messages, GameLanguage language, Action<string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            string jsonPayload;
            try
            {
                // No response_format: the chat contract is plain spoken text, enforced by prompt only.
                jsonPayload = BuildPetitionJsonPayload(messages, maxTokensOverride, strictSchema: false);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LlmReactionClient] Unexpected error while building chat request payload: {exception}");
                onFailure?.Invoke(LlmRequestError.NetworkError);
                yield break;
            }

            yield return SendChatRequest(
                jsonPayload,
                responseText =>
                {
                    string reply = ParseResponse(responseText, language);
                    if (!string.IsNullOrEmpty(reply))
                    {
                        onSuccess?.Invoke(reply);
                    }
                    else
                    {
                        Debug.LogWarning($"[LlmReactionClient] Chat request succeeded but no reply was parsed. Response body: {responseText}");
                        onFailure?.Invoke(LlmRequestError.EmptyResponse);
                    }
                },
                onFailure);
        }

        // Sends one petition turn. Strict json_schema is tried first; if the request shape is ever
        // rejected (HTTP 400, e.g. after a Groq API change), it retries once without response_format so
        // petitions degrade to the prompt-only JSON contract instead of failing permanently.
        private IEnumerator SendPetitionTurn(List<GroqApiMessage> messages, GameLanguage language, Action<PetitionResolution, string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride, bool strictSchema)
        {
            string jsonPayload;
            try
            {
                jsonPayload = BuildPetitionJsonPayload(messages, maxTokensOverride, strictSchema);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LlmReactionClient] Unexpected error while building petition request payload: {exception}");
                onFailure?.Invoke(LlmRequestError.NetworkError);
                yield break;
            }

            string responseText = null;
            LlmRequestError? failure = null;
            bool badRequest = false;

            yield return SendChatRequest(
                jsonPayload,
                body => responseText = body,
                error => failure = error,
                () => badRequest = true);

            if (badRequest && strictSchema)
            {
                Debug.LogWarning("[LlmReactionClient] json_schema petition request was rejected (400). Retrying once without response_format (prompt-only JSON contract).");
                yield return SendPetitionTurn(messages, language, onSuccess, onFailure, maxTokensOverride, strictSchema: false);
                yield break;
            }

            if (failure.HasValue)
            {
                onFailure?.Invoke(failure.Value);
                yield break;
            }

            if (badRequest)
            {
                // Both the strict-schema and prompt-only shapes were rejected; report a transport-level
                // failure rather than attempting to parse an empty response body.
                Debug.LogWarning("[LlmReactionClient] Petition request was rejected (400) even without response_format.");
                onFailure?.Invoke(LlmRequestError.NetworkError);
                yield break;
            }

            PetitionResolution result = ParsePetitionResponse(responseText, language, out string rawContent);
            if (result != null)
            {
                onSuccess?.Invoke(result, rawContent);
            }
            else
            {
                Debug.LogWarning($"[LlmReactionClient] Petition request succeeded but no resolution was parsed. Response body: {responseText}");
                onFailure?.Invoke(LlmRequestError.EmptyResponse);
            }
        }

        private IEnumerator RequestRoutine(string systemPrompt, string singleTurnUserMessage, GameLanguage language, Action<string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            string jsonPayload;
            try
            {
                jsonPayload = BuildJsonPayload(systemPrompt, singleTurnUserMessage, maxTokensOverride);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LlmReactionClient] Unexpected error while building request payload: {exception}");
                onFailure?.Invoke(LlmRequestError.NetworkError);
                yield break;
            }

            yield return SendChatRequest(
                jsonPayload,
                responseText =>
                {
                    string result = ParseResponse(responseText, language);
                    if (!string.IsNullOrEmpty(result))
                    {
                        onSuccess?.Invoke(result);
                    }
                    else
                    {
                        Debug.LogWarning($"[LlmReactionClient] Request succeeded but no reaction text was parsed. Response body: {responseText}");
                        onFailure?.Invoke(LlmRequestError.EmptyResponse);
                    }
                },
                onFailure);
        }

        // Sends one chat-completions request. Uses the player's own Groq key directly when one is
        // stored and active; otherwise goes through the shared proxy. URL and Authorization header
        // are chosen as a pair so a player key can never be attached to a proxy request.
        private IEnumerator SendChatRequest(string jsonPayload, Action<string> onSuccess, Action<LlmRequestError> onFailure, Action onBadRequest = null)
        {
            bool useDirect = LlmKeyStore.HasActiveKey;
            string url = useDirect ? directApiUrl : proxyUrl;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                if (useDirect)
                {
                    request.SetRequestHeader("Authorization", "Bearer " + LlmKeyStore.GetKey());
                }
                request.timeout = Mathf.CeilToInt(settings.apiTimeoutSeconds);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                }
                else if (request.responseCode == 401 && useDirect)
                {
                    // The stored key was rejected (e.g. revoked mid-session). Disable it for this
                    // session and transparently re-send the same payload through the shared proxy
                    // so gameplay never blocks.
                    LlmKeyStore.SessionDisabled = true;
                    Debug.LogWarning("[LlmReactionClient] Player API key was rejected (401); using the shared service for this session.");
                    yield return SendChatRequest(jsonPayload, onSuccess, onFailure, onBadRequest);
                }
                else if (request.responseCode == 429)
                {
                    string retryAfter = request.GetResponseHeader("Retry-After");
                    Debug.LogWarning($"[LlmReactionClient] Rate limited (429). Retry-After: {retryAfter ?? "not provided"}.");
                    onFailure?.Invoke(LlmRequestError.RateLimited);
                }
                else if (request.responseCode == 400 && onBadRequest != null)
                {
                    // Only petition callers pass onBadRequest; reaction/epilogue callers keep the
                    // generic failure path so a 400 never silently swallows their callback.
                    Debug.LogWarning($"[LlmReactionClient] Request rejected (HTTP 400), body: {request.downloadHandler?.text}.");
                    onBadRequest?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[LlmReactionClient] Request failed: {request.result}, HTTP {request.responseCode}, error: {request.error}, body: {request.downloadHandler?.text}.");
                    onFailure?.Invoke(LlmRequestError.NetworkError);
                }
            }
        }

        /// <summary>Probes the Groq models endpoint with a candidate key; costs no tokens.</summary>
        /// <param name="candidateKey">The key to validate; sanitized before sending.</param>
        /// <param name="onResult">Called with the probe outcome: Valid, Invalid (HTTP 401), or Unreachable.</param>
        public void ValidateApiKey(string candidateKey, Action<ApiKeyValidationResult> onResult)
        {
            if (settings == null)
            {
                Debug.LogError("[LlmReactionClient] LlmSettings reference is missing!");
                onResult?.Invoke(ApiKeyValidationResult.Unreachable);
                return;
            }

            StartCoroutine(ValidateApiKeyRoutine(candidateKey, onResult));
        }

        private IEnumerator ValidateApiKeyRoutine(string candidateKey, Action<ApiKeyValidationResult> onResult)
        {
            string key = LlmKeyStore.SanitizeKey(candidateKey);
            if (key.Length == 0)
            {
                onResult?.Invoke(ApiKeyValidationResult.Invalid);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(modelsUrl))
            {
                request.SetRequestHeader("Authorization", "Bearer " + key);
                request.timeout = Mathf.CeilToInt(settings.apiTimeoutSeconds);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onResult?.Invoke(ApiKeyValidationResult.Valid);
                }
                else if (request.responseCode == 401)
                {
                    onResult?.Invoke(ApiKeyValidationResult.Invalid);
                }
                else
                {
                    Debug.LogWarning($"[LlmReactionClient] Key validation request failed: {request.result}, HTTP {request.responseCode}, error: {request.error}.");
                    onResult?.Invoke(ApiKeyValidationResult.Unreachable);
                }
            }
        }

        /// <summary>Last known state of the shared proxy; <see cref="SharedServiceState.Unknown"/> until a probe finishes.</summary>
        public SharedServiceState SharedService { get; private set; } = SharedServiceState.Unknown;

        /// <summary>Raised whenever <see cref="SharedService"/> changes so UI indicators can update live.</summary>
        public event Action<SharedServiceState> SharedServiceChanged;

        private const string ProbeSystemPrompt = "Reply with OK.";
        private const string ProbeUserMessage = "ping";

        // Room for a short reply rather than the bare minimum, so a model that needs a few tokens of
        // headroom still answers 200 and the probe cannot report a false outage.
        private const int ProbeMaxTokens = 32;

        private bool probeInFlight;
        private float lastProbeTime = float.NegativeInfinity;

        /// <summary>Sends a throwaway completion through the shared proxy to confirm the whole line still works.</summary>
        /// <remarks>Always targets <see cref="proxyUrl"/>, even when a player key is active, because the shared
        /// line is what is being measured. Skipped while a probe is in flight or while the previous result is
        /// younger than <see cref="probeRefreshSeconds"/>.</remarks>
        public void ProbeSharedService()
        {
            if (probeInFlight || Time.unscaledTime - lastProbeTime < probeRefreshSeconds)
            {
                return;
            }

            lastProbeTime = Time.unscaledTime;

            if (!TryValidateConfig(null))
            {
                PublishSharedService(SharedServiceState.Unavailable);
                return;
            }

            probeInFlight = true;
            StartCoroutine(ProbeSharedServiceRoutine());
        }

        private IEnumerator ProbeSharedServiceRoutine()
        {
            PublishSharedService(SharedServiceState.Checking);

            float started = Time.unscaledTime;
            string payload = BuildJsonPayload(ProbeSystemPrompt, ProbeUserMessage, ProbeMaxTokens);
            SharedServiceState state;

            using (UnityWebRequest request = new UnityWebRequest(proxyUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = Mathf.CeilToInt(probeTimeoutSeconds);

                yield return request.SendWebRequest();

                state = InterpretProbeResponse(request, Time.unscaledTime - started);
            }

            probeInFlight = false;
            PublishSharedService(state);
        }

        // The worker accepts only POST and returns Groq's status verbatim, so one response code tells us
        // about the whole chain: the worker itself, its shared secret, Groq, and the configured model slug.
        // Players get a single generic line; the specifics land here in the console.
        private static SharedServiceState InterpretProbeResponse(UnityWebRequest request, float elapsedSeconds)
        {
            long code = request.responseCode;
            if (code == 200)
            {
                Debug.Log($"[LlmReactionClient] Shared service probe: HTTP 200 in {elapsedSeconds:F2}s. The shared line is working.");
                return SharedServiceState.Online;
            }

            string detail = code switch
            {
                401 or 403 => "the shared GROQ_API_KEY was rejected; rotate it with `wrangler secret put GROQ_API_KEY`",
                429 => $"shared quota or rate limit exhausted (Retry-After: {request.GetResponseHeader("Retry-After") ?? "not provided"})",
                500 => "the worker has no GROQ_API_KEY secret; a deploy resets bindings, so re-put the secret",
                502 => "the worker could not reach Groq",
                0 => $"no response ({request.result}, {request.error}); the worker is unreachable or the player is offline",
                _ => $"request rejected, so the model slug or parameters may be invalid; body: {request.downloadHandler?.text}"
            };

            Debug.LogWarning($"[LlmReactionClient] Shared service probe failed after {elapsedSeconds:F2}s with HTTP {code}: {detail}.");
            return SharedServiceState.Unavailable;
        }

        private void PublishSharedService(SharedServiceState state)
        {
            SharedService = state;
            SharedServiceChanged?.Invoke(state);
        }

        private string BuildJsonPayload(string systemPrompt, string singleTurnUserMessage, int? maxTokensOverride = null)
        {
            int tokens = maxTokensOverride.HasValue ? maxTokensOverride.Value : settings.maxTokensPerResponse;
            GroqApiRequest request = new GroqApiRequest
            {
                model = settings.groqModel,
                max_completion_tokens = tokens,
                temperature = settings.temperature,
                reasoning_effort = ResolveReasoningEffort()
            };

            request.messages.Add(new GroqApiMessage { role = "system", content = systemPrompt });
            request.messages.Add(new GroqApiMessage
            {
                role = "user",
                content = string.IsNullOrWhiteSpace(singleTurnUserMessage) ? DefaultSingleTurnUserMessage : singleTurnUserMessage
            });

            return JsonUtility.ToJson(request);
        }

        private string BuildPetitionJsonPayload(List<GroqApiMessage> messages, int? maxTokensOverride = null, bool strictSchema = true)
        {
            int tokens = maxTokensOverride.HasValue ? maxTokensOverride.Value : settings.maxTokensPerResponse;

            if (!strictSchema)
            {
                // Fallback shape: no response_format at all; the prompt's JSON contract does the work.
                GroqApiRequest plain = new GroqApiRequest
                {
                    model = settings.groqModel,
                    max_completion_tokens = tokens,
                    temperature = settings.temperature,
                    reasoning_effort = ResolveReasoningEffort(),
                    messages = messages
                };
                return JsonUtility.ToJson(plain);
            }

            GroqPetitionApiRequest request = new GroqPetitionApiRequest
            {
                model = settings.groqModel,
                max_completion_tokens = tokens,
                temperature = settings.temperature,
                messages = messages,
                reasoning_effort = ResolveReasoningEffort()
            };

            return JsonUtility.ToJson(request);
        }

        // Empty settings fall back to "none" (supported by the default Qwen model); models that only
        // accept low/medium/high, such as GPT-OSS, need the value changed on LlmSettings instead.
        private string ResolveReasoningEffort() =>
            string.IsNullOrWhiteSpace(settings.reasoningEffort) ? "none" : settings.reasoningEffort;

        // Strips <think>...</think> blocks emitted by reasoning models that leak chain-of-thought into visible output.
        private static string StripThoughtBlocks(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return System.Text.RegularExpressions.Regex.Replace(text, @"<think>[\s\S]*?</think>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        }

        // Recovers the reaction field when the model ignores response_format and returns raw petition JSON as plain text.
        private static string StripLeakedPetitionJson(string content)
        {
            string trimmed = content.Trim();
            if (trimmed.StartsWith("{") && trimmed.Contains("\"reaction\""))
            {
                try
                {
                    PetitionResolution leaked = JsonUtility.FromJson<PetitionResolution>(trimmed);
                    if (leaked != null && !string.IsNullOrWhiteSpace(leaked.reaction))
                    {
                        return leaked.reaction;
                    }
                }
                catch
                {
                }
            }

            return content;
        }

        // Removes parenthetical (...) and asterisk-wrapped *...* stage directions the model may produce despite system instructions.
        private static string StripStageDirections(string content)
        {
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\([^)]*\)", "").Trim();
            return System.Text.RegularExpressions.Regex.Replace(content, @"\*[^*]*\*", "").Trim();
        }

        private static string NormalizeWhitespaceAndQuotes(string content)
        {
            content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");
            return content.Trim('"', '\'', ' ');
        }

        private static string ParseResponse(string json, GameLanguage language)
        {
            try
            {
                GroqResponse response = JsonUtility.FromJson<GroqResponse>(json);
                if (response?.choices != null && response.choices.Length > 0)
                {
                    string content = response.choices[0]?.message?.content;
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return null;
                    }

                    content = StripThoughtBlocks(content);

                    content = StripLeakedPetitionJson(content);
                    content = StripStageDirections(content);
                    content = NormalizeWhitespaceAndQuotes(content);

                    if (language == GameLanguage.Arabic)
                    {
                        content = LlmTextSanitizer.StripNonArabic(content);
                    }

                    return content;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LlmReactionClient] Parsing error: {exception.Message}");
            }

            return null;
        }

        private static PetitionResolution ParsePetitionResponse(string json, GameLanguage language, out string rawContent)
        {
            rawContent = null;
            try
            {
                GroqResponse response = JsonUtility.FromJson<GroqResponse>(json);
                if (response?.choices != null && response.choices.Length > 0)
                {
                    string content = response.choices[0]?.message?.content;
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        return null;
                    }

                    content = content.Trim();

                    content = StripThoughtBlocks(content);

                    // Models sometimes wrap JSON in ```json fences despite response_format=json_object; strip them before parsing.
                    if (content.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                    {
                        content = content.Substring(7);
                    }
                    else if (content.StartsWith("```"))
                    {
                        content = content.Substring(3);
                    }

                    if (content.EndsWith("```"))
                    {
                        content = content.Substring(0, content.Length - 3);
                    }

                    content = content.Trim();

                    PetitionResolution resolution = JsonUtility.FromJson<PetitionResolution>(content);
                    if (resolution != null && !string.IsNullOrWhiteSpace(resolution.reaction))
                    {
                        // Same display cleanup as single-turn reactions: the reaction field is spoken dialogue,
                        // so strip stage directions and normalize whitespace regardless of the language.
                        resolution.reaction = NormalizeWhitespaceAndQuotes(StripStageDirections(resolution.reaction));

                        if (language == GameLanguage.Arabic)
                        {
                            resolution.reaction = LlmTextSanitizer.StripNonArabic(resolution.reaction);
                        }

                        if (string.IsNullOrWhiteSpace(resolution.reaction))
                        {
                            return null;
                        }

                        rawContent = content;
                        return resolution;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LlmReactionClient] Petition resolution parsing error: {exception.Message}");
            }

            return null;
        }
    }
}
