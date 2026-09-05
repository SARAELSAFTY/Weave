using RTLTMPro;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Localization
{
    /// <summary>Utility for configuring RTL rendering, applying language-aware fonts, and shaping Arabic text.</summary>
    /// <remarks>
    /// Works with both RTLTextMeshPro components (which handle shaping internally) and plain TMP_Text
    /// components (which require manual glyph shaping via RTLSupport).
    /// </remarks>
    public static class RtlTextHelper
    {
        // Shared buffer avoids per-call allocation when shaping Arabic strings.
        private static readonly FastStringBuilder ShapeBuffer = new FastStringBuilder(RTLSupport.DefaultBufferSize);

        /// <summary>Configures an RTLTextMeshPro component with Arabic-appropriate defaults.</summary>
        /// <param name="text">The text component to configure; no-op if null or not an RTLTextMeshPro.</param>
        public static void Configure(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            if (text is RTLTextMeshPro rtl)
            {
                rtl.Farsi = false;
                rtl.PreserveNumbers = true;
                rtl.FixTags = true;
                rtl.ForceFix = false;
            }
        }

        /// <summary>Applies the correct font, alignment, and RTL settings for a given language and category.</summary>
        /// <param name="text">The text component to update.</param>
        /// <param name="language">Target language for font resolution.</param>
        /// <param name="category">Text category for optional font override lookup.</param>
        public static void Apply(TMP_Text text, GameLanguage language, TextFontCategory category = TextFontCategory.Default)
        {
            if (text == null)
            {
                return;
            }

            Configure(text);

            FontSettings settings = LanguageManager.Instance != null
                ? LanguageManager.Instance.FontSettings
                : FontSettings.LoadDefault();

            if (settings != null)
            {
                TMP_FontAsset font = settings.GetFont(language, category);
                if (font != null)
                {
                    text.font = font;
                }

                if (settings.AlignmentMode == FontAlignmentMode.ForceMiddleCenter)
                {
                    text.alignment = TextAlignmentOptions.Center;
                }
            }

            // Only RTLTextMeshPro handles RTL layout natively; disable the built-in flag on plain TMP_Text.
            if (!(text is RTLTextMeshPro))
            {
                text.isRightToLeftText = false;
            }
        }

        /// <summary>Shapes an Arabic string into display-order glyphs using RTLSupport.</summary>
        /// <param name="value">The raw Arabic text to shape.</param>
        /// <param name="language">If not Arabic, returns the value unchanged.</param>
        /// <returns>The shaped string ready for display on a non-RTL TMP_Text component.</returns>
        public static string Shape(string value, GameLanguage language)
        {
            if (language != GameLanguage.Arabic || string.IsNullOrEmpty(value))
            {
                return value ?? string.Empty;
            }

            ShapeBuffer.Clear();
            RTLSupport.FixRTL(value, ShapeBuffer, farsi: false, fixTextTags: true, preserveNumbers: true);
            ShapeBuffer.Reverse();
            return ShapeBuffer.ToString();
        }

        /// <summary>Sets text content on a TMP_Text component with appropriate font, RTL, and shaping applied.</summary>
        /// <param name="text">The target text component.</param>
        /// <param name="value">The text content to display.</param>
        /// <param name="language">Target language for font and shaping.</param>
        /// <param name="category">Text category for optional font override lookup.</param>
        public static void SetText(TMP_Text text, string value, GameLanguage language, TextFontCategory category = TextFontCategory.Default)
        {
            if (text == null)
            {
                return;
            }

            Apply(text, language, category);
            if (text is RTLTextMeshPro)
            {
                text.text = value ?? string.Empty;
            }
            else
            {
                text.text = Shape(value, language);
            }
        }

        /// <summary>Replaces a TMP_InputField's text component with an RTLTextMeshPro for proper Arabic input support.</summary>
        /// <remarks>
        /// Destroys the original TMP_Text and adds an RTLTextMeshPro in its place, copying over visual properties.
        /// In edit mode, only configures the existing component since DestroyImmediate is required outside play mode.
        /// </remarks>
        /// <param name="inputField">The input field whose text component should be upgraded.</param>
        public static void EnsureRtlInputText(TMP_InputField inputField)
        {
            if (inputField == null)
            {
                return;
            }

            TMP_Text current = inputField.textComponent;
            if (current == null)
            {
                return;
            }

            if (current is RTLTextMeshPro rtl)
            {
                Configure(rtl);
                return;
            }

            // Outside play mode we cannot safely swap components; just configure what exists.
            if (!Application.isPlaying)
            {
                Configure(current);
                return;
            }

            // Swap the plain TMP_Text for an RTLTextMeshPro, preserving visual settings.
            TMP_FontAsset font = current.font;
            float fontSize = current.fontSize;
            Color color = current.color;
            TextAlignmentOptions alignment = current.alignment;
            FontStyles fontStyle = current.fontStyle;
            float characterSpacing = current.characterSpacing;
            float lineSpacing = current.lineSpacing;
            float wordSpacing = current.wordSpacing;
            Vector2 margin = current.margin;
            bool wordWrapping = current.textWrappingMode == TextWrappingModes.Normal;
            GameObject host = current.gameObject;

            Object.DestroyImmediate(current);
            RTLTextMeshPro shaped = host.AddComponent<RTLTextMeshPro>();
            shaped.font = font;
            shaped.fontSize = fontSize;
            shaped.color = color;
            shaped.alignment = alignment;
            shaped.fontStyle = fontStyle;
            shaped.characterSpacing = characterSpacing;
            shaped.lineSpacing = lineSpacing;
            shaped.wordSpacing = wordSpacing;
            shaped.margin = margin;
            shaped.textWrappingMode = wordWrapping ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            inputField.textComponent = shaped;
            Configure(shaped);
        }
    }
}
