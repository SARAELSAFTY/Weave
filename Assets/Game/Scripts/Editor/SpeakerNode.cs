using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// Visual node representing one CouncilMemberData speaker in the graph canvas, allowing
// editing of display name, title, and portrait directly from the graph window.
public class SpeakerNode : Node
{
    private static readonly Color SpeakerHeaderColor = new Color(0.28f, 0.15f, 0.38f);
    private static readonly Color SpeakerBorderColor = new Color(0.65f, 0.35f, 0.85f);

    public CouncilMemberData Speaker { get; }
    private readonly CardGraphView parentGraphView;

    public SpeakerNode(CouncilMemberData speaker, CardGraphView parentGraphView)
    {
        Speaker = speaker;
        this.parentGraphView = parentGraphView;
        title = !string.IsNullOrEmpty(speaker.memberId) ? speaker.memberId : speaker.name;

        ApplyNodeChrome();
        titleContainer.Insert(0, BuildHeaderLabel(speaker));
        HideDefaultTitleLabel();

        extensionContainer.Add(BuildBody(speaker));

        RefreshExpandedState();
        RefreshPorts();

        RegisterContextMenu();
        RegisterCallback<MouseDownEvent>(OnMouseDown);
    }

    private void ApplyNodeChrome()
    {
        style.backgroundColor = new StyleColor(new Color(0.14f, 0.13f, 0.17f));
        style.borderTopWidth = 2;
        style.borderBottomWidth = 2;
        style.borderLeftWidth = 2;
        style.borderRightWidth = 2;

        style.borderTopColor = new StyleColor(SpeakerBorderColor);
        style.borderBottomColor = new StyleColor(SpeakerBorderColor);
        style.borderLeftColor = new StyleColor(SpeakerBorderColor);
        style.borderRightColor = new StyleColor(SpeakerBorderColor);
        style.borderTopLeftRadius = 6;
        style.borderTopRightRadius = 6;
        style.borderBottomLeftRadius = 6;
        style.borderBottomRightRadius = 6;

        titleContainer.style.backgroundColor = new StyleColor(SpeakerHeaderColor);
        titleContainer.style.paddingLeft = 8;
        titleContainer.style.paddingRight = 8;
        titleContainer.style.height = 32;
    }

    private static Label BuildHeaderLabel(CouncilMemberData speaker)
    {
        string labelText = $"👤 {(!string.IsNullOrEmpty(speaker.memberId) ? speaker.memberId : speaker.name)}";
        return new Label(labelText)
        {
            style =
            {
                fontSize = 12,
                unityFontStyleAndWeight = FontStyle.Bold,
                color = new StyleColor(new Color(0.95f, 0.85f, 1.0f)),
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

    private VisualElement BuildBody(CouncilMemberData speaker)
    {
        VisualElement body = new VisualElement
        {
            style =
            {
                paddingTop = 6,
                paddingBottom = 6,
                paddingLeft = 8,
                paddingRight = 8,
                backgroundColor = new StyleColor(new Color(0.16f, 0.14f, 0.19f))
            }
        };

        TextField nameField = new TextField("Name:") { value = speaker.displayName };
        nameField.style.fontSize = 11;
        nameField.style.marginBottom = 4;
        nameField.RegisterValueChangedCallback(evt =>
        {
            if (speaker.displayName != evt.newValue)
            {
                Undo.RecordObject(speaker, "Edit Speaker Display Name");
                speaker.displayName = evt.newValue;
                EditorUtility.SetDirty(speaker);
            }
        });
        body.Add(nameField);

        TextField titleField = new TextField("Title:") { value = speaker.title };
        titleField.style.fontSize = 11;
        titleField.style.marginBottom = 4;
        titleField.RegisterValueChangedCallback(evt =>
        {
            if (speaker.title != evt.newValue)
            {
                Undo.RecordObject(speaker, "Edit Speaker Title");
                speaker.title = evt.newValue;
                EditorUtility.SetDirty(speaker);
            }
        });
        body.Add(titleField);

        ObjectField portraitField = new ObjectField("Portrait:")
        {
            objectType = typeof(Sprite),
            allowSceneObjects = false,
            value = speaker.portrait
        };
        portraitField.style.fontSize = 11;
        portraitField.RegisterValueChangedCallback(evt =>
        {
            Sprite newSprite = evt.newValue as Sprite;
            if (speaker.portrait != newSprite)
            {
                Undo.RecordObject(speaker, "Edit Speaker Portrait");
                speaker.portrait = newSprite;
                EditorUtility.SetDirty(speaker);
            }
        });
        body.Add(portraitField);

        return body;
    }

    private void RegisterContextMenu()
    {
        this.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            evt.menu.AppendAction("Ping Asset in Project", _ =>
            {
                Selection.activeObject = Speaker;
                EditorGUIUtility.PingObject(Speaker);
            });
            evt.menu.AppendAction("Open Asset in Inspector", _ => AssetDatabase.OpenAsset(Speaker));
        }));
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.clickCount == 2 && Speaker != null)
        {
            AssetDatabase.OpenAsset(Speaker);
            evt.StopPropagation();
        }
    }

    public override void OnSelected()
    {
        base.OnSelected();
        if (Speaker != null)
        {
            Selection.activeObject = Speaker;
            EditorGUIUtility.PingObject(Speaker);
        }
    }
}
