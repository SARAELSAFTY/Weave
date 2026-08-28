using System.Text.RegularExpressions;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Removes characters that cannot appear in Arabic narrative text.</summary>
    public static class LlmTextSanitizer
    {
        // Arabic blocks, whitespace, ASCII digits, and common narration punctuation.
        private static readonly Regex NonArabic = new Regex(
            @"[^\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF\s.,!?؟،0-9]");

        public static string StripNonArabic(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return NonArabic.Replace(text, string.Empty).Trim();
        }
    }
}
