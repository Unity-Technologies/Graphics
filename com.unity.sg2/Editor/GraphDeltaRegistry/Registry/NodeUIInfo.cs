using System.Collections.Generic;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    interface INodeUIDescriptorBuilder
    {
        NodeUIDescriptor CreateDescriptor(NodeHandler handler);
    }

    internal class StaticNodeUIDescriptorBuilder : INodeUIDescriptorBuilder
    {
        private readonly NodeUIDescriptor _descriptor;

        public StaticNodeUIDescriptorBuilder(NodeUIDescriptor descriptor)
        {
            this._descriptor = descriptor;
        }

        public NodeUIDescriptor CreateDescriptor(NodeHandler handler) => _descriptor;
    }

    /// <summary>
    /// NodeUIInfo is a registry for NodeUIDescriptors for all nodes that have
    /// been assigned a RegistryKey.
    /// </summary>
    internal class NodeUIInfo
    {
        private readonly Dictionary<RegistryKey, INodeUIDescriptorBuilder> _builders = new ();

        public void Register(RegistryKey key, NodeUIDescriptor descriptor)
            => Register(key, new StaticNodeUIDescriptorBuilder(descriptor));

        public void Register(RegistryKey key, INodeUIDescriptorBuilder descriptor)
            => _builders[key] = descriptor;

        public NodeUIDescriptor GetNodeUIDescriptor(RegistryKey key, NodeHandler nodeInstance)
            => _builders.ContainsKey(key) ? _builders[key].CreateDescriptor(nodeInstance) : CreateDefaultDescriptor(key);

        private static NodeUIDescriptor CreateDefaultDescriptor(RegistryKey key)
        {
            return new NodeUIDescriptor(
                1,
                 key.Name,
                 "DEFAULT_TOOLTIP",
                "DEFAULT_CATEGORY",
                new string[] { },
                key.Name,
                true,
                new Dictionary<string, string> { },
                new ParameterUIDescriptor[] {}
            );
        }
    }
}
