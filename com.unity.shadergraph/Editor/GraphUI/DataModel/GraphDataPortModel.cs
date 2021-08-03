using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class GraphDataPortModel : PortModel
    {
        public string graphDataName => UniqueName;
        public GraphDataNodeModel graphDataNodeModel => (GraphDataNodeModel) NodeModel;
    }
}
