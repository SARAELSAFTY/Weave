using System;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Pause menu with localized Resume and Restart buttons.</summary>
    public class PauseMenuView : SingleLocalizedButtonView
    {
        [Tooltip("Button that resumes the game when clicked.")]
        [SerializeField] private Button resumeButton;
        [Tooltip("Label on the resume button. Found automatically from the button's children if left empty.")]
        [SerializeField]
        private TMP_Text resumeButtonText;
        [Tooltip("Localized label shown on the resume button.")]
        [SerializeField]
        private LocalizedText resumeButtonLocalized = new LocalizedText
        {
            english = "Resume",
            arabic = "متابعة"
        };

        [Tooltip("Button that restarts the game when clicked.")]
        [SerializeField] private Button restartButton;
        [Tooltip("Label on the restart button. Found automatically from the button's children if left empty.")]
        [SerializeField]
        private TMP_Text restartButtonText;
        [Tooltip("Localized label shown on the restart button.")]
        [SerializeField]
        private LocalizedText restartButtonLocalized = new LocalizedText
        {
            english = "Restart",
            arabic = "إعادة التشغيل"
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

        /// <summary>Raised when the player clicks Resume.</summary>
        public event Action ResumeRequested;

        /// <summary>Raised when the player clicks Restart.</summary>
        public event Action RestartRequested;

        /// <summary>Raised when the player clicks the API key button.</summary>
        public event Action ApiKeyRequested;

        protected override Button ViewButton => resumeButton;

        protected override TMP_Text ViewLabel
        {
            get => resumeButtonText;
            set => resumeButtonText = value;
        }

        protected override LocalizedText DefaultLabelText => resumeButtonLocalized;
        protected override string ComponentTag => nameof(PauseMenuView);
        protected override string ButtonFieldName => nameof(resumeButton);

        protected override void OnAwakeCompleted()
        {
            gameObject.SetActive(false);

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(HandleRestartClicked);

                if (restartButtonText == null)
                {
                    restartButtonText = restartButton.GetComponentInChildren<TMP_Text>();
                }

                if (restartButtonText != null)
                {
                    restartButtonText.text = restartButtonLocalized.Get(LanguageManager.CurrentLanguageOrDefault);
                }
            }

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

        protected override void HandleButtonClicked() => ResumeRequested?.Invoke();

        private void HandleRestartClicked() => RestartRequested?.Invoke();

        private void HandleApiKeyClicked() => ApiKeyRequested?.Invoke();

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(HandleRestartClicked);
            }

            if (apiKeyButton != null)
            {
                apiKeyButton.onClick.RemoveListener(HandleApiKeyClicked);
            }
        }
    }
}