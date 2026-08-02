using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

// Visual node representing one CardData in the graph: editable ID header, start/ending
// badges, a short description preview, and left/right choice rows with output ports.
public class CardNode : Node
{
    private const int DescriptionPreviewLength = 50;
    private const int ChoicePreviewLength = 22;

    private static readonly Color LeftAccent = new Color(0f, 0.9f, 1f);
    private static readonly Color RightAccent = new Color(1f, 0.55f, 0f);
    private static readonly Color StartBorder = new Color(1.0f, 0.84f, 0.0f);
    private static readonly Color DefaultBorder = new Color(0.28f, 0.30f, 0.35f);

    public CardData Card { get; }
    public Port InputPort { get; private set; }
    public Port LeftPort { get; private set; }
    public Port RightPort { get; private set; }

    private readonly CardGraphView parentGraphView;

    public CardNode(CardData card, bool isStartCard, Dictionary<string, CardData> validCards, CardGraphView parentGraphView)
    {
        Card = card;
        this.parentGraphView = parentGraphView;
        title = card.cardId;

        ApplyNodeChrome(isStartCard);
        Label idLabel = BuildIdLabel(card);
        titleContainer.Insert(0, idLabel);
        HideDefaultTitleLabel();
        titleContainer.Add(BuildBadges(card, isStartCard));

        InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        InputPort.portName = "In";
        InputPort.portColor = new Color(0.85f, 0.85f, 0.85f);
        inputContainer.Add(InputPort);

        extensionContainer.Add(BuildBody(card));
        extensionContainer.Add(BuildChoices(card, validCards));

        RefreshExpandedState();
        RefreshPorts();

        RegisterContextMenu(isStartCard);
        RegisterCallback<MouseDownEvent>(OnMouseDown);
    }

    private void ApplyNodeChrome(bool isStartCard)
    {
        style.backgroundColor = new StyleColor(new Color(0.14f, 0.15f, 0.18f));
        style.borderTopWidth = isStartCard ? 2 : 1;
        style.borderBottomWidth = isStartCard ? 2 : 1;
        style.borderLeftWidth = isStartCard ? 2 : 1;
        style.borderRightWidth = isStartCard ? 2 : 1;

        Color borderColor = isStartCard ? StartBorder : DefaultBorder;
        style.borderTopColor = new StyleColor(borderColor);
        style.borderBottomColor = new StyleColor(borderColor);
        style.borderLeftColor = new StyleColor(borderColor);
        style.borderRightColor = new StyleColor(borderColor);
        style.borderTopLeftRadius = 6;
        style.borderTopRightRadius = 6;
        style.borderBottomLeftRadius = 6;
        style.borderBottomRightRadius = 6;

        titleContainer.style.backgroundColor = isStartCard
            ? new StyleColor(new Color(0.28f, 0.24f, 0.05f))
            : new StyleColor(new Color(0.18f, 0.19f, 0.22f));
        titleContainer.style.paddingLeft = 8;
        titleContainer.style.paddingRight = 8;
        titleContainer.style.height = 34;
    }

    private static Label BuildIdLabel(CardData card)
    {
        return new Label(card.cardId)
        {
            style =
            {
                fontSize = 12,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = new StyleColor(Color.white),
                flexGrow = 1,
                marginRight = 6,
                unityTextAlign = TextAnchor.MiddleLeft
            }
        };
    }

    private void HideDefaultTitleLabel()
    {
        Label defaultTitleLabel = titleContainer.Q<Label>("title-label");
        if (defaultTitleLabel != null)
        {
            defaultTitleLabel.style.display = DisplayStyle.None;
        }
    }

    private VisualElement BuildBadges(CardData card, bool isStartCard)
    {
        VisualElement badges = new VisualElement
        {
            style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
        };

        if (isStartCard)
        {
            badges.Add(MakeBadge("★ START",
                new Color(1.0f, 0.85f, 0.1f), new Color(0.45f, 0.38f, 0.0f),
                "This card is the Starting Card for the narrative run."));
        }

        if (card.isEnding)
        {
            badges.Add(MakeBadge("ENDING",
                new Color(1.0f, 0.45f, 0.45f), new Color(0.45f, 0.10f, 0.10f),
                "This card ends the game because it has no next card choice links."));
        }

        return badges;
    }

    private static Label MakeBadge(string text, Color foreground, Color background, string tooltip)
    {
        return new Label(text)
        {
            style =
            {
                fontSize = 9,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = new StyleColor(foreground),
                backgroundColor = new StyleColor(background),
                paddingLeft = 5,
                paddingRight = 5,
                paddingTop = 2,
                paddingBottom = 2,
                marginRight = 6,
                borderTopLeftRadius = 3,
                borderTopRightRadius = 3,
                borderBottomLeftRadius = 3,
                borderBottomRightRadius = 3
            },
            tooltip = tooltip
        };
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

        if (!string.IsNullOrEmpty(card.speakerId) || card.dayAdvance > 0)
        {
            body.Add(BuildMetaRow(card));
        }

        body.Add(new Label(Truncate(card.description, DescriptionPreviewLength, "(No description)"))
        {
            style =
            {
                fontSize = 11,
                color = new StyleColor(new Color(0.82f, 0.84f, 0.88f)),
                whiteSpace = WhiteSpace.Normal
            }
        });

        return body;
    }

    private static VisualElement BuildMetaRow(CardData card)
    {
        VisualElement row = new VisualElement
        {
            style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 }
        };

        if (!string.IsNullOrEmpty(card.speakerId))
        {
            row.Add(new Label($"Speaker: {card.speakerId}")
            {
                style = { fontSize = 10, color = new StyleColor(new Color(0.7f, 0.75f, 0.9f)) }
            });
        }

        if (card.dayAdvance > 0)
        {
            row.Add(new Label($"+{card.dayAdvance} Day{(card.dayAdvance > 1 ? "s" : "")}")
            {
                style = { fontSize = 10, color = new StyleColor(new Color(0.5f, 0.85f, 0.5f)) }
            });
        }

        return row;
    }

    private VisualElement BuildChoices(CardData card, Dictionary<string, CardData> validCards)
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

        choices.Add(CreateChoiceRow("L", card.leftChoiceText, LeftAccent, card.leftNextCardId, validCards, out Port leftPort));
        LeftPort = leftPort;

        choices.Add(CreateChoiceRow("R", card.rightChoiceText, RightAccent, card.rightNextCardId, validCards, out Port rightPort));
        RightPort = rightPort;

        return choices;
    }

    private VisualElement CreateChoiceRow(string prefix, string choiceText, Color accentColor, string targetCardId,
        Dictionary<string, CardData> validCards, out Port outputPort)
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
            style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, flexShrink = 1 }
        };

        leftSide.Add(new Label($"{prefix}: {Truncate(choiceText, ChoicePreviewLength, "(Choice text)")}")
        {
            style = { fontSize = 11, color = new StyleColor(accentColor) }
        });

        if (CardGraph.IsBrokenLink(targetCardId, validCards))
        {
            leftSide.Add(MakeBadge($"⚠ Broken: '{targetCardId}'",
                new Color(1.0f, 0.75f, 0.2f), new Color(0.35f, 0.22f, 0.0f),
                $"Points to Card ID '{targetCardId}' which does not exist in this database."));
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

    private void RegisterContextMenu(bool isStartCard)
    {
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Set as Starting Card", _ => parentGraphView.SetStartingCard(Card),
                isStartCard ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            evt.menu.AppendAction("Ping Asset in Project", _ =>
            {
                Selection.activeObject = Card;
                EditorGUIUtility.PingObject(Card);
            });
            evt.menu.AppendAction("Open Asset in Inspector", _ => AssetDatabase.OpenAsset(Card));
        }));
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.clickCount == 2 && Card != null)
        {
            AssetDatabase.OpenAsset(Card);
            evt.StopPropagation();
        }
    }

    public override void OnSelected()
    {
        base.OnSelected();
        if (Card != null)
        {
            Selection.activeObject = Card;
            EditorGUIUtility.PingObject(Card);
        }
    }
}
