using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Localization
{
    /// <summary>Controls how font alignment is applied when resolving fonts from <see cref="FontSettings"/>.</summary>
    public enum FontAlignmentMode
    {
        PreserveEditorAlignment = 0,

        ForceMiddleCenter = 1
    }

    /// <summary>Logical text categories that can each map to a distinct font override.</summary>
    public enum TextFontCategory
    {
        Default = 0,
        Title = 1,
        SpeakerName = 2,
        MenuUI = 3,
        DialogueBody = 4,
        Choice = 5
    }

    /// <summary>Pairs an English and Arabic font for a single text category, with an optional override flag.</summary>
    [Serializable]
    public class CategoryFontPair
    {
        /// <summary>When true, this category's fonts take precedence over the global defaults.</summary>
        [Tooltip("When enabled, this category's fonts take precedence over the global defaults.")]
        public bool overrideDefault = false;

        /// <summary>Font used for English text in this category.</summary>
        [Tooltip("Font used for English text in this category.")]
        public TMP_FontAsset englishFont;

        /// <summary>Font used for Arabic text in this category.</summary>
        [Tooltip("Font used for Arabic text in this category.")]
        public TMP_FontAsset arabicFont;

        /// <summary>Returns the font for the given language, falling back to the other language's font if needed.</summary>
        /// <param name="language">Target language.</param>
        public TMP_FontAsset GetFont(GameLanguage language)
        {
            if (language == GameLanguage.Arabic)
            {
                return arabicFont != null ? arabicFont : englishFont;
            }
            return englishFont != null ? englishFont : arabicFont;
        }
    }

    /// <summary>ScriptableObject that maps language and text category to TMP fonts, loaded from Resources at runtime.</summary>
    /// <remarks>
    /// Searched in order: "FontSettings", "Fonts/FontSettings", then any FontSettings asset found via LoadAll.
    /// Also registers all assigned fonts as TMP fallback assets so mixed-script text renders correctly.
    /// </remarks>
    [CreateAssetMenu(fileName = "FontSettings", menuName = "Weave/Localization/Font Settings")]
    public class FontSettings : ScriptableObject
    {
        [Tooltip("Default font for English text when no category override applies.")]
        [SerializeField]
        private TMP_FontAsset englishFont;

        [Tooltip("Default font for Arabic text when no category override applies.")]
        [SerializeField]
        private TMP_FontAsset arabicFont;

        [Tooltip("Whether to force middle-center alignment on all resolved text components.")]
        [SerializeField]
        private FontAlignmentMode alignmentMode = FontAlignmentMode.PreserveEditorAlignment;

        [Tooltip("Enable per-category font overrides below.")]
        [SerializeField]
        private bool enableCategoryOverrides = true;

        [Header("Category Overrides")]
        [Tooltip("Font override for title text, used when its Override toggle is on.")]
        [SerializeField]
        private CategoryFontPair titleFont = new CategoryFontPair();

        [Tooltip("Font override for speaker name labels, used when its Override toggle is on.")]
        [SerializeField]
        private CategoryFontPair speakerNameFont = new CategoryFontPair();

        [Tooltip("Font override for menu and button text, used when its Override toggle is on.")]
        [SerializeField]
        private CategoryFontPair menuUIFont = new CategoryFontPair();

        [Tooltip("Font override for dialogue body text, used when its Override toggle is on.")]
        [SerializeField]
        private CategoryFontPair dialogueBodyFont = new CategoryFontPair();

        [Tooltip("Font override for choice button text, used when its Override toggle is on.")]
        [SerializeField]
        private CategoryFontPair choiceFont = new CategoryFontPair();

        private static FontSettings cachedDefault;

        /// <summary>The configured alignment mode applied to all resolved text components.</summary>
        public FontAlignmentMode AlignmentMode => alignmentMode;

        /// <summary>Resolves the appropriate font for a language and optional text category.</summary>
        /// <param name="language">Target language.</param>
        /// <param name="category">Text category; use Default to skip overrides.</param>
        /// <returns>The best matching font, or null if none are assigned.</returns>
        public TMP_FontAsset GetFont(GameLanguage language, TextFontCategory category = TextFontCategory.Default)
        {
            if (enableCategoryOverrides && category != TextFontCategory.Default)
            {
                CategoryFontPair pair = GetCategoryPair(category);
                if (pair != null && pair.overrideDefault)
                {
                    TMP_FontAsset catFont = pair.GetFont(language);
                    if (catFont != null)
                    {
                        return catFont;
                    }
                }
            }

            switch (language)
            {
                case GameLanguage.Arabic:
                    return arabicFont != null ? arabicFont : englishFont;
                case GameLanguage.English:
                default:
                    return englishFont != null ? englishFont : arabicFont;
            }
        }

        private CategoryFontPair GetCategoryPair(TextFontCategory category)
        {
            switch (category)
            {
                case TextFontCategory.Title: return titleFont;
                case TextFontCategory.SpeakerName: return speakerNameFont;
                case TextFontCategory.MenuUI: return menuUIFont;
                case TextFontCategory.DialogueBody: return dialogueBodyFont;
                case TextFontCategory.Choice: return choiceFont;
                default: return null;
            }
        }

        /// <summary>Loads the singleton FontSettings asset from Resources, caching the result.</summary>
        /// <returns>The loaded instance, or null if no asset is found.</returns>
        public static FontSettings LoadDefault()
        {
            if (cachedDefault != null)
            {
                return cachedDefault;
            }

            cachedDefault = Resources.Load<FontSettings>("FontSettings");
            if (cachedDefault == null)
            {
                cachedDefault = Resources.Load<FontSettings>("Fonts/FontSettings");
            }

            // Last resort: grab any FontSettings asset in Resources root.
            if (cachedDefault == null)
            {
                FontSettings[] all = Resources.LoadAll<FontSettings>("");
                if (all != null && all.Length > 0)
                {
                    cachedDefault = all[0];
                }
            }

            return cachedDefault;
        }

        /// <summary>Registers all assigned fonts as TMP global fallbacks and cross-links English/Arabic pairs.</summary>
        /// <remarks>
        /// Ensures glyphs missing from one font fall through to the other language's font,
        /// preventing blank characters when mixed-script text is rendered.
        /// </remarks>
        public void EnsureFallbackRegistered()
        {
            if (TMP_Settings.fallbackFontAssets != null)
            {
                RegisterFallback(arabicFont);
                RegisterFallback(englishFont);

                if (enableCategoryOverrides)
                {
                    RegisterFallback(titleFont?.arabicFont);
                    RegisterFallback(titleFont?.englishFont);
                    RegisterFallback(speakerNameFont?.arabicFont);
                    RegisterFallback(speakerNameFont?.englishFont);
                    RegisterFallback(menuUIFont?.arabicFont);
                    RegisterFallback(menuUIFont?.englishFont);
                    RegisterFallback(dialogueBodyFont?.arabicFont);
                    RegisterFallback(dialogueBodyFont?.englishFont);
                    RegisterFallback(choiceFont?.arabicFont);
                    RegisterFallback(choiceFont?.englishFont);
                }
            }

            // Cross-link so each language's font falls back to the other for missing glyphs.
            LinkFallback(arabicFont, englishFont);
            LinkFallback(englishFont, arabicFont);
        }

        private void RegisterFallback(TMP_FontAsset font)
        {
            if (font != null && TMP_Settings.fallbackFontAssets != null && !TMP_Settings.fallbackFontAssets.Contains(font))
            {
                TMP_Settings.fallbackFontAssets.Add(font);
            }
        }

        private void LinkFallback(TMP_FontAsset mainFont, TMP_FontAsset fallbackFont)
        {
            if (mainFont == null || fallbackFont == null || mainFont == fallbackFont)
            {
                return;
            }

            if (mainFont.fallbackFontAssetTable == null)
            {
                mainFont.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            if (!mainFont.fallbackFontAssetTable.Contains(fallbackFont))
            {
                mainFont.fallbackFontAssetTable.Add(fallbackFont);
            }
        }
    }
}
