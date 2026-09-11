using System.Text.RegularExpressions;

namespace Game.Scripts.Llm
{
    /// <summary>Repairs model-mangled Arabic text by stripping non-Arabic characters and rejoining orphaned final letters.</summary>
    /// <remarks>LLMs sometimes split Arabic words at letter boundaries, producing isolated final-form letters
    /// separated by whitespace. The <see cref="OrphanedFinalLetter"/> regex detects and merges these back.</remarks>
    public static class LlmTextSanitizer
    {
        // Matches any character outside Arabic Unicode blocks (Arabic, Arabic Supplement, Arabic Presentation Forms-A/B)
        // while preserving whitespace, common punctuation, Arabic-specific punctuation (؟ ،), and digits.
        private static readonly Regex NonArabic = new Regex(
            @"[^\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF\s.,!?؟،0-9]");

        // Detects an Arabic letter followed by optional period + whitespace + another Arabic letter at a word boundary,
        // which indicates the model split off a final-form letter. Rejoins them by removing the intervening separator.
        // Negative lookahead (?!\u0648) avoids merging the conjunction و (wa) with adjacent words.
        private static readonly Regex OrphanedFinalLetter = new Regex(
            @"([\u0600-\u06FF\uFB50-\uFDFF\uFE70-\uFEFF])\.?\s+(?!\u0648)([\u0600-\u06FF\uFB50-\uFDFF\uFE70-\uFEFF])(?=[\s.,!?؟،]|$)");

        // Detects whether the string contains at least one Arabic letter.
        private static readonly Regex HasArabicLetters = new Regex(
            @"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]");

        /// <summary>Removes non-Arabic characters and repairs orphaned final-letter splits in the given text.</summary>
        /// <param name="text">Raw LLM output expected to be Arabic.</param>
        /// <returns>Cleaned Arabic text, or empty string if no Arabic letters exist.</returns>
        public static string StripNonArabic(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            string cleaned = NonArabic.Replace(text, string.Empty);
            cleaned = OrphanedFinalLetter.Replace(cleaned, "$1$2").Trim();

            // If stripping non-Arabic left only punctuation/spaces, the model replied in the wrong language;
            // return empty string so the caller can activate the localized fallback.
            if (!HasArabicLetters.IsMatch(cleaned))
            {
                return string.Empty;
            }

            return cleaned;
        }
    }
}
