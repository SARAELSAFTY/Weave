namespace Game.Scripts.Localization
{
    /// <summary>Holds paired English and Arabic strings for a single piece of localized content.</summary>
    [System.Serializable]
    public struct LocalizedText
    {
        /// <summary>English version of the text.</summary>
        [UnityEngine.Tooltip("English version of the text.")]
        public string english;

        /// <summary>Arabic version of the text.</summary>
        [UnityEngine.Tooltip("Arabic version of the text.")]
        public string arabic;

        /// <summary>True when both language strings are null or whitespace.</summary>
        public bool IsEmpty => string.IsNullOrWhiteSpace(english) && string.IsNullOrWhiteSpace(arabic);

        /// <summary>Returns the string for the requested language, falling back to English if Arabic is empty.</summary>
        /// <param name="language">Target language.</param>
        public string Get(GameLanguage language)
        {
            if (language == GameLanguage.Arabic && !string.IsNullOrWhiteSpace(arabic))
            {
                return arabic;
            }

            return english ?? string.Empty;
        }
    }
}
