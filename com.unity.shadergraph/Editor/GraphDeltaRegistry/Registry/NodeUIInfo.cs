using System.Collections.Generic;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    /// <summary>
    /// NodeUIInfo is a registry for NodeUIDescriptors for all nodes that have
    /// been assigned a RegistryKey.
    /// </summary>
    class NodeUIInfo
    {
        private readonly Dictionary<RegistryKey, NodeUIDescriptor> RegistryKeyToNodeUIDescriptor;

        public NodeUIDescriptor this[RegistryKey key]
        {
            get {
                return RegistryKeyToNodeUIDescriptor[key];
            }
            set {
                RegistryKeyToNodeUIDescriptor[key] = value;
            }
        }
    }
}
