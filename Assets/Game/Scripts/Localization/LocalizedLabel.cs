using TMPro;
using UnityEngine;

namespace Game.Scripts.Localization
{
    /// <summary>
    /// Drop-in component for any TMP_Text in menus or UI.
    /// Define English and Arabic text in the Inspector, and it automatically
    /// refreshes whenever the game language changes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Weave/Localization/Localized Label")]
    public class LocalizedLabel : MonoBehaviour
    {
        [SerializeField] private TMP_Text textComponent;

        [SerializeField, Tooltip("Font category used for category-specific font overrides from FontSettings.")]
        private TextFontCategory fontCategory = TextFontCategory.Default;

        [SerializeField, TextArea(1, 5), Tooltip("Text shown when game is in English.")]
        private string english;

        [SerializeField, TextArea(1, 5), Tooltip("Text shown when game is in Arabic.")]
        private string arabic;

        public string English
        {
            get => english;
            set
            {
                english = value;
                Refresh();
            }
        }

        public string Arabic
        {
            get => arabic;
            set
            {
                arabic = value;
                Refresh();
            }
        }

        public TextFontCategory FontCategory
        {
            get => fontCategory;
            set
            {
                fontCategory = value;
                Refresh();
            }
        }

        public TMP_Text TargetText => textComponent;

        private void Reset()
        {
            if (textComponent == null)
            {
                textComponent = GetComponent<TMP_Text>();
            }

            if (textComponent != null && string.IsNullOrEmpty(english))
            {
                english = textComponent.text;
            }
        }

        private void Awake()
        {
            if (textComponent == null)
            {
                textComponent = GetComponent<TMP_Text>();
            }

            if (textComponent != null)
            {
                RtlTextHelper.Configure(textComponent);
            }
        }

        private void OnEnable()
        {
            if (textComponent == null)
            {
                textComponent = GetComponent<TMP_Text>();
            }

            if (Application.isPlaying && LanguageManager.Instance != null)
            {
                LanguageManager.Instance.LanguageChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (Application.isPlaying && LanguageManager.HasInstance)
            {
                LanguageManager.Instance.LanguageChanged -= Refresh;
            }
        }

        private void OnValidate()
        {
            if (textComponent == null)
            {
                textComponent = GetComponent<TMP_Text>();
            }

            if (!Application.isPlaying && textComponent != null)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (textComponent == null)
            {
                return;
            }

            GameLanguage language = Application.isPlaying && LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : GameLanguage.English;

            string content = language == GameLanguage.Arabic && !string.IsNullOrWhiteSpace(arabic)
                ? arabic
                : (english ?? string.Empty);

            RtlTextHelper.SetText(textComponent, content, language, fontCategory);
        }
    }
}
