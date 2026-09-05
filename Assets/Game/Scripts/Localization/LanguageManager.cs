using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.Localization
{
    /// <summary>Singleton that owns the active language and broadcasts language-change events.</summary>
    /// <remarks>
    /// Auto-bootstraps a DontDestroyOnLoad instance via RuntimeInitializeOnLoadMethod before any scene loads.
    /// If a scene-placed duplicate exists, it hands off its authored FontSettings to the live singleton
    /// and destroys only the component (not the host GameObject, which may carry other essential components).
    /// </remarks>
    public class LanguageManager : MonoBehaviour
    {
        /// <summary>The live singleton instance.</summary>
        public static LanguageManager Instance { get; private set; }

        /// <summary>True when the singleton has been created and not yet destroyed.</summary>
        public static bool HasInstance => Instance != null;

        private GameLanguage currentLanguage = GameLanguage.English;

        [Tooltip("Font mapping asset used to resolve fonts per language and text category.")]
        [SerializeField]
        private FontSettings fontSettings;

        /// <summary>The currently active language.</summary>
        public GameLanguage CurrentLanguage => currentLanguage;

        /// <summary>The active language, or English when no LanguageManager exists (edit mode or pre-bootstrap).</summary>
        public static GameLanguage CurrentLanguageOrDefault => Instance != null ? Instance.CurrentLanguage : GameLanguage.English;

        /// <summary>The loaded font settings, lazily falling back to Resources if not assigned in the Inspector.</summary>
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

        /// <summary>Raised after the active language changes.</summary>
        public event Action LanguageChanged;

        /// <summary>Creates the DontDestroyOnLoad singleton before any scene is loaded.</summary>
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

        // Handles the duplicate-instance case: transfers authored settings to the live singleton
        // and removes only this component so the host GameObject remains intact.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
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

        // Editor-only hotkey (F10) to toggle language without leaving play mode.
        private void Update()
        {
#if UNITY_EDITOR

            if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
            {
                SetLanguage(currentLanguage == GameLanguage.English ? GameLanguage.Arabic : GameLanguage.English);
            }
#endif
        }

        /// <summary>Sets the active language and raises <see cref="LanguageChanged"/> if it differs from the current value.</summary>
        /// <param name="language">The language to switch to.</param>
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
