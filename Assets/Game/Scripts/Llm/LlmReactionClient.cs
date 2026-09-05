using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Localization;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.Llm
{
    /// <summary>MonoBehaviour that sends chat-completions requests to a Groq-compatible proxy and parses the responses.</summary>
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
            string jsonPayload;
            try
            {
                jsonPayload = BuildPetitionJsonPayload(messages, maxTokensOverride);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LlmReactionClient] Unexpected error while building petition request payload: {exception}");
                onFailure?.Invoke(LlmRequestError.NetworkError);
                yield break;
            }

            yield return SendProxyRequest(
                jsonPayload,
                responseText =>
                {
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
                },
                onFailure);
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

            yield return SendProxyRequest(
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

        private IEnumerator SendProxyRequest(string jsonPayload, Action<string> onSuccess, Action<LlmRequestError> onFailure)
        {
            using (UnityWebRequest request = new UnityWebRequest(proxyUrl, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = Mathf.CeilToInt(settings.apiTimeoutSeconds);

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    onSuccess?.Invoke(request.downloadHandler.text);
                }
                else if (request.responseCode == 429)
                {
                    Debug.LogWarning("[LlmReactionClient] Rate limited (429).");
                    onFailure?.Invoke(LlmRequestError.RateLimited);
                }
                else
                {
                    Debug.LogWarning($"[LlmReactionClient] Request failed: {request.result}, HTTP {request.responseCode}, error: {request.error}, body: {request.downloadHandler?.text}.");
                    onFailure?.Invoke(LlmRequestError.NetworkError);
                }
            }
        }

        private string BuildJsonPayload(string systemPrompt, string singleTurnUserMessage, int? maxTokensOverride = null)
        {
            int tokens = maxTokensOverride.HasValue ? maxTokensOverride.Value : settings.maxTokensPerResponse;
            GroqApiRequest request = new GroqApiRequest
            {
                model = settings.groqModel,
                max_completion_tokens = tokens,
                temperature = settings.temperature,
                reasoning_effort = string.IsNullOrWhiteSpace(settings.reasoningEffort) ? "none" : settings.reasoningEffort
            };

            request.messages.Add(new GroqApiMessage { role = "system", content = systemPrompt });
            if (!string.IsNullOrWhiteSpace(singleTurnUserMessage))
            {
                request.messages.Add(new GroqApiMessage { role = "user", content = singleTurnUserMessage });
            }

            return JsonUtility.ToJson(request);
        }

        private string BuildPetitionJsonPayload(List<GroqApiMessage> messages, int? maxTokensOverride = null)
        {
            int tokens = maxTokensOverride.HasValue ? maxTokensOverride.Value : settings.maxTokensPerResponse;
            GroqPetitionApiRequest request = new GroqPetitionApiRequest
            {
                model = settings.groqModel,
                max_completion_tokens = tokens,
                temperature = settings.temperature,
                messages = messages,
                reasoning_effort = string.IsNullOrWhiteSpace(settings.reasoningEffort) ? "none" : settings.reasoningEffort
            };

            return JsonUtility.ToJson(request);
        }

        // Strips <think>...</think> blocks emitted by reasoning models that leak chain-of-thought into visible output.
        private static string StripThoughtBlocks(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, @"<think>[\s\S]*?</think>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        }

        // Recovers the reaction field when the model ignores response_format and returns raw petition JSON as plain text.
        private static string StripLeakedPetitionJson(string content)
        {
            string trimmed = content.Trim();
            if (trimmed.StartsWith("{") && trimmed.Contains("\"reaction\""))
            {
                try { var leaked = JsonUtility.FromJson<PetitionResolution>(trimmed); if (leaked != null && !string.IsNullOrWhiteSpace(leaked.reaction)) return leaked.reaction; } catch { }
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
                    if (string.IsNullOrWhiteSpace(content)) return null;

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
                    if (string.IsNullOrWhiteSpace(content)) return null;

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
