using Unity.GraphToolsFoundation.Editor;
using Unity.GraphToolsFoundation;

namespace UnityEditor.ShaderGraph.GraphUI
{
    //[ItemLibraryItem(typeof(ShaderGraphStencil), SearcherContext.Graph, "Connection Info")]
    class ConnectionInfoNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            AddInputPort("In", PortType.Data, TypeHandle.Float);
            AddOutputPort("Out", PortType.Data, TypeHandle.Float);
        }
    }
}
