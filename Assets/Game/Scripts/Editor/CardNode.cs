using System.Collections.Generic;
using Game.Scripts.Definitions;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    public class CardNode : BaseNode
    {
        private const int DescriptionPreviewLength = 50;
        private const int ChoicePreviewLength = 22;

        private static readonly Color LeftAccent = new Color(0f, 0.9f, 1f);
        private static readonly Color RightAccent = new Color(1f, 0.55f, 0f);
        private static readonly Color ContinueAccent = new Color(0.75f, 0.45f, 1f);
        private static readonly Color ResourceAccent = new Color(0.4f, 1.0f, 0.4f);
        private static readonly Color NegativeResourceAccent = new Color(1.0f, 0.4f, 0.4f);
        private static readonly Color StartBorder = new Color(1.0f, 0.84f, 0.0f);
        private static readonly Color LlmBorder = new Color(0.65f, 0.35f, 1f);
        private static readonly Color PetitionBorder = new Color(0.1f, 0.85f, 0.75f);
        private static readonly Color DefaultBorder = new Color(0.28f, 0.30f, 0.35f);

        public CardData Card { get; }
        public Port InputPort { get; private set; }
        public Port LeftPort { get; private set; }
        public Port RightPort { get; private set; }
        public Port ContinuePort { get; private set; }

        private readonly CardGraphView parentGraphView;
        private bool isStartCardCached;

        protected override Object TargetAsset => Card;
        protected override string TargetId => Card != null ? Card.DisplayName : "Null Card";

        public CardNode(CardData card, bool isStartCard, CardGraphView parentGraphView)
        {
            Card = card;
            this.parentGraphView = parentGraphView;
            this.isStartCardCached = isStartCard;

            InitializeNode(TargetId);
            ApplyNodeChrome(isStartCard, card.isLlmReactionCard, card.isPetitionCard);
            titleContainer.Add(BuildBadges(card, isStartCard));

            InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "In";
            InputPort.portColor = new Color(0.85f, 0.85f, 0.85f);
            inputContainer.Add(InputPort);

            extensionContainer.Add(BuildBody(card));
            extensionContainer.Add(BuildChoices(card));

            RefreshExpandedState();
            RefreshPorts();
        }

        private void ApplyNodeChrome(bool isStartCard, bool isLlmCard, bool isPetitionCard = false)
        {
            style.width = 260;
            style.maxWidth = 260;
            style.backgroundColor = new StyleColor(new Color(0.14f, 0.15f, 0.18f));

            bool highlightBorder = isStartCard || isLlmCard || isPetitionCard;
            Color borderColor = isStartCard ? StartBorder : isLlmCard ? LlmBorder : isPetitionCard ? PetitionBorder : DefaultBorder;
            Color headerColor = isStartCard ? new Color(0.28f, 0.24f, 0.05f)
                : isLlmCard ? new Color(0.22f, 0.12f, 0.32f)
                : isPetitionCard ? new Color(0.05f, 0.22f, 0.20f)
                : new Color(0.18f, 0.19f, 0.22f);

            ApplyBaseChrome(headerColor, borderColor, highlightBorder ? 2f : 1f, 34f);
        }

        private VisualElement BuildBadges(CardData card, bool isStartCard)
        {
            VisualElement badges = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            if (isStartCard)
            {
                badges.Add(MakeBadge("START",
                    new Color(1.0f, 0.85f, 0.1f), new Color(0.45f, 0.38f, 0.0f),
                    "Starting Card"));
            }

            if (card.isLlmReactionCard)
            {
                badges.Add(MakeBadge("LLM",
                    new Color(0.85f, 0.65f, 1f), new Color(0.30f, 0.12f, 0.45f),
                    "LLM reaction card"));
            }

            if (card.isPetitionCard)
            {
                badges.Add(MakeBadge("PETITION",
                    new Color(0.2f, 1f, 0.9f), new Color(0.03f, 0.28f, 0.25f),
                    "Petition card — player types free-form commands resolved by AI."));
            }

            if (card.speaker == null)
            {
                badges.Add(MakeBadge("No Speaker",
                    new Color(1.0f, 0.75f, 0.2f), new Color(0.35f, 0.22f, 0.0f),
                    "Every card must have a speaker assigned — the run will fail to start without one."));
            }
            else if ((card.isLlmReactionCard || card.isPetitionCard) && string.IsNullOrWhiteSpace(card.speaker.llmPersonaPrompt))
            {
                badges.Add(MakeBadge("No Speaker Persona",
                    new Color(1.0f, 0.75f, 0.2f), new Color(0.35f, 0.22f, 0.0f),
                    $"Speaker '{card.speaker.DisplayName}' has no Persona Prompt authored, so this reaction will have no persona."));
            }

            if (card.IsEnding)
            {
                badges.Add(MakeBadge("ENDING",
                    new Color(1.0f, 0.45f, 0.45f), new Color(0.45f, 0.10f, 0.10f),
                    "This card ends the game because it has no next card choice links."));
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

        private VisualElement BuildBody(CardData card)
        {
            VisualElement body = new VisualElement
            {
                style =
                {
                    paddingTop = 6,
                    paddingBottom = 6,
                    paddingLeft = 8,
                    paddingRight = 8,
                    backgroundColor = new StyleColor(new Color(0.16f, 0.17f, 0.20f))
                }
            };

            body.Add(BuildMetaRow(card));

            string previewText = card.isLlmReactionCard
                ? Truncate(card.llmPromptSeed, DescriptionPreviewLength, "(No prompt seed)")
                : card.isPetitionCard
                    ? Truncate(card.petitionSeedPrompt, DescriptionPreviewLength, "(No petition seed)")
                    : Truncate(card.description, DescriptionPreviewLength, "(No description)");

            Label previewLabel = new Label(previewText);
            previewLabel.style.fontSize = 11;
            previewLabel.style.color = new StyleColor(new Color(0.82f, 0.84f, 0.88f));
            previewLabel.style.whiteSpace = WhiteSpace.Normal;
            body.Add(previewLabel);

            return body;
        }

        private VisualElement BuildMetaRow(CardData card)
        {
            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center,
                    marginBottom = 4
                }
            };

            VisualElement speakerContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            Label speakerLabel = new Label("Speaker:");
            speakerLabel.style.fontSize = 10;
            speakerLabel.style.color = new StyleColor(new Color(0.7f, 0.75f, 0.9f));
            speakerLabel.style.marginRight = 4;
            speakerContainer.Add(speakerLabel);

            NarrativeDatabase db = parentGraphView?.Database;
            List<SpeakerData> speakerList = db?.speakers ?? new List<SpeakerData>();

            List<string> choices = new List<string> { "(None)" };
            int selectedIndex = 0;
            for (int i = 0; i < speakerList.Count; i++)
            {
                SpeakerData s = speakerList[i];
                if (s == null) continue;
                choices.Add(s.DisplayName);
                if (card.speaker == s)
                {
                    selectedIndex = choices.Count - 1;
                }
            }

            PopupField<string> speakerPopup = new PopupField<string>(choices, selectedIndex);
            speakerPopup.style.fontSize = 10;
            speakerPopup.style.height = 18;
            speakerPopup.style.maxWidth = 110;
            speakerPopup.RegisterValueChangedCallback(evt =>
            {
                int newIdx = choices.IndexOf(evt.newValue);
                SpeakerData newSpeaker = (newIdx > 0 && newIdx - 1 < speakerList.Count) ? speakerList[newIdx - 1] : null;
                if (card.speaker != newSpeaker)
                {
                    Undo.RecordObject(card, "Assign Card Speaker");
                    card.speaker = newSpeaker;
                    EditorUtility.SetDirty(card);
                    parentGraphView?.Populate(parentGraphView.Database);
                }
            });

            speakerContainer.Add(speakerPopup);
            row.Add(speakerContainer);

            if (card.dayAdvance > 0)
            {
                Label dayLabel = new Label($"+{card.dayAdvance} Day{(card.dayAdvance > 1 ? "s" : "")}");
                dayLabel.style.fontSize = 10;
                dayLabel.style.color = new StyleColor(new Color(0.5f, 0.85f, 0.5f));
                row.Add(dayLabel);
            }

            return row;
        }

        private VisualElement BuildChoices(CardData card)
        {
            VisualElement choices = new VisualElement
            {
                style =
                {
                    paddingTop = 4,
                    paddingBottom = 4,
                    backgroundColor = new StyleColor(new Color(0.13f, 0.14f, 0.16f))
                }
            };

            if (card.UsesContinueExit)
            {
                choices.Add(CreateContinueRow(out Port continuePort));
                ContinuePort = continuePort;
                return choices;
            }

            choices.Add(CreateChoiceRow("L", card.leftChoiceText, LeftAccent, card.leftResourceChange, out Port leftPort));
            LeftPort = leftPort;

            choices.Add(CreateChoiceRow("R", card.rightChoiceText, RightAccent, card.rightResourceChange, out Port rightPort));
            RightPort = rightPort;

            return choices;
        }

        private VisualElement CreateContinueRow(out Port outputPort)
        {
            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,
                    paddingLeft = 8,
                    paddingRight = 4,
                    paddingTop = 3,
                    paddingBottom = 3
                }
            };

            Label continueLabel = new Label("Continue");
            continueLabel.style.fontSize = 11;
            continueLabel.style.color = new StyleColor(ContinueAccent);
            row.Add(continueLabel);

            outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputPort.portName = "Continue";
            outputPort.portColor = ContinueAccent;
            row.Add(outputPort);

            return row;
        }

        private VisualElement CreateChoiceRow(string prefix, string choiceText, Color accentColor, ResourceChange resourceChange, out Port outputPort)
        {
            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    justifyContent = Justify.SpaceBetween,
                    paddingLeft = 8,
                    paddingRight = 4,
                    paddingTop = 3,
                    paddingBottom = 3
                }
            };

            VisualElement leftSide = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexShrink = 1
                }
            };

            Label choiceLabel = new Label($"{prefix}: {Truncate(choiceText, ChoicePreviewLength, "(Choice text)")}");
            choiceLabel.style.fontSize = 11;
            choiceLabel.style.color = new StyleColor(accentColor);
            leftSide.Add(choiceLabel);

            if (resourceChange.values != null && resourceChange.values.Length > 0)
            {
                VisualElement resourceContainer = new VisualElement();
                resourceContainer.style.flexDirection = FlexDirection.Row;
                resourceContainer.style.marginLeft = 4;

                foreach (var rv in resourceChange.values)
                {
                    if (rv.resource == null) continue;

                    string name = rv.resource.DisplayName;
                    Label resLabel = new Label($"{(rv.value >= 0 ? "+" : "")}{rv.value}{name[0]}");
                    resLabel.style.fontSize = 9;
                    resLabel.style.color = new StyleColor(rv.value >= 0 ? ResourceAccent : NegativeResourceAccent);
                    resLabel.style.marginLeft = 2;
                    resLabel.tooltip = $"{name}: {rv.value}";
                    resourceContainer.Add(resLabel);
                }
                leftSide.Add(resourceContainer);
            }

            row.Add(leftSide);

            outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            outputPort.portName = prefix;
            outputPort.portColor = accentColor;
            row.Add(outputPort);

            return row;
        }

        private static string Truncate(string text, int maxLength, string emptyFallback)
        {
            if (string.IsNullOrEmpty(text))
            {
                return emptyFallback;
            }

            return text.Length > maxLength ? text.Substring(0, maxLength) + "…" : text;
        }

        protected override void AddCustomContextMenuActions(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Set as Starting Card", _ => parentGraphView.SetStartingCard(Card),
                isStartCardCached ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            // Empty seeds fall back to LlmPromptTemplates defaults; do not write hardcoded seeds here.

            evt.menu.AppendAction("Is LLM Reaction Card", _ =>
            {
                Undo.RecordObject(Card, "Toggle LLM Reaction");
                Card.isLlmReactionCard = !Card.isLlmReactionCard;
                if (Card.isLlmReactionCard)
                {
                    Card.isPetitionCard = false;
                }
                EditorUtility.SetDirty(Card);
                parentGraphView.Populate(parentGraphView.Database);
            }, Card.isLlmReactionCard ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction("Is Petition Card", _ =>
            {
                Undo.RecordObject(Card, "Toggle Petition Card");
                Card.isPetitionCard = !Card.isPetitionCard;
                if (Card.isPetitionCard)
                {
                    Card.isLlmReactionCard = false;
                }
                EditorUtility.SetDirty(Card);
                parentGraphView.Populate(parentGraphView.Database);
            }, Card.isPetitionCard ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            evt.menu.AppendSeparator();

            NarrativeDatabase db = parentGraphView?.Database;
            if (db != null && db.speakers != null)
            {
                evt.menu.AppendAction("Assign Speaker/None", _ => AssignSpeaker(null),
                    Card.speaker == null ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

                foreach (SpeakerData s in db.speakers)
                {
                    if (s != null)
                    {
                        SpeakerData speakerObj = s;
                        string label = s.DisplayName;
                        evt.menu.AppendAction($"Assign Speaker/{label}", _ => AssignSpeaker(speakerObj),
                            Card.speaker == speakerObj ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
                    }
                }
            }
        }

        private void AssignSpeaker(SpeakerData speaker)
        {
            if (Card == null || Card.speaker == speaker)
            {
                return;
            }

            Undo.RecordObject(Card, "Assign Card Speaker");
            Card.speaker = speaker;
            EditorUtility.SetDirty(Card);
            parentGraphView?.Populate(parentGraphView.Database);
        }
    }
}