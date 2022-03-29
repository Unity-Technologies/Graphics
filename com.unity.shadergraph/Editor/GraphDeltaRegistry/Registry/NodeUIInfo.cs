using System.Collections.Generic;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta
{

    /// <summary>
    /// NodeUIInfo is a registry for NodeUIDescriptors for all nodes that have
    /// been assigned a RegistryKey.
    /// </summary>
    internal class NodeUIInfo
    {
        private readonly Dictionary<RegistryKey, NodeUIDescriptor> RegistryKeyToNodeUIDescriptor = new ();

        public NodeUIDescriptor this[RegistryKey key]
        {
            get
            {
                if (RegistryKeyToNodeUIDescriptor.ContainsKey(key))
                    return RegistryKeyToNodeUIDescriptor[key];
                return new NodeUIDescriptor();
            }
            set
            {
                RegistryKeyToNodeUIDescriptor[key] = (NodeUIDescriptor)value;
            }
        }

        public void Clear()
        {
            RegistryKeyToNodeUIDescriptor.Clear();
        }
    }
}
