using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ConversionEdge : Wire
    {
        protected override void BuildPartList()
        {
            // Not calling base.BuildPartList() because we want to use our ConversionEdgePart instead of the default
            // bubble.
            PartList.AppendPart(WireControlPart.Create(wireControlPartName, Model, this, ussClassName));
            PartList.AppendPart(new ConversionEdgePart(wireBubblePartName, Model as GraphElementModel, this, ussClassName));
        }
    }
}
