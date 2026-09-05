using UnityEngine;

namespace Game.Scripts.Llm
{
    /// <summary>Provides simple token-based placeholder substitution for LLM prompt templates.</summary>
    public static class PromptTemplateUtility
    {
        /// <summary>Replaces all occurrences of {token} in the template with the given value.</summary>
        /// <param name="template">Prompt template string containing {token} placeholders.</param>
        /// <param name="token">Placeholder name without braces (e.g. "resourceName").</param>
        /// <param name="value">Replacement string; null is treated as empty.</param>
        /// <returns>The template with placeholders replaced, or empty string if the template was null/empty.</returns>
        /// <remarks>Logs a warning when the expected placeholder is not found in the template,
        /// which usually indicates a mismatch between the template text and the calling code.</remarks>
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
