using Unity.GraphToolsFoundation.Editor;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ConversionEdgeModel : WireModel
    {
        // EdgeLabel is used by EdgeBubblePart (and subclass ConversionEdgePart) to determine what to display.
        public override string WireLabel
        {
            get => $"{FromPort.PortDataType.FriendlyName()} -> {ToPort.PortDataType.FriendlyName()}";
            set { }
        }
    }
}
