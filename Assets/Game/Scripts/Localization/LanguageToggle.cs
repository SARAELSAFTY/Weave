using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Localization
{
    /// <summary>A button that toggles the active language between English and Arabic.</summary>
    /// <remarks>
    /// If a LocalizedLabel is present on the label's TMP_Text, delegates rendering to it;
    /// otherwise applies localizedText directly via RtlTextHelper.
    /// </remarks>
    public class LanguageToggle : LocalizedDisplay
    {
        [Tooltip("The Button component that triggers the language toggle.")]
        [SerializeField] private Button button;

        [Tooltip("The text label updated when the language changes.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("Fallback bilingual text displayed on the label when no LocalizedLabel component is present.")]
        [SerializeField]
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

        /// <summary>Switches the active language to the opposite of the current one.</summary>
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

        // Delegates to the child LocalizedLabel if present so it handles its own font/RTL logic;
        // otherwise falls back to applying localizedText directly.
        protected override void RefreshContent(GameLanguage language)
        {
            if (label == null)
            {
                return;
            }

            if (localizedLabelComponent != null)
            {
                localizedLabelComponent.Refresh();
                return;
            }

            if (!localizedText.IsEmpty)
            {
                RtlTextHelper.SetText(label, localizedText.Get(language), language);
            }
        }
    }
}
