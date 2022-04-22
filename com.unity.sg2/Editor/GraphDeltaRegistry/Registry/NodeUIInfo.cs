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
        readonly Dictionary<RegistryKey, NodeUIDescriptor> m_RegistryKeyToNodeUIDescriptor = new ();

        public NodeUIDescriptor this[RegistryKey key]
        {
            get => m_RegistryKeyToNodeUIDescriptor.ContainsKey(key) ? m_RegistryKeyToNodeUIDescriptor[key] : CreateDefaultDescriptor();
            set => m_RegistryKeyToNodeUIDescriptor[key] = value;
        }

        static NodeUIDescriptor CreateDefaultDescriptor()
        {
            return new NodeUIDescriptor(
                1,
                 "DEFAULT_NAME",
                 "DEFAULT_TOOLTIP",
                new string[] { "DEFAULT_CATEGORY" },
                new string[] { },
                "DEFAULT_DISPLAY_NAME",
                true,
                new Dictionary<string, string> { },
                new ParameterUIDescriptor[] {}
                );
        }

        public void Clear()
        {
            m_RegistryKeyToNodeUIDescriptor.Clear();
        }
    }
}
