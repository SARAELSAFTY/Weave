using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.UI
{
    /// <summary>Shows the current day number as a localized "Day N" label.</summary>
    public class DayDisplay : LocalizedDisplay
    {
        [Tooltip("Text component showing the day counter. Found automatically on this object or its children if left empty.")]
        [SerializeField, FormerlySerializedAs("dayText")]
        private TMP_Text textComponent;

        // Fallback formats matching the serialized field defaults, used when a serialized format was cleared.
        private const string DefaultFormatEnglish = "Day {0}";
        private const string DefaultFormatArabic = "اليوم {0}";

        [Tooltip("English format for the day label; {0} is replaced by the day number.")]
        [SerializeField, FormerlySerializedAs("format")]
        private string formatEnglish = DefaultFormatEnglish;

        [Tooltip("Arabic format for the day label; {0} is replaced by the day number.")]
        [SerializeField]
        private string formatArabic = DefaultFormatArabic;

        private int lastDay = 1;

        private void Reset()
        {
            ResolveTextComponent();
        }

        private void Awake()
        {
            ResolveTextComponent();

            if (InspectorValidation.RequireField(textComponent, nameof(textComponent), nameof(DayDisplay), this))
            {
                enabled = false;
                return;
            }

            RtlTextHelper.Configure(textComponent);
        }

        /// <summary>Sets the displayed day number and refreshes the label.</summary>
        /// <param name="day">Day number to display.</param>
        public void SetDay(int day)
        {
            lastDay = day;
            Refresh();
        }

        protected override void RefreshContent(GameLanguage language)
        {
            if (textComponent == null)
            {
                return;
            }

            string format = language == GameLanguage.Arabic ? formatArabic : formatEnglish;
            if (string.IsNullOrEmpty(format))
            {
                format = language == GameLanguage.Arabic ? DefaultFormatArabic : DefaultFormatEnglish;
            }

            RtlTextHelper.SetText(textComponent, string.Format(format, lastDay), language);
        }

        private void ResolveTextComponent()
        {
            if (textComponent != null)
            {
                return;
            }

            textComponent = GetComponent<TMP_Text>();
            if (textComponent == null)
            {
                textComponent = GetComponentInChildren<TMP_Text>();
            }
        }
    }
}
