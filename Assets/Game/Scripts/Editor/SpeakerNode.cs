using Game.Scripts.Definitions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    // Visual node representing one SpeakerData speaker in the graph canvas, allowing
    // viewing of display name, portrait, and LLM persona prompt.
    public class SpeakerNode : BaseNode
    {
        private static readonly Color SpeakerHeaderColor = new Color(0.28f, 0.15f, 0.38f);
        private static readonly Color SpeakerBorderColor = new Color(0.65f, 0.35f, 0.85f);
        private static readonly Color LlmAccent = new Color(0.85f, 0.6f, 1.0f);

        public SpeakerData Speaker { get; }
        private readonly CardGraphView parentGraphView;

        protected override Object TargetAsset => Speaker;
        protected override string TargetId => Speaker != null ? Speaker.AssetName : "Null Speaker";

        public SpeakerNode(SpeakerData speaker, CardGraphView parentGraphView)
        {
            Speaker = speaker;
            this.parentGraphView = parentGraphView;
            bool hasPersona = speaker != null && !string.IsNullOrWhiteSpace(speaker.llmPersonaPrompt);

            InitializeNode(TargetId);
            ApplyNodeChrome();

            if (speaker != null)
            {
                titleContainer.Add(BuildBadges(speaker, hasPersona));
                extensionContainer.Add(BuildBody(speaker, hasPersona));
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        private void ApplyNodeChrome()
        {
            style.width = 240;
            style.maxWidth = 240;
            style.backgroundColor = new StyleColor(new Color(0.14f, 0.13f, 0.17f));
            ApplyBaseChrome(SpeakerHeaderColor, SpeakerBorderColor, 2f, 32f);
        }

        private static VisualElement BuildBadges(SpeakerData speaker, bool hasPersona)
        {
            VisualElement badges = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            if (hasPersona)
            {
                badges.Add(MakeBadge("PERSONA", LlmAccent, new Color(0.30f, 0.12f, 0.45f), "Has LLM persona prompt authored"));
            }

            return badges;
        }

        private static Label MakeBadge(string text, Color foreground, Color background, string tooltip)
        {
            Label label = new Label(text);
            label.style.fontSize = 9;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new StyleColor(foreground);
            label.style.backgroundColor = new StyleColor(background);
            label.style.paddingLeft = 5;
            label.style.paddingRight = 5;
            label.style.paddingTop = 2;
            label.style.paddingBottom = 2;
            label.style.marginRight = 6;
            label.style.borderTopLeftRadius = 3;
            label.style.borderTopRightRadius = 3;
            label.style.borderBottomLeftRadius = 3;
            label.style.borderBottomRightRadius = 3;
            label.tooltip = tooltip;
            return label;
        }

        private VisualElement BuildBody(SpeakerData speaker, bool hasPersona)
        {
            VisualElement body = new VisualElement
            {
                style =
                {
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    backgroundColor = new StyleColor(new Color(0.16f, 0.14f, 0.19f)),
                    minWidth = 160,
                    maxWidth = 224
                }
            };

            Label nameValue = new Label(speaker.AssetName);
            nameValue.style.fontSize = 11;
            nameValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameValue.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f));
            nameValue.style.marginBottom = 2;
            body.Add(nameValue);

            // Show player-facing display name only when it differs from the asset name
            if (!string.IsNullOrWhiteSpace(speaker.displayName) &&
                speaker.displayName.Trim() != speaker.AssetName)
            {
                Label displayLabel = new Label($"UI: {speaker.displayName.Trim()}");
                displayLabel.style.fontSize = 9;
                displayLabel.style.color = new StyleColor(new Color(0.6f, 0.7f, 0.85f));
                displayLabel.style.marginBottom = 4;
                body.Add(displayLabel);
            }

            if (hasPersona)
            {
                string personaText = string.IsNullOrEmpty(speaker.llmPersonaPrompt) ? "(No Persona Prompt)" : speaker.llmPersonaPrompt;
                Label personaValue = new Label(personaText);
                personaValue.style.fontSize = 9;
                personaValue.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.65f));
                personaValue.style.whiteSpace = WhiteSpace.Normal;
                personaValue.style.unityFontStyleAndWeight = FontStyle.Italic;
                personaValue.style.maxHeight = 60;
                body.Add(personaValue);
            }

            if (speaker.portrait != null)
            {
                Image portraitImage = new Image
                {
                    sprite = speaker.portrait,
                    scaleMode = ScaleMode.ScaleToFit
                };
                portraitImage.style.width = 64;
                portraitImage.style.height = 64;
                portraitImage.style.marginTop = 8;
                portraitImage.style.alignSelf = Align.Center;
                portraitImage.style.borderTopWidth = 1;
                portraitImage.style.borderBottomWidth = 1;
                portraitImage.style.borderLeftWidth = 1;
                portraitImage.style.borderRightWidth = 1;
                portraitImage.style.borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                portraitImage.style.borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                portraitImage.style.borderLeftColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                portraitImage.style.borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f));
                body.Add(portraitImage);
            }

            return body;
        }
    }
}
