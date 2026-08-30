using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Localization;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.Runtime.Llm
{
    public class LlmReactionClient : MonoBehaviour
    {
        [SerializeField, Tooltip("Global LLM settings.")]
        private LlmSettings settings;

        [SerializeField, Tooltip("URL of the LLM proxy server.")]
        private string proxyUrl = "https://your-proxy.workers.dev";

        /// <summary>
        /// Assigns settings/proxyUrl in code instead of via the Inspector. Intended for editor tooling
        /// (LlmTesterWindow) that spins up a temporary instance of this component so it can reuse the real
        /// request/parse logic below instead of duplicating it.
        /// </summary>
        public void Configure(LlmSettings settingsToUse, string proxyUrlToUse)
        {
            settings = settingsToUse;
            proxyUrl = proxyUrlToUse;
        }

        public void RequestReaction(string systemPrompt, string singleTurnUserMessage, GameLanguage language, Action<string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            if (!TryValidateConfig(onFailure))
            {
                return;
            }

            StartCoroutine(RequestRoutine(systemPrompt, singleTurnUserMessage, language, onSuccess, onFailure, maxTokensOverride));
        }

        /// <summary>
        /// Sends a full multi-turn message list (from <see cref="PetitionSession.BuildMessagesForSubmission"/>)
        /// and returns the parsed resolution plus cleaned raw text for <see cref="PetitionSession.RecordReply"/>.
        /// </summary>
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

        private static string StripThoughtBlocks(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, @"<think>[\s\S]*?</think>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        }

        private static string StripLeakedPetitionJson(string content)
        {
            string trimmed = content.Trim();
            if (trimmed.StartsWith("{") && trimmed.Contains("\"reaction\""))
            {
                try { var leaked = JsonUtility.FromJson<PetitionResolution>(trimmed); if (leaked != null && !string.IsNullOrWhiteSpace(leaked.reaction)) return leaked.reaction; } catch { }
            }
            return content;
        }

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

                    // Defense in depth if reasoning_effort is misconfigured away from "none".
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

                    // Strip <think> blocks before JSON parse - a leading block would break FromJson.
                    content = StripThoughtBlocks(content);

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
