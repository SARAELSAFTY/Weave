using RTLTMPro;
using TMPro;
using UnityEngine;

namespace Game.Scripts.Localization
{
    public static class RtlTextHelper
    {
        // Shared static buffer; intentionally non-reentrant/thread-unsafe to avoid allocations.
        private static readonly FastStringBuilder ShapeBuffer = new FastStringBuilder(RTLSupport.DefaultBufferSize);

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

            if (!(text is RTLTextMeshPro))
            {
                text.isRightToLeftText = false;
            }
        }

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

        /// <summary>
        /// Typed Arabic only gets contextual shaping on an RTLTextMeshPro component; a plain TMP text
        /// shows disconnected letters. Swaps the input field's text component for an RTLTextMeshPro at
        /// runtime. Never runs in edit mode - destroying the component there would edit the scene.
        /// </summary>
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

            if (!Application.isPlaying)
            {
                Configure(current);
                return;
            }

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
