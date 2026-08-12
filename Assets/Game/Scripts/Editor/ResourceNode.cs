using Game.Scripts.Definitions;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    public class ResourceNode : BaseNode
    {
        private static readonly Color ResourceHeaderColor = new Color(0.10f, 0.28f, 0.22f);
        private static readonly Color ResourceBorderColor = new Color(0.25f, 0.75f, 0.55f);

        public ResourceData Data { get; }
        public Port CollapsePort { get; private set; }

        protected override Object TargetAsset => Data;
        protected override string TargetId => Data != null ? Data.DisplayName : "Null Resource";

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
                CollapsePort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                CollapsePort.portName = "Collapse Ending";
                CollapsePort.portColor = new Color(1f, 0.35f, 0.35f);
                outputContainer.Add(CollapsePort);
            }

            RefreshExpandedState();
            RefreshPorts();
        }

        private void ApplyNodeChrome()
        {
            style.width = 200;
            style.maxWidth = 200;
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

            AddIdentityLabels(body, data.DisplayName, data.assetName);

            Label startValue = new Label($"Starting Value: {data.defaultStartingValue}");
            startValue.style.fontSize = 10;
            startValue.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            body.Add(startValue);

            AddPreviewImage(body, data.icon, 40f, withBorder: false);

            return body;
        }
    }
}
