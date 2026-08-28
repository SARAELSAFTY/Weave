using System;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    public class PauseMenuView : SingleLocalizedButtonView
    {
        [SerializeField, Tooltip("Button that resumes the run.")] private Button resumeButton;
        [SerializeField, Tooltip("Resume button label. Auto-fetched from the button if left empty.")]
        private TMP_Text resumeButtonText;
        [SerializeField, Tooltip("Localized Resume button label.")]
        private LocalizedText resumeButtonLocalized = new LocalizedText
        {
            english = "Resume",
            arabic = "متابعة"
        };

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

        protected override void OnAwakeCompleted()
        {
            gameObject.SetActive(false);
        }
    }
}
