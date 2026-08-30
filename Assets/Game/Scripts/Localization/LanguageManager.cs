using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Localization
{
    public class LanguageManager : MonoBehaviour
    {
        public static LanguageManager Instance { get; private set; }
        public static bool HasInstance => Instance != null;

        private GameLanguage currentLanguage = GameLanguage.English;

        [SerializeField, Tooltip("Active Font Settings for language font switching. Auto-loaded if not assigned.")]
        private FontSettings fontSettings;

        public GameLanguage CurrentLanguage => currentLanguage;

        public FontSettings FontSettings
        {
            get
            {
                if (fontSettings == null)
                {
                    fontSettings = FontSettings.LoadDefault();
                }
                return fontSettings;
            }
        }

        public event Action LanguageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            GameObject go = new GameObject("[LanguageManager]");
            DontDestroyOnLoad(go);
            go.AddComponent<LanguageManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // A manager already exists (auto-created before scene load). Hand over any authored
                // FontSettings so the Inspector assignment is honoured, then remove only THIS component.
                // Never Destroy(gameObject) here - this component may sit on an essential object
                // (e.g. GameManager) and destroying the host would break the game.
                if (fontSettings != null)
                {
                    Instance.fontSettings = fontSettings;
                    Instance.fontSettings.EnsureFallbackRegistered();
                }

                Destroy(this);
                return;
            }

            Instance = this;

            if (fontSettings == null)
            {
                fontSettings = FontSettings.LoadDefault();
            }

            if (fontSettings != null)
            {
                fontSettings.EnsureFallbackRegistered();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            // Dev-only hotkey for flipping between English and Arabic in the Editor.
            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                SetLanguage(currentLanguage == GameLanguage.English ? GameLanguage.Arabic : GameLanguage.English);
            }
#endif
        }

        public void SetLanguage(GameLanguage language)
        {
            if (currentLanguage == language)
            {
                return;
            }

            currentLanguage = language;
            LanguageChanged?.Invoke();
        }
    }
}
