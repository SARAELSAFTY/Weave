using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Base class for screens built around a single localized button, such as the start and pause screens.</summary>
    public abstract class SingleLocalizedButtonView : LocalizedDisplay
    {
        /// <summary>The button this view is built around.</summary>
        protected abstract Button ViewButton { get; }
        /// <summary>Text label on the button; resolved automatically from the button's children when null.</summary>
        protected abstract TMP_Text ViewLabel { get; set; }
        /// <summary>Localized label applied when the label has no LocalizedLabel component.</summary>
        protected abstract LocalizedText DefaultLabelText { get; }
        /// <summary>Component name used in validation log messages.</summary>
        protected abstract string ComponentTag { get; }
        /// <summary>Serialized field name of the button, used in validation log messages.</summary>
        protected abstract string ButtonFieldName { get; }

        private TMP_Text labelText;

        /// <summary>Validates the button, resolves the label, configures RTL support and wires the click listener.</summary>
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

        /// <summary>Hook called at the end of Awake for subclass setup.</summary>
        protected virtual void OnAwakeCompleted()
        {
        }

        /// <summary>Called when the button is clicked.</summary>
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

        /// <summary>Refreshes the label, preferring a LocalizedLabel component on it over the default text.</summary>
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
