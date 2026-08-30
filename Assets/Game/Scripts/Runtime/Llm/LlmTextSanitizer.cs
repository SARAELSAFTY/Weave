using System.Text.RegularExpressions;

namespace Game.Scripts.Runtime.Llm
{
    /// <summary>Removes characters that cannot appear in Arabic narrative text.</summary>
    public static class LlmTextSanitizer
    {
        // Arabic blocks, whitespace, ASCII digits, and common narration punctuation.
        private static readonly Regex NonArabic = new Regex(
            @"[^\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF\s.,!?؟،0-9]");

        // Models occasionally break a word into "letter[.] letter" with a single orphaned letter
        // after the break (e.g. "الموار. د"). Single-letter Arabic words do not occur in correct
        // text, so rejoin them. و is excluded because it can legitimately appear isolated.
        private static readonly Regex OrphanedFinalLetter = new Regex(
            @"([\u0600-\u06FF\uFB50-\uFDFF\uFE70-\uFEFF])\.?\s+(?!\u0648)([\u0600-\u06FF\uFB50-\uFDFF\uFE70-\uFEFF])(?=[\s.,!?؟،]|$)");

        public static string StripNonArabic(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string cleaned = NonArabic.Replace(text, string.Empty);
            cleaned = OrphanedFinalLetter.Replace(cleaned, "$1$2");
            return cleaned.Trim();
        }
    }
}
