using System.Text;
using Unity.GraphToolsFoundation.Editor;
using Unity.GraphToolsFoundation;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ConnectionInfoPart : BaseModelViewPart
    {
        public ConnectionInfoPart(string name, GraphElementModel model, ModelView ownerElement,
            string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        public override VisualElement Root => label;
        private TextElement label;

        private string DescribeConnection(PortModel port, bool isInput)
        {
            var desc = new StringBuilder();

            desc.AppendLine($"<b>{port.UniqueName}</b>");

            var edgeNumber = 0;
            foreach (var edge in port.GetConnectedWires())
            {
                desc.AppendLine($" Edge #{edgeNumber++}");
                desc.AppendLine($"  Edge type: {edge.GetType().FriendlyName()}");

                var direction = isInput ? "From" : "To";
                var otherPort = isInput ? edge.FromPort : edge.ToPort;
                var otherNode = otherPort.NodeModel;

                desc.AppendLine($"  {direction} port: {otherPort.UniqueName} ({otherPort.PortDataType}) " +
                    $"on {otherNode.GetType().FriendlyName()}");
            }

            if (edgeNumber == 0)
            {
                desc.AppendLine(" No Connections");
            }

            return desc.ToString();
        }

        protected override void BuildPartUI(VisualElement parent)
        {
            if (m_Model is not NodeModel)
                return;

            label = new Label("");
            label.style.marginTop = 4;
            label.style.marginBottom = 4;
            label.style.marginLeft = 4;
            label.style.marginRight = 4;

            parent.Add(label);
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is not NodeModel nodeModel)
                return;

            if (nodeModel.Collapsed)
            {
                label.text = "<i>Expand for details</i>";
                return;
            }

            var desc = new StringBuilder();

            foreach (var inputPort in nodeModel.GetInputPorts())
            {
                desc.Append(DescribeConnection(inputPort, isInput: true));
            }

            desc.AppendLine("---");

            foreach (var outputPort in nodeModel.GetOutputPorts())
            {
                desc.Append(DescribeConnection(outputPort, isInput: false));
            }

            desc.AppendLine("---");

            foreach (var nodeModelPort in nodeModel.GetInputPorts())
            {
                desc.Append($"Embedded value of <b>{nodeModelPort.UniqueName}</b>: ");
                desc.AppendLine(nodeModelPort.EmbeddedValue.ObjectValue.ToString());
            }

            label.text = desc.ToString();
        }
    }
}
