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
