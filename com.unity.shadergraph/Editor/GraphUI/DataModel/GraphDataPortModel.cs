using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI.DataModel
{
    public class GraphDataPortModel : PortModel
    {
        public string graphDataName => UniqueName;

        public GraphDataNodeModel graphDataNodeModel => (GraphDataNodeModel) NodeModel;
        IGraphHandler graphHandler => ((ShaderGraphModel) GraphModel).GraphHandler;

        public IPortReader portReader => graphHandler
            .GetNode(graphDataNodeModel.graphDataName)
            .TryGetPort(graphDataName, out var reader)
            ? reader
            : null;
    }
}
