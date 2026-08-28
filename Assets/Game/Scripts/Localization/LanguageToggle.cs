using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Localization
{
    public class LanguageToggle : LocalizedDisplay
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        [SerializeField, Tooltip("Custom localized text. If a LocalizedLabel component is present, it will be used instead.")]
        private LocalizedText localizedText;

        private LocalizedLabel localizedLabelComponent;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (label == null && button != null)
            {
                label = button.GetComponentInChildren<TMP_Text>();
            }

            if (label != null)
            {
                localizedLabelComponent = label.GetComponent<LocalizedLabel>();
            }

            if (button == null)
            {
                Debug.LogError($"[LanguageToggle] Missing Button on '{gameObject.name}'.", this);
                enabled = false;
                return;
            }

            if (label != null)
            {
                RtlTextHelper.Configure(label);
            }

            button.onClick.AddListener(Toggle);
        }

        public void Toggle()
        {
            if (LanguageManager.Instance == null)
            {
                return;
            }

            GameLanguage next = LanguageManager.Instance.CurrentLanguage == GameLanguage.English
                ? GameLanguage.Arabic
                : GameLanguage.English;

            LanguageManager.Instance.SetLanguage(next);
        }

        protected override void RefreshContent(GameLanguage language)
        {
            if (label == null)
            {
                return;
            }

            // If LocalizedLabel is attached, let it handle the display.
            if (localizedLabelComponent != null)
            {
                localizedLabelComponent.Refresh();
                return;
            }

            // Otherwise, if localizedText is configured in Inspector, use it.
            if (!localizedText.IsEmpty)
            {
                RtlTextHelper.SetText(label, localizedText.Get(language), language);
            }
        }
    }
}
