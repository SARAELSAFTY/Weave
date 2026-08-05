using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GroqService : MonoBehaviour
{
    private const string GroqApiUrl = "https://api.groq.com/openai/v1/chat/completions";
    [SerializeField] private TextAsset apiKeyAsset;
    private string cachedApiKey;

    public void GetDemonResponse(string systemPrompt, List<ConversationTurn> history, string playerMessage, DeceptionMode mode, DemonConfig config, Action<string, bool> onComplete)
    {
        StartCoroutine(RequestRoutine(systemPrompt, history, playerMessage, mode, config, onComplete));
    }

    private IEnumerator RequestRoutine(string systemPrompt, List<ConversationTurn> history, string playerMessage, DeceptionMode mode, DemonConfig config, Action<string, bool> onComplete)
    {
        string apiKey = GetApiKey();
        if (string.IsNullOrEmpty(apiKey))
        {
            onComplete?.Invoke("The shadows are quiet right now...", true);
            yield break;
        }

        string jsonPayload = BuildJsonPayload(systemPrompt, history, playerMessage, config);

        using UnityWebRequest request = new UnityWebRequest(GroqApiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
        request.timeout = Mathf.CeilToInt(config.apiTimeoutSeconds);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string reply = ParseResponse(request.downloadHandler.text);
            if (!string.IsNullOrEmpty(reply))
            {
                onComplete?.Invoke(reply, false);
                yield break;
            }
        }

        Debug.LogWarning("[GroqService] API network failure, using fallback message.");
        onComplete?.Invoke("Truth or lies... what difference does it make?", true);
    }

    private string BuildJsonPayload(string systemPrompt, List<ConversationTurn> history, string playerMessage, DemonConfig config)
    {
        var request = new GroqApiRequest
        {
            model = config.groqModel,
            max_tokens = config.maxTokensPerResponse,
            temperature = 0.9f
        };

        request.messages.Add(new GroqApiMessage { role = "system", content = systemPrompt });

        if (history != null)
        {
            foreach (var turn in history)
            {
                request.messages.Add(new GroqApiMessage
                {
                    role = turn.speaker == Speaker.PLAYER ? "user" : "assistant",
                    content = turn.message
                });
            }
        }

        if (!string.IsNullOrEmpty(playerMessage))
        {
            request.messages.Add(new GroqApiMessage { role = "user", content = playerMessage });
        }

        return JsonUtility.ToJson(request);
    }

    private string ParseResponse(string json)
    {
        try
        {
            GroqResponse response = JsonUtility.FromJson<GroqResponse>(json);
            if (response?.choices != null && response.choices.Length > 0)
            {
                return response.choices[0].message?.content;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GroqService] Parsing error: {e.Message}");
        }
        return null;
    }

    private string GetApiKey()
    {
        if (!string.IsNullOrEmpty(cachedApiKey)) return cachedApiKey;
        if (apiKeyAsset != null && !string.IsNullOrWhiteSpace(apiKeyAsset.text))
        {
            cachedApiKey = apiKeyAsset.text.Trim();
            return cachedApiKey;
        }
        TextAsset loaded = Resources.Load<TextAsset>("groq_api_key");
        if (loaded != null) cachedApiKey = loaded.text.Trim();
        return cachedApiKey;
    }
}