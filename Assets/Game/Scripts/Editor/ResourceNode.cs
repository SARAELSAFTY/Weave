using Game.Scripts.Definitions;
using Game.Scripts.Runtime.Narrative;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    // Visual node representing one resource in the graph canvas.
    // Selecting or double-clicking this node pings/opens the individual ResourceData asset.
    public class ResourceNode : BaseNode
    {
        private static readonly Color ResourceHeaderColor = new Color(0.10f, 0.28f, 0.22f);
        private static readonly Color ResourceBorderColor = new Color(0.25f, 0.75f, 0.55f);

        public ResourceData Data { get; }

        protected override Object TargetAsset => Data;
        protected override string TargetId => Data != null ? Data.id : "Null Resource";

        protected override string PingActionLabel => "Ping Resource Asset";
        protected override string OpenActionLabel => "Open Resource Asset";

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
                paddingBottom = 6,
                paddingLeft = 8,
                paddingRight = 8,
                backgroundColor = new StyleColor(new Color(0.15f, 0.18f, 0.17f)),
                minWidth = 160
            }
        };

        Label nameValue = new Label(string.IsNullOrEmpty(data.displayName) ? "(Empty Name)" : data.displayName);
        nameValue.style.fontSize = 11;
        nameValue.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameValue.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f));
        nameValue.style.marginBottom = 4;
        body.Add(nameValue);

        Label startValue = new Label($"Starting Value: {data.defaultStartingValue}");
        startValue.style.fontSize = 10;
        startValue.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
        body.Add(startValue);

        if (data.icon != null)
        {
            Image iconImage = new Image
            {
                sprite = data.icon,
                scaleMode = ScaleMode.ScaleToFit
            };
            iconImage.style.width = 40;
            iconImage.style.height = 40;
            iconImage.style.marginTop = 8;
            iconImage.style.alignSelf = Align.Center;
            body.Add(iconImage);
        }

        return body;
    }

    public override void OnUnselected()
    {
        base.OnUnselected();
    }
}
}