using System;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    public class StartScreenView : SingleLocalizedButtonView
    {
        [SerializeField, Tooltip("Button that begins the run.")] private Button playButton;
        [SerializeField, Tooltip("Play button label. Auto-fetched from the button if left empty.")]
        private TMP_Text playButtonText;
        [SerializeField, Tooltip("Localized Play button label.")]
        private LocalizedText playButtonLocalized = new LocalizedText
        {
            english = "Play",
            arabic = "ابدأ"
        };

        public event Action PlayRequested;

        protected override Button ViewButton => playButton;

        protected override TMP_Text ViewLabel
        {
            get => playButtonText;
            set => playButtonText = value;
        }

        protected override LocalizedText DefaultLabelText => playButtonLocalized;
        protected override string ComponentTag => nameof(StartScreenView);
        protected override string ButtonFieldName => nameof(playButton);

        protected override void HandleButtonClicked() => PlayRequested?.Invoke();
    }
}
