using UnityEngine;

namespace Game.Scripts.Localization
{
    public abstract class LocalizedDisplay : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            if (LanguageManager.Instance != null)
            {
                LanguageManager.Instance.LanguageChanged += Refresh;
            }

            Refresh();
        }

        protected virtual void OnDisable()
        {
            if (LanguageManager.HasInstance)
            {
                LanguageManager.Instance.LanguageChanged -= Refresh;
            }
        }

        protected void Refresh()
        {
            GameLanguage lang = LanguageManager.Instance != null
                ? LanguageManager.Instance.CurrentLanguage
                : GameLanguage.English;
            RefreshContent(lang);
        }

        protected abstract void RefreshContent(GameLanguage language);
    }
}
