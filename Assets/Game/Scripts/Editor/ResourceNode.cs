using Game.Scripts.Definitions;
using Game.Scripts.Localization;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Scripts.Editor
{
    /// <summary>Graph node representing a ResourceData asset, with an output port for collapse endings.</summary>
    public class ResourceNode : BaseNode
    {
        private static readonly Color ResourceHeaderColor = new Color(0.10f, 0.28f, 0.22f);
        private static readonly Color ResourceBorderColor = new Color(0.25f, 0.75f, 0.55f);

        /// <summary>The resource asset this node represents.</summary>
        public ResourceData Data { get; }
        /// <summary>Output port connecting this resource to the card ending that collapses it.</summary>
        public Port CollapsePort { get; private set; }

        protected override Object TargetAsset => Data;
        protected override string TargetId => Data != null ? Data.AssetName : "Null Resource";

        protected override string PingActionLabel => "Ping Resource Asset";
        protected override string OpenActionLabel => "Open Resource Asset";

        /// <summary>Builds the node body, preview icon and collapse port for the given resource.</summary>
        /// <param name="data">Resource asset to display; may be null for a placeholder node.</param>
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

            AddIdentityLabels(body, data.GetDisplayName(GameLanguage.English), data.assetName);

            Label startValue = new Label($"Starting Value: {data.defaultStartingValue}");
            startValue.style.fontSize = 10;
            startValue.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.7f));
            body.Add(startValue);

            AddPreviewImage(body, data.icon, 40f, withBorder: false);

            return body;
        }
    }
}
