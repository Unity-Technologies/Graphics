using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class DataNode : CollapsibleInOutNode
    {
        public const string DataPartName = "data-node-value-container";

        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.AppendPart(new DataNodeTextPart(DataPartName, Model, this, ussClassName));
        }
    }
}
