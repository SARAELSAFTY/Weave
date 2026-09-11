using System;
using System.Collections.Generic;
using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    /// <summary>Graph node representing a CardData asset: status badges, description preview and ports for its branches.</summary>
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
        private static readonly Color ChatBorder = new Color(0.95f, 0.6f, 0.2f);
        private static readonly Color DefaultBorder = new Color(0.28f, 0.30f, 0.35f);

        /// <summary>The card asset this node represents.</summary>
        public CardData Card { get; }
        /// <summary>Input port; cards leading into this one connect here.</summary>
        public Port InputPort { get; private set; }
        /// <summary>Output port for the left choice branch.</summary>
        public Port LeftPort { get; private set; }
        /// <summary>Output port for the right choice branch.</summary>
        public Port RightPort { get; private set; }
        /// <summary>Output port for the continue exit, when the card has no choices.</summary>
        public Port ContinuePort { get; private set; }

        private readonly CardGraphView parentGraphView;
        private bool isStartCardCached;
        private VisualElement metaRow;
        private VisualElement bodyContainer;
        private Label summaryLabel;

        protected override UnityEngine.Object TargetAsset => Card;
        protected override string TargetId => Card != null ? Card.AssetName : "Null Card";
        protected override string PingActionLabel => "Ping Card Asset";
        protected override string OpenActionLabel => "Open Card Asset";

        /// <summary>Builds the node chrome, badges, body and branch ports for the given card.</summary>
        /// <param name="card">Card asset to display.</param>
        /// <param name="isStartCard">Whether this card is the database's starting card (highlighted border + START badge).</param>
        /// <param name="parentGraphView">Graph view owning this node, used for rebuilds and starting-card changes.</param>
        public CardNode(CardData card, bool isStartCard, CardGraphView parentGraphView)
        {
            Card = card;
            this.parentGraphView = parentGraphView;
            this.isStartCardCached = isStartCard;

            InitializeNode(TargetId);
            ApplyNodeChrome(isStartCard, card.isLlmReactionCard, card.isPetitionCard, card.isChatCard);
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

        private void ApplyNodeChrome(bool isStartCard, bool isLlmCard, bool isPetitionCard = false, bool isChatCard = false)
        {
            style.width = 260;
            style.maxWidth = 260;
            style.backgroundColor = new StyleColor(new Color(0.14f, 0.15f, 0.18f));

            bool highlightBorder = isStartCard || isLlmCard || isPetitionCard || isChatCard;
            Color borderColor = isStartCard ? StartBorder
                : isLlmCard ? LlmBorder
                : isPetitionCard ? PetitionBorder
                : isChatCard ? ChatBorder
                : DefaultBorder;
            Color headerColor = isStartCard ? new Color(0.28f, 0.24f, 0.05f)
                : isLlmCard ? new Color(0.22f, 0.12f, 0.32f)
                : isPetitionCard ? new Color(0.05f, 0.22f, 0.20f)
                : isChatCard ? new Color(0.30f, 0.19f, 0.05f)
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
                    new Color(1.0f, 0.85f, 0.1f), new Color(0.45f, 0.38f, 0.0f)));
            }

            if (card.isLlmReactionCard)
            {
                badges.Add(MakeBadge("LLM",
                    new Color(0.85f, 0.65f, 1f), new Color(0.30f, 0.12f, 0.45f)));
            }

            if (card.isPetitionCard)
            {
                badges.Add(MakeBadge("PETITION",
                    new Color(0.2f, 1f, 0.9f), new Color(0.03f, 0.28f, 0.25f)));
            }

            if (card.isChatCard)
            {
                badges.Add(MakeBadge("CHAT",
                    new Color(1f, 0.8f, 0.5f), new Color(0.35f, 0.22f, 0.02f)));
            }

            if ((card.isPetitionCard || card.isChatCard) && card.petitionerSource == PetitionerSource.GeneratedCommoner)
            {
                badges.Add(MakeBadge("COMMONER",
                    new Color(0.2f, 1f, 0.9f), new Color(0.03f, 0.28f, 0.25f)));
            }
            else if ((card.isLlmReactionCard || card.isPetitionCard || card.isChatCard) && card.speaker != null &&
                     string.IsNullOrWhiteSpace(card.speaker.llmPersonaPrompt))
            {
                badges.Add(MakeBadge("No Speaker Persona",
                    new Color(1.0f, 0.75f, 0.2f), new Color(0.35f, 0.22f, 0.0f)));
            }

            if (!card.isPetitionCard && !card.isChatCard && card.speaker == null)
            {
                badges.Add(MakeBadge("NARRATOR",
                    new Color(0.7f, 0.8f, 1f), new Color(0.15f, 0.25f, 0.45f)));
            }

            if (card.HasBrokenBranch)
            {
                badges.Add(MakeBadge("DEAD END",
                    new Color(1.0f, 0.45f, 0.45f), new Color(0.45f, 0.10f, 0.10f)));
            }

            if (card.IsEnding)
            {
                badges.Add(MakeBadge("ENDING",
                    new Color(1.0f, 0.45f, 0.45f), new Color(0.45f, 0.10f, 0.10f)));
            }

            return badges;
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

            bodyContainer = body;

            summaryLabel = new Label(BuildSummary(card));
            summaryLabel.style.fontSize = 10;
            summaryLabel.style.color = new StyleColor(new Color(0.62f, 0.66f, 0.74f));
            summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            summaryLabel.style.marginBottom = 4;
            body.Add(summaryLabel);

            string previewText = card.isLlmReactionCard
                ? BuildSeedPreview(card.reactionSeedOverride, "reaction")
                : card.isPetitionCard
                    ? BuildSeedPreview(card.petitionSeedOverride, "petition")
                    : card.isChatCard
                        ? BuildSeedPreview(card.chatSeedOverride, "chat")
                        : Truncate(card.GetDescription(GameLanguage.English), DescriptionPreviewLength, "(No description)");

            Label previewLabel = new Label(previewText);
            previewLabel.style.fontSize = 11;
            previewLabel.style.color = new StyleColor(new Color(0.82f, 0.84f, 0.88f));
            previewLabel.style.whiteSpace = WhiteSpace.Normal;
            body.Add(previewLabel);

            return body;
        }

        // Builds the inline-editable row (speaker / visual template / art mode popups) on first selection,
        // so node construction avoids the visual-template asset scan for nodes that are never selected.
        private VisualElement BuildMetaRow(CardData card)
        {
            VisualElement row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    marginBottom = 4
                }
            };

            NarrativeDatabase db = parentGraphView?.Database;
            List<SpeakerData> speakerList = db?.speakers ?? new List<SpeakerData>();

            List<string> choices = new List<string> { "(No Speaker)" };
            List<SpeakerData> choiceSpeakers = new List<SpeakerData> { null };
            int selectedIndex = 0;
            foreach (SpeakerData s in speakerList)
            {
                if (s == null)
                {
                    continue;
                }

                choices.Add(s.GetDisplayName(GameLanguage.English));
                choiceSpeakers.Add(s);
                if (card.speaker == s)
                {
                    selectedIndex = choices.Count - 1;
                }
            }

            PopupField<string> speakerPopup = new PopupField<string>(choices, selectedIndex);
            speakerPopup.style.fontSize = 10;
            speakerPopup.style.height = 18;
            speakerPopup.RegisterValueChangedCallback(evt =>
            {
                int newIdx = choices.IndexOf(evt.newValue);
                SpeakerData newSpeaker = newIdx >= 0 && newIdx < choiceSpeakers.Count ? choiceSpeakers[newIdx] : null;
                if (card.speaker != newSpeaker)
                {
                    Undo.RecordObject(card, "Assign Card Speaker");
                    card.speaker = newSpeaker;
                    EditorUtility.SetDirty(card);
                    parentGraphView?.Populate(parentGraphView.Database);
                }
            });
            row.Add(speakerPopup);

            List<CardVisualTemplate> templateAssets = new List<CardVisualTemplate>();
            foreach (string guid in AssetDatabase.FindAssets("t:CardVisualTemplate"))
            {
                CardVisualTemplate loaded = AssetDatabase.LoadAssetAtPath<CardVisualTemplate>(AssetDatabase.GUIDToAssetPath(guid));
                if (loaded != null)
                {
                    templateAssets.Add(loaded);
                }
            }

            templateAssets.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));

            List<string> templateChoices = new List<string> { "(Default Look)" };
            List<CardVisualTemplate> choiceTemplates = new List<CardVisualTemplate> { null };
            int selectedTemplateIndex = 0;
            foreach (CardVisualTemplate t in templateAssets)
            {
                templateChoices.Add(t.name);
                choiceTemplates.Add(t);
                if (card.visualTemplate == t)
                {
                    selectedTemplateIndex = templateChoices.Count - 1;
                }
            }

            PopupField<string> templatePopup = new PopupField<string>(templateChoices, selectedTemplateIndex);
            templatePopup.style.fontSize = 10;
            templatePopup.style.height = 18;
            templatePopup.RegisterValueChangedCallback(evt =>
            {
                int newIdx = templateChoices.IndexOf(evt.newValue);
                CardVisualTemplate newTemplate = newIdx >= 0 && newIdx < choiceTemplates.Count ? choiceTemplates[newIdx] : null;
                if (card.visualTemplate != newTemplate)
                {
                    Undo.RecordObject(card, "Assign Card Visual Template");
                    card.visualTemplate = newTemplate;
                    EditorUtility.SetDirty(card);
                    parentGraphView?.Populate(parentGraphView.Database);
                }
            });
            row.Add(templatePopup);

            List<string> artChoices = new List<string> { "Art: Speaker", "Art: Image", "Art: None" };
            PopupField<string> artPopup = new PopupField<string>(artChoices, (int)card.artMode);
            artPopup.style.fontSize = 10;
            artPopup.style.height = 18;
            artPopup.RegisterValueChangedCallback(evt =>
            {
                CardArtMode newMode = (CardArtMode)artChoices.IndexOf(evt.newValue);
                if (card.artMode != newMode)
                {
                    Undo.RecordObject(card, "Change Card Art Mode");
                    card.artMode = newMode;
                    EditorUtility.SetDirty(card);
                    parentGraphView?.Populate(parentGraphView.Database);
                }
            });
            row.Add(artPopup);

            if (card.dayAdvance > 0)
            {
                Label dayLabel = new Label($"+{card.dayAdvance} Day{(card.dayAdvance > 1 ? "s" : "")}");
                dayLabel.style.fontSize = 10;
                dayLabel.style.color = new StyleColor(new Color(0.5f, 0.85f, 0.5f));
                row.Add(dayLabel);
            }

            return row;
        }

        /// <summary>Shows the editable meta row in place of the summary label while the node is selected.</summary>
        public override void OnSelected()
        {
            base.OnSelected();
            if (metaRow == null)
            {
                metaRow = BuildMetaRow(Card);
                bodyContainer.Insert(bodyContainer.IndexOf(summaryLabel), metaRow);
            }

            metaRow.style.display = DisplayStyle.Flex;
            summaryLabel.style.display = DisplayStyle.None;
        }

        /// <summary>Restores the read-only summary label when the node is deselected, rebuilding it to reflect any edits.</summary>
        public override void OnUnselected()
        {
            base.OnUnselected();
            if (metaRow != null)
            {
                metaRow.style.display = DisplayStyle.None;
            }

            if (summaryLabel != null)
            {
                summaryLabel.text = BuildSummary(Card);
                summaryLabel.style.display = DisplayStyle.Flex;
            }
        }

        private static string BuildSummary(CardData card)
        {
            string speaker = card.speaker != null ? card.speaker.GetDisplayName(GameLanguage.English) : "No Speaker";
            string look = card.visualTemplate != null ? card.visualTemplate.name : "Default Look";
            string art = card.artMode == CardArtMode.EventImage ? "Art: Image"
                : card.artMode == CardArtMode.None ? "Art: None"
                : "Art: Speaker";
            return Truncate($"{speaker}  ·  {look}  ·  {art}", 48, "");
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

            choices.Add(CreateChoiceRow("L", card.GetLeftChoice(GameLanguage.English), LeftAccent, card.leftResourceChange, out Port leftPort));
            LeftPort = leftPort;

            choices.Add(CreateChoiceRow("R", card.GetRightChoice(GameLanguage.English), RightAccent, card.rightResourceChange, out Port rightPort));
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

                foreach (ResourceValue rv in resourceChange.values)
                {
                    if (rv.resource == null)
                    {
                        continue;
                    }

                    string name = rv.resource.GetDisplayName(GameLanguage.English);
                    Label resLabel = new Label($"{(rv.value >= 0 ? "+" : "")}{rv.value}{name[0]}");
                    resLabel.style.fontSize = 9;
                    resLabel.style.color = new StyleColor(rv.value >= 0 ? ResourceAccent : NegativeResourceAccent);
                    resLabel.style.marginLeft = 2;
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

        private static string BuildSeedPreview(string seedOverride, string kind)
        {
            return !string.IsNullOrWhiteSpace(seedOverride)
                ? Truncate(seedOverride, DescriptionPreviewLength, seedOverride)
                : $"(Uses global {kind} seed prompt)";
        }

        private static string Truncate(string text, int maxLength, string emptyFallback)
        {
            if (string.IsNullOrEmpty(text))
            {
                return emptyFallback;
            }

            return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
        }

        /// <summary>Adds card-specific context-menu actions: starting card, card kind toggles and speaker assignment.</summary>
        protected override void AddCustomContextMenuActions(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Set as Starting Card", _ => parentGraphView.SetStartingCard(Card),
                isStartCardCached ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            // LLM reaction, petition, and chat are mutually exclusive card kinds; enabling one disables the others.
            (string label, string undoName, Func<CardData, bool> get, Action<CardData, bool> set, Action<CardData> clearOthers)[] modeToggles =
            {
                ("Is LLM Reaction Card", "Toggle LLM Reaction", c => c.isLlmReactionCard, (c, v) => c.isLlmReactionCard = v,
                    c => { c.isPetitionCard = false; c.isChatCard = false; }),
                ("Is Petition Card", "Toggle Petition Card", c => c.isPetitionCard, (c, v) => c.isPetitionCard = v,
                    c => { c.isLlmReactionCard = false; c.isChatCard = false; }),
                ("Is Chat Card", "Toggle Chat Card", c => c.isChatCard, (c, v) => c.isChatCard = v,
                    c => { c.isLlmReactionCard = false; c.isPetitionCard = false; })
            };

            foreach ((string label, string undoName, Func<CardData, bool> get, Action<CardData, bool> set, Action<CardData> clearOthers) toggle in modeToggles)
            {
                DropdownMenuAction.Status status = toggle.get(Card) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                evt.menu.AppendAction(toggle.label, _ =>
                {
                    bool enabling = !toggle.get(Card);
                    Undo.RecordObject(Card, toggle.undoName);
                    toggle.set(Card, enabling);
                    if (enabling)
                    {
                        toggle.clearOthers(Card);
                    }
                    EditorUtility.SetDirty(Card);
                    parentGraphView.Populate(parentGraphView.Database);
                }, status);
            }

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
                        string label = s.GetDisplayName(GameLanguage.English);
                        evt.menu.AppendAction($"Assign Speaker/{label}", _ => AssignSpeaker(s),
                            Card.speaker == s ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
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
