using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class DataNode : CollapsibleInOutNode
    {
        public const string DataPartName = "data-node-value-container";

        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.AppendPart(new DataNodeTextPart(DataPartName, Model as GraphElementModel, this, ussClassName));
        }
    }
}
