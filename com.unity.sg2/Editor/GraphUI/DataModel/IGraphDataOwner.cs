using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.GraphUI
{
    /// <summary>
    /// This interface should be implemented by any GraphElementModel that can be backed by data in the CLDS layer
    /// </summary>
    public interface IGraphDataOwner
    {
        /// <summary>
        /// The identifier/unique name used to represent this entity and retrieve info. regarding it from CLDS
        /// </summary>
        public string graphDataName { get; }

        /// <summary>
        /// The Registry key that represents the concrete type within the Registry, of this IGraphDataOwner
        /// </summary>
        public RegistryKey registryKey { get; }

        /// <summary>
        /// A flag that represents whether or not this IGraphDataOwner is actually backed by data in the CLDS layer, or whether it is a "fake" entity, like searcher previews for nodes
        /// </summary>
        public bool existsInGraphData { get; }
    }
}
