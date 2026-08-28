using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Scripts.UI
{
    public class DayDisplay : LocalizedDisplay
    {
        [SerializeField, FormerlySerializedAs("dayText")]
        private TMP_Text textComponent;

        [SerializeField, FormerlySerializedAs("format"), Tooltip("English format string for day text.")]
        private string formatEnglish = "Day {0}";

        [SerializeField, Tooltip("Arabic format string for day text.")]
        private string formatArabic = "اليوم {0}";

        private int _lastDay = 1;

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

        public void SetDay(int day)
        {
            _lastDay = day;
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
                format = language == GameLanguage.Arabic ? "اليوم {0}" : "Day {0}";
            }

            RtlTextHelper.SetText(textComponent, string.Format(format, _lastDay), language);
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
