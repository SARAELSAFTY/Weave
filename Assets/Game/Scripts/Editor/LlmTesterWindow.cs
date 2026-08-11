using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Runtime.Llm;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Game.Scripts.Editor
{
    /// <summary>
    /// Manual Groq request inspector (Weave → LLM Tester). Requires Play Mode for coroutines.
    /// </summary>
    public class LlmTesterWindow : EditorWindow
    {
        private LlmSettings settings;
        private TextAsset apiKeyAsset;

        private enum RequestMode { Reaction, Petition }
        private RequestMode mode = RequestMode.Reaction;
        private string systemPrompt = "You are a wise royal advisor. Speak one sentence in character.";
        private string userTurn = "What do you think of the harvest situation?";

        private string sentPayload = string.Empty;
        private string rawResponse = string.Empty;
        private string parsedResult = string.Empty;
        private string statusLine = string.Empty;
        private bool isBusy;
        private Vector2 promptScroll;
        private Vector2 resultScroll;

        private TesterRunner runner;

        [MenuItem("Weave/LLM Tester")]
        public static void Open()
        {
            LlmTesterWindow window = GetWindow<LlmTesterWindow>("LLM Tester");
            window.minSize = new Vector2(480, 560);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            EditorGUILayout.Space(6);
            DrawSettings();
            EditorGUILayout.Space(6);
            DrawPromptArea();
            EditorGUILayout.Space(6);
            DrawSendButton();
            EditorGUILayout.Space(6);
            DrawResults();
        }

        private void DrawHeader()
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            EditorGUILayout.LabelField("Groq API Manual Tester", titleStyle);
            EditorGUILayout.LabelField("Fires real requests with the same payload as the game.", EditorStyles.miniLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to send requests (coroutines require a running MonoBehaviour).", MessageType.Warning);
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            settings = (LlmSettings)EditorGUILayout.ObjectField("LLM Settings", settings, typeof(LlmSettings), false);
            apiKeyAsset = (TextAsset)EditorGUILayout.ObjectField("API Key Asset", apiKeyAsset, typeof(TextAsset), false);

            if (settings != null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("Model", settings.groqModel);
                    EditorGUILayout.IntField("Max Tokens", settings.maxTokensPerResponse);
                    EditorGUILayout.FloatField("Temperature", settings.temperature);
                }
            }
        }

        private void DrawPromptArea()
        {
            EditorGUILayout.LabelField("Request", EditorStyles.boldLabel);

            mode = (RequestMode)EditorGUILayout.EnumPopup("Mode", mode);

            EditorGUILayout.LabelField("System Prompt:");
            promptScroll = EditorGUILayout.BeginScrollView(promptScroll, GUILayout.Height(100));
            systemPrompt = EditorGUILayout.TextArea(systemPrompt, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (mode == RequestMode.Reaction)
            {
                EditorGUILayout.LabelField("User Turn:");
                userTurn = EditorGUILayout.TextField(userTurn);
            }
            else
            {
                EditorGUILayout.HelpBox("Petition mode sends system prompt only (no user turn) and requests json_object response_format.", MessageType.Info);

                if (string.IsNullOrWhiteSpace(systemPrompt) || systemPrompt.IndexOf("json", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    EditorGUILayout.HelpBox("Groq petition requests need the prompt to mention 'json' when response_format is json_object.", MessageType.Warning);
                }
            }
        }

        private void DrawSendButton()
        {
            using (new EditorGUI.DisabledScope(isBusy || !Application.isPlaying || settings == null || apiKeyAsset == null))
            {
                string label = isBusy ? "Sending…" : $"Send {mode} Request";
                if (GUILayout.Button(label, GUILayout.Height(32)))
                {
                    Send();
                }
            }
        }

        private void DrawResults()
        {
            if (string.IsNullOrEmpty(statusLine) && string.IsNullOrEmpty(sentPayload))
            {
                return;
            }

            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(statusLine))
            {
                MessageType msgType = statusLine.StartsWith("✓") ? MessageType.Info
                    : statusLine.StartsWith("⚠") ? MessageType.Warning
                    : MessageType.Error;
                EditorGUILayout.HelpBox(statusLine, msgType);
            }

            if (!string.IsNullOrEmpty(parsedResult))
            {
                EditorGUILayout.LabelField("Parsed Output:");
                EditorGUILayout.HelpBox(parsedResult, MessageType.None);
            }

            resultScroll = EditorGUILayout.BeginScrollView(resultScroll);

            if (!string.IsNullOrEmpty(sentPayload))
            {
                EditorGUILayout.LabelField("JSON Sent:", EditorStyles.miniBoldLabel);
                EditorGUILayout.TextArea(sentPayload, EditorStyles.helpBox, GUILayout.ExpandHeight(false));
            }

            if (!string.IsNullOrEmpty(rawResponse))
            {
                EditorGUILayout.LabelField("Raw Response:", EditorStyles.miniBoldLabel);
                EditorGUILayout.TextArea(rawResponse, EditorStyles.helpBox, GUILayout.ExpandHeight(false));
            }

            EditorGUILayout.EndScrollView();
        }

        private void Send()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            isBusy = true;
            sentPayload = string.Empty;
            rawResponse = string.Empty;
            parsedResult = string.Empty;
            statusLine = "Sending…";
            Repaint();

            if (runner == null)
            {
                GameObject go = new GameObject("[LlmTesterRunner]")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                runner = go.AddComponent<TesterRunner>();
            }

            string apiKey = apiKeyAsset != null ? apiKeyAsset.text.Trim() : string.Empty;
            string payload = BuildPayload(out string payloadStr);
            sentPayload = payloadStr;

            runner.Run(payload, apiKey, settings.apiTimeoutSeconds,
                onDone: (httpCode, responseBody) =>
                {
                    rawResponse = responseBody;
                    HandleResponse(httpCode, responseBody);
                    isBusy = false;
                    Repaint();
                });
        }

        private string BuildPayload(out string prettyJson)
        {
            string json;

            if (mode == RequestMode.Petition)
            {
                GroqPetitionApiRequest req = new GroqPetitionApiRequest
                {
                    model = settings.groqModel,
                    max_completion_tokens = settings.maxTokensPerResponse,
                    temperature = settings.temperature,
                    reasoning_effort = string.IsNullOrWhiteSpace(settings.reasoningEffort) ? "none" : settings.reasoningEffort
                };
                req.messages.Add(new GroqApiMessage { role = "system", content = systemPrompt });
                json = JsonUtility.ToJson(req, true);
            }
            else
            {
                GroqApiRequest req = new GroqApiRequest
                {
                    model = settings.groqModel,
                    max_completion_tokens = settings.maxTokensPerResponse,
                    temperature = settings.temperature,
                    reasoning_effort = string.IsNullOrWhiteSpace(settings.reasoningEffort) ? "none" : settings.reasoningEffort
                };
                req.messages.Add(new GroqApiMessage { role = "system", content = systemPrompt });
                req.messages.Add(new GroqApiMessage { role = "user", content = userTurn });
                json = JsonUtility.ToJson(req, true);
            }

            prettyJson = json;
            return json;
        }

        private void HandleResponse(long httpCode, string body)
        {
            if (httpCode >= 200 && httpCode < 300)
            {
                try
                {
                    GroqResponse resp = JsonUtility.FromJson<GroqResponse>(body);
                    string content = resp?.choices?[0]?.message?.content;
                    parsedResult = string.IsNullOrWhiteSpace(content) ? "(empty content)" : content;
                    statusLine = $"✓ HTTP {httpCode} — OK";
                }
                catch (Exception ex)
                {
                    parsedResult = string.Empty;
                    statusLine = $"✗ HTTP {httpCode} — Parse error: {ex.Message}";
                }
            }
            else
            {
                parsedResult = string.Empty;

                string errorDetail = TryExtractGroqError(body);
                statusLine = $"✗ HTTP {httpCode}{(string.IsNullOrEmpty(errorDetail) ? string.Empty : " — " + errorDetail)}";
            }
        }

        private static string TryExtractGroqError(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            // Shape: {"error":{"message":"...","type":"...","code":"..."}}
            try
            {
                GroqErrorWrapper wrapper = JsonUtility.FromJson<GroqErrorWrapper>(body);
                return wrapper?.error?.message ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        [Serializable]
        private class GroqErrorWrapper
        {
            public GroqErrorBody error;
        }

        [Serializable]
        private class GroqErrorBody
        {
            public string message;
            public string type;
            public string code;
        }

        private class TesterRunner : MonoBehaviour
        {
            public void Run(string jsonPayload, string apiKey, float timeout, Action<long, string> onDone)
            {
                StartCoroutine(SendRoutine(jsonPayload, apiKey, timeout, onDone));
            }

            private IEnumerator SendRoutine(string jsonPayload, string apiKey, float timeout, Action<long, string> onDone)
            {
                const string url = "https://api.groq.com/openai/v1/chat/completions";

                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    byte[] body = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    request.timeout = Mathf.CeilToInt(timeout);

                    yield return request.SendWebRequest();

                    onDone?.Invoke(request.responseCode, request.downloadHandler.text ?? string.Empty);
                }
            }
        }
    }
}