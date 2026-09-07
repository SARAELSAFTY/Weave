using System.Collections.Generic;
using Game.Scripts.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Scripts.UI
{
    /// <summary>Base class for screens built around a single localized button, such as the start and pause screens.</summary>
    /// <remarks>Subclasses can add an optional secondary button (the AI-settings entry) via <see cref="ConfigureOptionalButton"/>.</remarks>
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
        private readonly List<(Button button, UnityAction onClick)> optionalButtonBindings = new List<(Button, UnityAction)>();

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

        /// <summary>Wires an optional secondary button: adds the click listener and applies the localized label.</summary>
        /// <remarks>Wiring is skipped when the button is null; listeners registered here are removed again
        /// in <see cref="OnDestroy"/>, so subclasses overriding OnDestroy must call the base method.</remarks>
        /// <param name="button">Optional button; skipped when null.</param>
        /// <param name="labelText">Label on the button; resolved automatically from the button's children when null.</param>
        /// <param name="localizedLabel">Localized label shown on the button.</param>
        /// <param name="onClick">Click handler invoked when the button is pressed.</param>
        protected void ConfigureOptionalButton(Button button, TMP_Text labelText, LocalizedText localizedLabel, UnityAction onClick)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.AddListener(onClick);
            optionalButtonBindings.Add((button, onClick));

            if (labelText == null)
            {
                labelText = button.GetComponentInChildren<TMP_Text>();
            }

            if (labelText != null)
            {
                RtlTextHelper.SetText(labelText, localizedLabel.Get(LanguageManager.CurrentLanguageOrDefault), LanguageManager.CurrentLanguageOrDefault);
            }
        }

        /// <summary>Removes the listeners registered by <see cref="ConfigureOptionalButton"/>.</summary>
        protected virtual void OnDestroy()
        {
            foreach ((Button button, UnityAction onClick) in optionalButtonBindings)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(onClick);
                }
            }

            optionalButtonBindings.Clear();
        }

        /// <summary>Shows the screen and refreshes its localized content.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>Hides the screen.</summary>
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
