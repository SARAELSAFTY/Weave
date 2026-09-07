using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    /// <summary>Graph node representing a ResourceData asset, displaying thresholds, visual/LLM speakers, and inline EN/AR fallback text.</summary>
    public class ResourceNode : BaseNode
    {
        private static readonly Color ResourceHeaderColor = new Color(0.10f, 0.28f, 0.22f);
        private static readonly Color ResourceBorderColor = new Color(0.25f, 0.75f, 0.55f);
        private static readonly Color CollapseAccent = new Color(1f, 0.38f, 0.38f);
        private static readonly Color WarningAccent = new Color(1f, 0.78f, 0.2f);
        private static readonly Color SpeakerAccent = new Color(0.6f, 0.8f, 1f);

        /// <summary>The resource asset this node represents.</summary>
        public ResourceData Data { get; }

        protected override Object TargetAsset => Data;
        protected override string TargetId => Data != null ? Data.AssetName : "Null Resource";

        protected override string PingActionLabel => "Ping Resource Asset";
        protected override string OpenActionLabel => "Open Resource Asset";

        /// <summary>Builds the node body with resource thresholds, speaker selectors, and inline fallback collapse text.</summary>
        /// <param name="data">Resource asset to display; may be null for a placeholder node.</param>
        public ResourceNode(ResourceData data)
        {
            Data = data;

            InitializeNode(TargetId);
            ApplyNodeChrome();

            if (data != null)
            {
                extensionContainer.Add(BuildBody(data));
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        private void ApplyNodeChrome()
        {
            style.width = 240;
            style.maxWidth = 240;
            style.backgroundColor = new StyleColor(new Color(0.13f, 0.16f, 0.15f));
            ApplyBaseChrome(ResourceHeaderColor, ResourceBorderColor, 2f, 32f);
        }

        private static VisualElement BuildBody(ResourceData data)
        {
            VisualElement body = new VisualElement
            {
                style =
                {
                    paddingTop = 6,
                    paddingBottom = 8,
                    paddingLeft = 8,
                    paddingRight = 8,
                    backgroundColor = new StyleColor(new Color(0.15f, 0.18f, 0.17f)),
                    minWidth = 200
                }
            };

            AddIdentityLabels(body, data.GetDisplayName(GameLanguage.English), data.assetName);

            Label startValue = new Label($"Start: {data.defaultStartingValue}");
            startValue.style.fontSize = 10;
            startValue.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            body.Add(startValue);

            Label warningLabel = new Label($"⚠  Warning at {data.warningThresholdPercent}%");
            warningLabel.style.fontSize = 9;
            warningLabel.style.color = new StyleColor(WarningAccent);
            warningLabel.style.marginTop = 2;
            body.Add(warningLabel);

            Label collapseLabel = new Label($"✕  Collapse at ≤ {data.collapseThreshold}");
            collapseLabel.style.fontSize = 9;
            collapseLabel.style.color = new StyleColor(CollapseAccent);
            collapseLabel.style.marginTop = 1;
            body.Add(collapseLabel);

            AddPreviewImage(body, data.icon, 36f, withBorder: false);

            body.Add(CreateSeparator());

            Label speakersHeader = new Label("Speakers");
            speakersHeader.style.fontSize = 10;
            speakersHeader.style.color = new StyleColor(SpeakerAccent);
            speakersHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            speakersHeader.style.marginBottom = 2;
            body.Add(speakersHeader);

            Label speakerLabel = new Label("Speaker (Portrait & LLM Voice)");
            speakerLabel.style.fontSize = 9;
            speakerLabel.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.75f));
            body.Add(speakerLabel);

            ObjectField speakerField = new ObjectField
            {
                objectType = typeof(SpeakerData),
                allowSceneObjects = false,
                value = data.speaker
            };
            speakerField.style.fontSize = 10;
            speakerField.style.marginBottom = 4;
            speakerField.RegisterValueChangedCallback(evt =>
            {
                SpeakerData newSpeaker = evt.newValue as SpeakerData;
                if (data.speaker != newSpeaker)
                {
                    Undo.RecordObject(data, "Assign Speaker");
                    data.speaker = newSpeaker;
                    EditorUtility.SetDirty(data);
                }
            });
            body.Add(speakerField);

            body.Add(CreateSeparator());

            Label fallbackHeader = new Label("Collapse Fallback Text");
            fallbackHeader.style.fontSize = 10;
            fallbackHeader.style.color = new StyleColor(CollapseAccent);
            fallbackHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            fallbackHeader.style.marginBottom = 1;
            body.Add(fallbackHeader);

            Label fallbackHint = new Label("Used if LLM epilogue fails or is offline");
            fallbackHint.style.fontSize = 8;
            fallbackHint.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));
            fallbackHint.style.marginBottom = 4;
            body.Add(fallbackHint);

            Label enLabel = new Label("English");
            enLabel.style.fontSize = 9;
            enLabel.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.75f));
            body.Add(enLabel);

            TextField enField = new TextField
            {
                multiline = true,
                isDelayed = true,
                value = data.collapseEndingFallbackEnglish ?? string.Empty
            };
            enField.style.fontSize = 10;
            enField.style.height = 40;
            enField.style.whiteSpace = WhiteSpace.Normal;
            enField.style.marginBottom = 4;
            enField.RegisterValueChangedCallback(evt =>
            {
                if (data.collapseEndingFallbackEnglish != evt.newValue)
                {
                    Undo.RecordObject(data, "Change Fallback Collapse English");
                    data.collapseEndingFallbackEnglish = evt.newValue;
                    EditorUtility.SetDirty(data);
                }
            });
            body.Add(enField);

            Label arLabel = new Label("Arabic");
            arLabel.style.fontSize = 9;
            arLabel.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.75f));
            body.Add(arLabel);

            TextField arField = new TextField
            {
                multiline = true,
                isDelayed = true,
                value = data.collapseEndingFallbackArabic ?? string.Empty
            };
            arField.style.fontSize = 10;
            arField.style.height = 40;
            arField.style.whiteSpace = WhiteSpace.Normal;
            arField.RegisterValueChangedCallback(evt =>
            {
                if (data.collapseEndingFallbackArabic != evt.newValue)
                {
                    Undo.RecordObject(data, "Change Fallback Collapse Arabic");
                    data.collapseEndingFallbackArabic = evt.newValue;
                    EditorUtility.SetDirty(data);
                }
            });
            body.Add(arField);

            return body;
        }

        private static VisualElement CreateSeparator()
        {
            VisualElement sep = new VisualElement();
            sep.style.height = 1;
            sep.style.backgroundColor = new StyleColor(new Color(0.28f, 0.28f, 0.28f));
            sep.style.marginTop = 6;
            sep.style.marginBottom = 5;
            return sep;
        }
    }
}
