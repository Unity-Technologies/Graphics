using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ConversionEdgeModel : EdgeModel
    {
        // EdgeLabel is used by EdgeBubblePart (and subclass ConversionEdgePart) to determine what to display.
        public override string EdgeLabel
        {
            get => $"{FromPort.PortDataType.FriendlyName()} -> {ToPort.PortDataType.FriendlyName()}";
            set { }
        }
    }
}
