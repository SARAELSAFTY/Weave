namespace Game.Scripts.Localization
{
    [System.Serializable]
    public struct LocalizedText
    {
        public string english;
        public string arabic;

        public bool IsEmpty => string.IsNullOrWhiteSpace(english) && string.IsNullOrWhiteSpace(arabic);

        public string Get(GameLanguage language)
        {
            if (language == GameLanguage.Arabic && !string.IsNullOrWhiteSpace(arabic))
                return arabic;
            return english ?? string.Empty;
        }
    }
}
