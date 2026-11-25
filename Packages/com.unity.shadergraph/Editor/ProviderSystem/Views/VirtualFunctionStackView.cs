using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    sealed class VirtualFunctionStackView : StackNode
    {
        internal VirtualFunctionStackData stackData { get; private set; }

        internal Port inputPort { get; }
        internal Port outputPort { get; }

        internal VirtualFunctionStackView(VirtualFunctionStackData data)
        {
            capabilities &= ~(Capabilities.Deletable | Capabilities.Copiable);
            var header = new Label(AbstractVirtualFunctionNode.ContextLabel(data.context)) { name = "headerLabel" };
            headerContainer.Add(header);
            inputPort = AddPort(Direction.Input, inputContainer);
            outputPort = AddPort(Direction.Output, outputContainer);
            Bind(data);

            if (string.IsNullOrEmpty(data.context))
            {
                var badge = InternalBridge.IconBadge.CreateComment("These virtual functions have no 'sg:Context' hint, so they are shown in this stack.");
                hierarchy.Add(badge);
                badge.AttachTo(header, SpriteAlignment.RightCenter);
            }
        }

        static Port AddPort(Direction direction, VisualElement container)
        {
            var port = Port.Create<Edge>(Orientation.Vertical, direction, Port.Capacity.Single, null);
            port.portName = string.Empty;
            port.pickingMode = PickingMode.Ignore;
            container.Add(port);
            return port;
        }

        internal void Bind(VirtualFunctionStackData data)
        {
            stackData = data;
            if (data.isPlaced && GetPosition().position != data.position)
                SetPosition(new Rect(data.position, Vector2.zero));
        }

        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
            => element.userData is VirtualFunctionOutputNode output && stackData.Holds(output.providerKey);

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
        }

        protected override void OnSeparatorContextualMenuEvent(ContextualMenuPopulateEvent evt, int separatorIndex)
        {
        }
    }
}
