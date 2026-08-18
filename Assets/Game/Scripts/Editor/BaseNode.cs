using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    /// <summary>Shared Card Graph node: chrome, ID label, double-click open, selection.</summary>
    public abstract class BaseNode : Node
    {
        protected abstract UnityEngine.Object TargetAsset { get; }
        protected abstract string TargetId { get; }

        protected virtual string PingActionLabel => "Ping Asset in Project";
        protected virtual string OpenActionLabel => "Open Asset in Inspector";

        protected BaseNode()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDownInternal);
        }

        protected void InitializeNode(string titleText)
        {
            title = titleText;
            titleContainer.Insert(0, BuildIdLabel(TargetId));
            HideDefaultTitleLabel();
            
            RegisterContextMenu();
        }

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

        protected static Label MakeBadge(string text, Color foreground, Color background, string tooltip)
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

        // Name label + optional "ID: {assetName}" label, shown only when it differs from the display name.
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

        // Centered square preview image, appended after identity/content labels.
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

        protected void HideDefaultTitleLabel()
        {
            Label defaultTitleLabel = titleContainer.Q<Label>("title-label");
            if (defaultTitleLabel != null)
            {
                defaultTitleLabel.style.display = DisplayStyle.None;
            }
        }

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

        protected virtual void AddCustomContextMenuActions(ContextualMenuPopulateEvent evt) { }

        private void OnMouseDownInternal(MouseDownEvent evt)
        {
            if (evt.clickCount == 2 && TargetAsset != null)
            {
                AssetDatabase.OpenAsset(TargetAsset);
                evt.StopPropagation();
            }
        }

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
