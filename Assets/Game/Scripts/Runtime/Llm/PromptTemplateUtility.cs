using UnityEngine;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>
    /// Fills a single {token} placeholder in an author-editable template string. Centralizes the pattern so
    /// every templated seed prompt (e.g. LlmPromptTemplates.defaultWarningSeedPrompt) is filled the same safe
    /// way instead of an ad-hoc string.Replace at the call site, and warns instead of silently no-op'ing if an
    /// Inspector edit accidentally removes the token.
    /// </summary>
    public static class PromptTemplateUtility
    {
        public static string Fill(string template, string token, string value)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            string placeholder = "{" + token + "}";
            if (!template.Contains(placeholder))
            {
                Debug.LogWarning($"[PromptTemplateUtility] Template does not contain expected placeholder '{placeholder}'. '{value}' will not appear in the prompt sent to the LLM.");
            }

            return template.Replace(placeholder, value ?? string.Empty);
        }
    }
}


