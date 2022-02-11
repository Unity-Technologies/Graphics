using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [SearcherItem(typeof(ShaderGraphStencil), SearcherContext.Graph, "Data Node")]
    public class DataNodeModel : NodeModel
    {
        public int intValue;
        public float floatValue;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddInputPort("In", PortType.Data, TypeHandle.Float);
            AddOutputPort("Out", PortType.Data, TypeHandle.Float);
        }
    }
}
