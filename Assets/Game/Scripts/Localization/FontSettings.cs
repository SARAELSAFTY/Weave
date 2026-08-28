using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Localization
{
    public enum FontAlignmentMode
    {
        [Tooltip("Preserve the original alignment set in the Editor / Prefab")]
        PreserveEditorAlignment = 0,

        [Tooltip("Force all text components to Middle Center alignment")]
        ForceMiddleCenter = 1
    }

    public enum TextFontCategory
    {
        Default = 0,
        Title = 1,
        SpeakerName = 2,
        MenuUI = 3,
        DialogueBody = 4,
        Choice = 5
    }

    [Serializable]
    public class CategoryFontPair
    {
        [Tooltip("If true, overrides the global default font for this category. If false, falls back to default.")]
        public bool overrideDefault = false;
        public TMP_FontAsset englishFont;
        public TMP_FontAsset arabicFont;

        public TMP_FontAsset GetFont(GameLanguage language)
        {
            if (language == GameLanguage.Arabic)
            {
                return arabicFont != null ? arabicFont : englishFont;
            }
            return englishFont != null ? englishFont : arabicFont;
        }
    }

    [CreateAssetMenu(fileName = "FontSettings", menuName = "Weave/Localization/Font Settings")]
    public class FontSettings : ScriptableObject
    {
        [Header("Global Default Fonts")]
        [SerializeField, Tooltip("Default font asset used for English text.")]
        private TMP_FontAsset englishFont;

        [SerializeField, Tooltip("Default font asset used for Arabic text.")]
        private TMP_FontAsset arabicFont;

        [Header("Alignment")]
        [SerializeField, Tooltip("Controls how text alignment is handled across languages.")]
        private FontAlignmentMode alignmentMode = FontAlignmentMode.PreserveEditorAlignment;

        [Header("Category Overrides (Optional)")]
        [SerializeField, Tooltip("Master switch: If disabled, all text uses the Global Default Fonts regardless of category.")]
        private bool enableCategoryOverrides = true;

        [SerializeField, Tooltip("Font override for Titles and Headers.")]
        private CategoryFontPair titleFont = new CategoryFontPair();

        [SerializeField, Tooltip("Font override for Character/Speaker Names.")]
        private CategoryFontPair speakerNameFont = new CategoryFontPair();

        [SerializeField, Tooltip("Font override for Menu & UI buttons.")]
        private CategoryFontPair menuUIFont = new CategoryFontPair();

        [SerializeField, Tooltip("Font override for Card story & dialogue body.")]
        private CategoryFontPair dialogueBodyFont = new CategoryFontPair();

        [SerializeField, Tooltip("Font override for Card Choice swipe labels.")]
        private CategoryFontPair choiceFont = new CategoryFontPair();

        private static FontSettings cachedDefault;

        public FontAlignmentMode AlignmentMode => alignmentMode;

        /// <summary>
        /// Returns the appropriate TMP_FontAsset for the specified language and optional category.
        /// </summary>
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

            // Fallback to Global Default Font
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

        /// <summary>
        /// Attempts to load the default FontSettings asset from any Resources folder.
        /// </summary>
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

        /// <summary>
        /// Registers all Arabic and English font assets as fallbacks so missing glyphs are automatically resolved.
        /// </summary>
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

            // Cross-link fallbacks directly on font assets so they resolve missing glyphs without warnings
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
