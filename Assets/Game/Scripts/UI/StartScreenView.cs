using System;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Start screen with a single localized Play button.</summary>
    public class StartScreenView : SingleLocalizedButtonView
    {
        [Tooltip("Button that starts the game when clicked.")]
        [SerializeField] private Button playButton;
        [Tooltip("Label on the play button. Found automatically from the button's children if left empty.")]
        [SerializeField]
        private TMP_Text playButtonText;
        [Tooltip("Localized label shown on the play button.")]
        [SerializeField]
        private LocalizedText playButtonLocalized = new LocalizedText
        {
            english = "Play",
            arabic = "ابدأ"
        };

        [Tooltip("Button that opens the API key panel; optional.")]
        [SerializeField] private Button apiKeyButton;
        [Tooltip("Label on the API key button. Found automatically from the button's children if left empty.")]
        [SerializeField]
        private TMP_Text apiKeyButtonText;
        [Tooltip("Localized label shown on the API key button.")]
        [SerializeField]
        private LocalizedText apiKeyButtonLocalized = new LocalizedText
        {
            english = "AI",
            arabic = "الذكاء الاصطناعي"
        };

        /// <summary>Raised when the player clicks Play.</summary>
        public event Action PlayRequested;

        /// <summary>Raised when the player clicks the API key button.</summary>
        public event Action ApiKeyRequested;

        protected override Button ViewButton => playButton;

        protected override TMP_Text ViewLabel
        {
            get => playButtonText;
            set => playButtonText = value;
        }

        protected override LocalizedText DefaultLabelText => playButtonLocalized;
        protected override string ComponentTag => nameof(StartScreenView);
        protected override string ButtonFieldName => nameof(playButton);

        protected override void OnAwakeCompleted()
        {
            if (apiKeyButton != null)
            {
                apiKeyButton.onClick.AddListener(HandleApiKeyClicked);

                if (apiKeyButtonText == null)
                {
                    apiKeyButtonText = apiKeyButton.GetComponentInChildren<TMP_Text>();
                }

                if (apiKeyButtonText != null)
                {
                    RtlTextHelper.SetText(apiKeyButtonText, apiKeyButtonLocalized.Get(LanguageManager.CurrentLanguageOrDefault), LanguageManager.CurrentLanguageOrDefault);
                }
            }
        }

        protected override void HandleButtonClicked() => PlayRequested?.Invoke();

        private void HandleApiKeyClicked() => ApiKeyRequested?.Invoke();

        private void OnDestroy()
        {
            if (apiKeyButton != null)
            {
                apiKeyButton.onClick.RemoveListener(HandleApiKeyClicked);
            }
        }
    }
}
