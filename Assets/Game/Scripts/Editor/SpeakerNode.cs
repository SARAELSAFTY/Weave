using Game.Scripts.Definitions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    public class SpeakerNode : BaseNode
    {
        private static readonly Color SpeakerHeaderColor = new Color(0.28f, 0.15f, 0.38f);
        private static readonly Color SpeakerBorderColor = new Color(0.65f, 0.35f, 0.85f);
        private static readonly Color LlmAccent = new Color(0.85f, 0.6f, 1.0f);

        public SpeakerData Speaker { get; }
        private readonly CardGraphView parentGraphView;

        protected override Object TargetAsset => Speaker;
        protected override string TargetId => Speaker != null ? Speaker.DisplayName : "Null Speaker";

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

            AddIdentityLabels(body, speaker.DisplayName, speaker.assetName);

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

            AddPreviewImage(body, speaker.portrait, 64f, withBorder: true);

            return body;
        }
    }
}
