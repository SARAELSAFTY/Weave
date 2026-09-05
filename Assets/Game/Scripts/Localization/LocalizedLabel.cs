using TMPro;
using UnityEngine;

namespace Game.Scripts.Localization
{
    /// <summary>Displays a bilingual text label on a TMP_Text component, auto-refreshing on language changes.</summary>
    /// <remarks>
    /// Works in both play mode and edit mode (ExecuteAlways). In edit mode the label always shows English
    /// since LanguageManager may not exist outside play mode.
    /// </remarks>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    [AddComponentMenu("Weave/Localization/Localized Label")]
    public class LocalizedLabel : MonoBehaviour
    {
        [Tooltip("The TMP_Text component that displays the localized content.")]
        [SerializeField] private TMP_Text textComponent;

        [Tooltip("Text category used to resolve a per-category font override.")]
        [SerializeField]
        private TextFontCategory fontCategory = TextFontCategory.Default;

        [Tooltip("English version of the label text.")]
        [SerializeField, TextArea(1, 5)]
        private string english;

        [Tooltip("Arabic version of the label text.")]
        [SerializeField, TextArea(1, 5)]
        private string arabic;

        /// <summary>Gets or sets the English text, refreshing the display on change.</summary>
        public string English
        {
            get => english;
            set
            {
                english = value;
                Refresh();
            }
        }

        /// <summary>Gets or sets the Arabic text, refreshing the display on change.</summary>
        public string Arabic
        {
            get => arabic;
            set
            {
                arabic = value;
                Refresh();
            }
        }

        /// <summary>Gets or sets the font category, refreshing the display on change.</summary>
        public TextFontCategory FontCategory
        {
            get => fontCategory;
            set
            {
                fontCategory = value;
                Refresh();
            }
        }

        /// <summary>The resolved TMP_Text component this label writes to.</summary>
        public TMP_Text TargetText => textComponent;

        // Seeds the English field from the existing TMP_Text content when first added via Inspector.
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

        // Subscribes only during play mode; edit mode skips subscription but still refreshes.
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

        // Live preview in the Inspector without entering play mode.
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

        /// <summary>Updates the displayed text, font, and RTL settings for the current language.</summary>
        public void Refresh()
        {
            if (textComponent == null)
            {
                return;
            }

            // In edit mode there is no LanguageManager; default to English for preview.
            GameLanguage language = Application.isPlaying
                ? LanguageManager.CurrentLanguageOrDefault
                : GameLanguage.English;

            string content = new LocalizedText { english = english, arabic = arabic }.Get(language);

            RtlTextHelper.SetText(textComponent, content, language, fontCategory);
        }
    }
}
