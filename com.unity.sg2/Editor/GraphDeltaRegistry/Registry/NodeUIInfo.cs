using System.Collections.Generic;
using UnityEditor.ShaderGraph.Defs;
using System.Linq;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    interface INodeUIDescriptorBuilder
    {
        NodeUIDescriptor CreateDescriptor(NodeHandler handler);
    }

    internal class StaticNodeUIDescriptorBuilder : INodeUIDescriptorBuilder
    {
        private NodeUIDescriptor descriptor;
        public StaticNodeUIDescriptorBuilder(NodeUIDescriptor descriptor) => this.descriptor = descriptor;
        public NodeUIDescriptor CreateDescriptor(NodeHandler handler) => descriptor;
    }

    /// <summary>
    /// NodeUIInfo is a registry for NodeUIDescriptors for all nodes that have
    /// been assigned a RegistryKey.
    /// </summary>
    internal class NodeUIInfo
    {
        readonly Dictionary<RegistryKey, INodeUIDescriptorBuilder> factories = new ();

        public void Register(RegistryKey key, NodeUIDescriptor descriptor)
            => Register(key, new StaticNodeUIDescriptorBuilder(descriptor));

        public void Register(RegistryKey key, INodeUIDescriptorBuilder descriptor)
            => factories[key] = descriptor;

        public NodeUIDescriptor GetNodeUIDescriptor(RegistryKey key, NodeHandler nodeInstance)
            => factories.ContainsKey(key) ? factories[key].CreateDescriptor(nodeInstance) : CreateDefaultDescriptor(key);

        private static NodeUIDescriptor CreateDefaultDescriptor(RegistryKey key, NodeHandler nodeInstance = null)
        {
            List<ParameterUIDescriptor> parameters = new();
            if (nodeInstance != null)
            {
                foreach(var port in nodeInstance.GetPorts())
                {
                    parameters.Add(new(port.LocalID));
                }
            }

            return new NodeUIDescriptor(
                1,
                 key.ToString(),
                 "DEFAULT_TOOLTIP",
                new string[] { "DEFAULT_CATEGORY" },
                new string[] { },
                key.Name,
                true,
                new Dictionary<string, string> { },
                parameters.ToArray()
                );
        }
    }
}
