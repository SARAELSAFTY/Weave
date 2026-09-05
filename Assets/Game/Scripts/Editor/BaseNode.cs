using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    /// <summary>
    /// Abstract base for all graph nodes in the card narrative editor, providing shared chrome styling, context-menu actions, and double-click-to-open behavior.
    /// </summary>
    /// <remarks>
    /// Subclasses must supply <see cref="TargetAsset"/> and <see cref="TargetId"/> so the base class can wire up
    /// ping/open context-menu entries, selection-driven Inspector focus, and the title bar ID label.
    /// Call <see cref="InitializeNode"/> from the subclass constructor after setting those properties.
    /// </remarks>
    public abstract class BaseNode : Node
    {
        /// <summary>The ScriptableObject (or other asset) this node represents; used for ping, open, and selection sync.</summary>
        protected abstract UnityEngine.Object TargetAsset { get; }

        /// <summary>A short identifier string displayed in the node's title bar.</summary>
        protected abstract string TargetId { get; }

        /// <summary>Label for the context-menu action that pings the target asset in the Project window.</summary>
        protected virtual string PingActionLabel => "Ping Asset in Project";

        /// <summary>Label for the context-menu action that opens the target asset in the Inspector.</summary>
        protected virtual string OpenActionLabel => "Open Asset in Inspector";

        /// <summary>
        /// Registers a mouse-down callback so that double-clicking the node opens the target asset.
        /// </summary>
        protected BaseNode()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDownInternal);
        }

        /// <summary>
        /// Configures the node title, inserts an ID label into the title container, hides the default GraphView title label, and registers the context menu.
        /// </summary>
        /// <param name="titleText">Text shown as the node's accessible title.</param>
        protected void InitializeNode(string titleText)
        {
            title = titleText;
            titleContainer.Insert(0, BuildIdLabel(TargetId));
            HideDefaultTitleLabel();

            RegisterContextMenu();
        }

        /// <summary>
        /// Creates a bold white label styled for display as the node's asset-ID in the title bar.
        /// </summary>
        /// <param name="id">The identifier text to display.</param>
        /// <returns>A configured <see cref="Label"/> ready to be inserted into the title container.</returns>
        protected static Label BuildIdLabel(string id)
        {
            Label label = new Label(id);
            label.style.fontSize = 12;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = new StyleColor(Color.white);
            label.style.flexGrow = 1;
            label.style.marginRight = 6;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            return label;
        }

        /// <summary>
        /// Creates a small rounded badge label with custom foreground/background colors, used for status indicators in node headers.
        /// </summary>
        /// <param name="text">Badge text content.</param>
        /// <param name="foreground">Text color.</param>
        /// <param name="background">Background fill color.</param>
        /// <returns>A styled badge <see cref="Label"/>.</returns>
        protected static Label MakeBadge(string text, Color foreground, Color background)
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
            return label;
        }

        /// <summary>
        /// Adds a display-name label and an optional secondary asset-name label to the given container; the secondary label is omitted when it matches the display name.
        /// </summary>
        /// <param name="body">The visual element to add labels to.</param>
        /// <param name="displayName">Primary name shown in bold.</param>
        /// <param name="assetName">Secondary identifier shown in muted text if different from <paramref name="displayName"/>.</param>
        protected static void AddIdentityLabels(VisualElement body, string displayName, string assetName)
        {
            Label nameValue = new Label(displayName);
            nameValue.style.fontSize = 11;
            nameValue.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameValue.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f));
            nameValue.style.marginBottom = 2;
            body.Add(nameValue);

            if (!string.IsNullOrWhiteSpace(assetName) && assetName.Trim() != displayName)
            {
                Label idLabel = new Label($"ID: {assetName.Trim()}");
                idLabel.style.fontSize = 9;
                idLabel.style.color = new StyleColor(new Color(0.55f, 0.55f, 0.55f));
                idLabel.style.marginBottom = 4;
                body.Add(idLabel);
            }
        }

        /// <summary>
        /// Adds a centered sprite preview image to the given container, optionally with a thin border.
        /// </summary>
        /// <param name="body">The visual element to add the image to.</param>
        /// <param name="sprite">The sprite to display; if null, nothing is added.</param>
        /// <param name="size">Width and height of the image in pixels.</param>
        /// <param name="withBorder">Whether to draw a 1px dark border around the image.</param>
        protected static void AddPreviewImage(VisualElement body, Sprite sprite, float size, bool withBorder)
        {
            if (sprite == null) return;

            Image image = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit };
            image.style.width = size;
            image.style.height = size;
            image.style.marginTop = 8;
            image.style.alignSelf = Align.Center;

            if (withBorder)
            {
                Color borderColor = new Color(0.3f, 0.3f, 0.3f);
                image.style.borderTopWidth = 1;
                image.style.borderBottomWidth = 1;
                image.style.borderLeftWidth = 1;
                image.style.borderRightWidth = 1;
                image.style.borderTopColor = new StyleColor(borderColor);
                image.style.borderBottomColor = new StyleColor(borderColor);
                image.style.borderLeftColor = new StyleColor(borderColor);
                image.style.borderRightColor = new StyleColor(borderColor);
            }

            body.Add(image);
        }

        /// <summary>
        /// Hides the default GraphView title label so that only the custom ID label is visible in the title bar.
        /// </summary>
        protected void HideDefaultTitleLabel()
        {
            Label defaultTitleLabel = titleContainer.Q<Label>("title-label");
            if (defaultTitleLabel != null)
            {
                defaultTitleLabel.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Applies border, corner-radius, and header-background styling to the node chrome.
        /// </summary>
        /// <param name="headerColor">Background color for the title container.</param>
        /// <param name="borderColor">Color of the node's outer border.</param>
        /// <param name="borderWidth">Width of the outer border in pixels.</param>
        /// <param name="headerHeight">Fixed height of the title container in pixels.</param>
        protected void ApplyBaseChrome(Color headerColor, Color borderColor, float borderWidth = 2f, float headerHeight = 32f)
        {
            style.borderTopWidth = borderWidth;
            style.borderBottomWidth = borderWidth;
            style.borderLeftWidth = borderWidth;
            style.borderRightWidth = borderWidth;

            style.borderTopColor = new StyleColor(borderColor);
            style.borderBottomColor = new StyleColor(borderColor);
            style.borderLeftColor = new StyleColor(borderColor);
            style.borderRightColor = new StyleColor(borderColor);

            style.borderTopLeftRadius = 6;
            style.borderTopRightRadius = 6;
            style.borderBottomLeftRadius = 6;
            style.borderBottomRightRadius = 6;

            titleContainer.style.backgroundColor = new StyleColor(headerColor);
            titleContainer.style.paddingLeft = 8;
            titleContainer.style.paddingRight = 8;
            titleContainer.style.height = headerHeight;
        }

        /// <summary>
        /// Attaches a contextual menu manipulator that appends subclass-specific actions followed by standard Ping/Open asset actions.
        /// </summary>
        private void RegisterContextMenu()
        {
            this.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                AddCustomContextMenuActions(evt);

                evt.menu.AppendAction(PingActionLabel, _ =>
                {
                    if (TargetAsset != null)
                    {
                        Selection.activeObject = TargetAsset;
                        EditorGUIUtility.PingObject(TargetAsset);
                    }
                });
                evt.menu.AppendAction(OpenActionLabel, _ =>
                {
                    if (TargetAsset != null)
                    {
                        AssetDatabase.OpenAsset(TargetAsset);
                    }
                });
            }));
        }

        /// <summary>Override point for subclasses to insert custom actions into the node's right-click context menu before the standard Ping/Open entries.</summary>
        /// <param name="evt">The contextual menu populate event to append actions to.</param>
        protected virtual void AddCustomContextMenuActions(ContextualMenuPopulateEvent evt) { }

        /// <summary>Handles double-click on the node to open the target asset in the Inspector.</summary>
        private void OnMouseDownInternal(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && TargetAsset != null)
            {
                AssetDatabase.OpenAsset(TargetAsset);
                evt.StopPropagation();
            }
        }

        /// <summary>When the node is selected in the graph, sets the target asset as the active selection and pings it in the Project window.</summary>
        public override void OnSelected()
        {
            base.OnSelected();
            if (TargetAsset != null)
            {
                Selection.activeObject = TargetAsset;
                EditorGUIUtility.PingObject(TargetAsset);
            }
        }
    }
}
