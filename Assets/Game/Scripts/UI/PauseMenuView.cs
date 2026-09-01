using System;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Pause menu with a single localized Resume button.</summary>
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

        /// <summary>Raised when the player clicks Resume.</summary>
        public event Action ResumeRequested;

        protected override Button ViewButton => resumeButton;

        protected override TMP_Text ViewLabel
        {
            get => resumeButtonText;
            set => resumeButtonText = value;
        }

        protected override LocalizedText DefaultLabelText => resumeButtonLocalized;
        protected override string ComponentTag => nameof(PauseMenuView);
        protected override string ButtonFieldName => nameof(resumeButton);

        protected override void HandleButtonClicked() => ResumeRequested?.Invoke();

        /// <summary>Starts hidden; the menu is shown only while the game is paused.</summary>
        protected override void OnAwakeCompleted()
        {
            gameObject.SetActive(false);
        }
    }
}
