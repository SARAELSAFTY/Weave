using UnityEngine;

namespace Game.Scripts.Localization
{
    /// <summary>Base class for UI components that auto-refresh their content when the active language changes.</summary>
    /// <remarks>Subscribes to LanguageManager.LanguageChanged on enable and unsubscribes on disable.</remarks>
    public abstract class LocalizedDisplay : MonoBehaviour
    {
        /// <summary>Subscribes to language-change events and performs an initial refresh.</summary>
        protected virtual void OnEnable()
        {
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.LanguageChanged += Refresh;
            }

            Refresh();
        }

        /// <summary>Unsubscribes from language-change events.</summary>
        protected virtual void OnDisable()
        {
            if (LanguageManager.HasInstance)
            {
                LanguageManager.Instance.LanguageChanged -= Refresh;
            }
        }

        /// <summary>Reads the current language from LanguageManager and delegates to <see cref="RefreshContent"/>.</summary>
        protected void Refresh()
        {
            RefreshContent(LanguageManager.CurrentLanguageOrDefault);
        }

        /// <summary>Updates the display content for the given language.</summary>
        /// <param name="language">The language to render.</param>
        protected abstract void RefreshContent(GameLanguage language);
    }
}
