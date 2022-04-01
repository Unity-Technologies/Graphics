using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public interface IGraphDataPortModelOwner
    {
        public string graphDataName { get; }
        public RegistryKey registryKey { get; }
        public bool existsInGraphData { get; }
    }
}
