using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.Runtime.Llm
{
    public class LlmReactionClient : MonoBehaviour
    {
        private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";

        [SerializeField, Tooltip("Global LLM settings.")]
        private LlmSettings settings;

        [SerializeField, Tooltip("Prompt templates and default copy.")]
        private LlmPromptTemplates promptTemplates;

        [SerializeField, Tooltip("TextAsset containing the Groq API key. Drag the key file here.")]
        private TextAsset apiKeyAsset;

        private string cachedApiKey;

        public void RequestReaction(string systemPrompt, Action<string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            if (settings == null)
            {
                Debug.LogError("[LlmReactionClient] LlmSettings reference is missing!");
                onFailure?.Invoke(LlmRequestError.NotConfigured);
                return;
            }

            if (string.IsNullOrEmpty(GetApiKey()))
            {
                Debug.LogError("[LlmReactionClient] API key is missing. Assign a TextAsset containing the Groq API key to the apiKeyAsset field in the Inspector.");
                onFailure?.Invoke(LlmRequestError.NotConfigured);
                return;
            }

            StartCoroutine(RequestRoutine(systemPrompt, onSuccess, onFailure, maxTokensOverride));
        }

        /// <summary>
        /// Sends a full multi-turn message list (from <see cref="PetitionSession.BuildMessagesForSubmission"/>)
        /// and returns the parsed resolution plus cleaned raw text for <see cref="PetitionSession.RecordReply"/>.
        /// </summary>
        public void RequestPetitionTurn(List<GroqApiMessage> messages, Action<PetitionResolution, string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            if (settings == null)
            {
                Debug.LogError("[LlmReactionClient] LlmSettings reference is missing!");
                onFailure?.Invoke(LlmRequestError.NotConfigured);
                return;
            }

            if (string.IsNullOrEmpty(GetApiKey()))
            {
                Debug.LogError("[LlmReactionClient] API key is missing. Assign a TextAsset containing the Groq API key to the apiKeyAsset field in the Inspector.");
                onFailure?.Invoke(LlmRequestError.NotConfigured);
                return;
            }

            StartCoroutine(RequestPetitionRoutine(messages, onSuccess, onFailure, maxTokensOverride));
        }

        private IEnumerator RequestPetitionRoutine(List<GroqApiMessage> messages, Action<PetitionResolution, string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            LlmRequestError? error = null;
            PetitionResolution result = null;
            string rawContent = null;

            string jsonPayload;
            try
            {
                jsonPayload = BuildPetitionJsonPayload(messages, maxTokensOverride);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LlmReactionClient] Unexpected error while building petition request payload: {exception}");
                error = LlmRequestError.NetworkError;
                jsonPayload = null;
            }

            if (!error.HasValue)
            {
                using (UnityWebRequest request = new UnityWebRequest(GroqApiUrl, "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", $"Bearer {GetApiKey()}");
                    request.timeout = Mathf.CeilToInt(settings.apiTimeoutSeconds);

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            result = ParsePetitionResponse(request.downloadHandler.text, out rawContent);
                            if (result == null)
                            {
                                Debug.LogWarning($"[LlmReactionClient] Petition request succeeded but no resolution was parsed. Response body: {request.downloadHandler.text}");
                                error = LlmRequestError.EmptyResponse;
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError($"[LlmReactionClient] Unexpected error while parsing petition response: {exception}");
                            error = LlmRequestError.EmptyResponse;
                        }
                    }
                    else if (request.responseCode == 429)
                    {
                        Debug.LogWarning("[LlmReactionClient] Rate limited (429).");
                        error = LlmRequestError.RateLimited;
                    }
                    else
                    {
                        Debug.LogWarning($"[LlmReactionClient] Petition request failed: {request.result}, HTTP {request.responseCode}, error: {request.error}, body: {request.downloadHandler.text}.");
                        error = LlmRequestError.NetworkError;
                    }
                }
            }

            if (error.HasValue)
            {
                onFailure?.Invoke(error.Value);
            }
            else
            {
                onSuccess?.Invoke(result, rawContent);
            }
        }

        private IEnumerator RequestRoutine(string systemPrompt, Action<string> onSuccess, Action<LlmRequestError> onFailure, int? maxTokensOverride = null)
        {
            LlmRequestError? error = null;
            string result = null;

            string jsonPayload;
            try
            {
                jsonPayload = BuildJsonPayload(systemPrompt, maxTokensOverride);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[LlmReactionClient] Unexpected error while building request payload: {exception}");
                error = LlmRequestError.NetworkError;
                jsonPayload = null;
            }

            if (!error.HasValue)
            {
                using (UnityWebRequest request = new UnityWebRequest(GroqApiUrl, "POST"))
                {
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", $"Bearer {GetApiKey()}");
                    request.timeout = Mathf.CeilToInt(settings.apiTimeoutSeconds);

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            result = ParseResponse(request.downloadHandler.text);
                            if (string.IsNullOrEmpty(result))
                            {
                                Debug.LogWarning($"[LlmReactionClient] Request succeeded but no reaction text was parsed. Response body: {request.downloadHandler.text}");
                                error = LlmRequestError.EmptyResponse;
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError($"[LlmReactionClient] Unexpected error while parsing response: {exception}");
                            error = LlmRequestError.NetworkError;
                        }
                    }
                    else if (request.responseCode == 429)
                    {
                        Debug.LogWarning("[LlmReactionClient] Rate limited (429).");
                        error = LlmRequestError.RateLimited;
                    }
                    else
                    {
                        Debug.LogWarning($"[LlmReactionClient] Request failed: {request.result}, HTTP {request.responseCode}, error: {request.error}, body: {request.downloadHandler.text}.");
                        error = LlmRequestError.NetworkError;
                    }
                }
            }

            if (error.HasValue)
            {
                onFailure?.Invoke(error.Value);
            }
            else
            {
                onSuccess?.Invoke(result);
            }
        }

        private string BuildJsonPayload(string systemPrompt, int? maxTokensOverride = null)
        {
            int tokens = maxTokensOverride.HasValue ? maxTokensOverride.Value : settings.maxTokensPerResponse;
            GroqApiRequest request = new GroqApiRequest
            {
                model = settings.groqModel,
                max_completion_tokens = tokens,
                temperature = settings.temperature,
                reasoning_effort = string.IsNullOrWhiteSpace(settings.reasoningEffort) ? "none" : settings.reasoningEffort
            };

            string userTurnPrompt = promptTemplates != null
                ? promptTemplates.reactionUserTurnPrompt
                : string.Empty;

            request.messages.Add(new GroqApiMessage { role = "system", content = systemPrompt });
            request.messages.Add(new GroqApiMessage { role = "user", content = userTurnPrompt });

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

        private string ParseResponse(string json)
        {
            try
            {
                GroqResponse response = JsonUtility.FromJson<GroqResponse>(json);
                if (response?.choices != null && response.choices.Length > 0)
                {
                    string content = response.choices[0].message?.content;
                    if (string.IsNullOrWhiteSpace(content)) return null;

                    // Defense in depth if reasoning_effort is misconfigured away from "none".
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"<think>[\s\S]*?</think>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

                    // Strip stage directions the model sometimes wraps in () or * *.
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"\([^)]*\)", "").Trim();
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"\*[^*]*\*", "").Trim();

                    content = System.Text.RegularExpressions.Regex.Replace(content, @"\s+", " ");
                    content = content.Trim('"', '\'', ' ');

                    return content;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LlmReactionClient] Parsing error: {exception.Message}");
            }

            return null;
        }

        private PetitionResolution ParsePetitionResponse(string json, out string rawContent)
        {
            rawContent = null;
            try
            {
                GroqResponse response = JsonUtility.FromJson<GroqResponse>(json);
                if (response?.choices != null && response.choices.Length > 0)
                {
                    string content = response.choices[0].message?.content;
                    if (string.IsNullOrWhiteSpace(content)) return null;

                    content = content.Trim();

                    // Strip <think> blocks before JSON parse — a leading block would break FromJson.
                    content = System.Text.RegularExpressions.Regex.Replace(content, @"<think>[\s\S]*?</think>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

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

        private string GetApiKey()
        {
            if (!string.IsNullOrEmpty(cachedApiKey))
            {
                return cachedApiKey;
            }

            if (apiKeyAsset != null && !string.IsNullOrWhiteSpace(apiKeyAsset.text))
            {
                cachedApiKey = apiKeyAsset.text.Trim();
            }

            return cachedApiKey;
        }
    }
}
