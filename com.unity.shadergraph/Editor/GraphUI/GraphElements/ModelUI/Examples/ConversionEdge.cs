using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ConversionEdge : Edge
    {
        protected override void BuildPartList()
        {
            // Not calling base.BuildPartList() because we want to use our ConversionEdgePart instead of the default
            // bubble.
            PartList.AppendPart(EdgeControlPart.Create(edgeControlPartName, Model, this, ussClassName));
            PartList.AppendPart(new ConversionEdgePart(edgeBubblePartName, Model, this, ussClassName));
        }
    }
}
