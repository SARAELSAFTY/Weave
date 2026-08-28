using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>
    /// Shared behavior for views driven by a single localized button (pause menu, start screen).
    /// Subclasses keep their own serialized fields so existing scene references stay intact,
    /// and expose them through the abstract members below.
    /// </summary>
    public abstract class SingleLocalizedButtonView : LocalizedDisplay
    {
        protected abstract Button ViewButton { get; }
        protected abstract TMP_Text ViewLabel { get; set; }
        protected abstract LocalizedText DefaultLabelText { get; }
        protected abstract string ComponentTag { get; }
        protected abstract string ButtonFieldName { get; }

        private TMP_Text labelText;

        protected virtual void Awake()
        {
            if (InspectorValidation.RequireField(ViewButton, ButtonFieldName, ComponentTag, this))
            {
                enabled = false;
                return;
            }

            labelText = ViewLabel;
            if (labelText == null)
            {
                labelText = ViewButton.GetComponentInChildren<TMP_Text>();
                ViewLabel = labelText;
            }

            RtlTextHelper.Configure(labelText);
            ViewButton.onClick.AddListener(HandleButtonClicked);
            OnAwakeCompleted();
        }

        /// <summary>Hook for subclass-specific Awake work (e.g. hiding the view initially).</summary>
        protected virtual void OnAwakeCompleted()
        {
        }

        protected abstract void HandleButtonClicked();

        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        protected override void RefreshContent(GameLanguage language)
        {
            if (labelText == null)
            {
                return;
            }

            if (labelText.TryGetComponent<LocalizedLabel>(out LocalizedLabel label))
            {
                label.Refresh();
                return;
            }

            RtlTextHelper.SetText(labelText, DefaultLabelText.Get(language), language);
        }
    }
}
