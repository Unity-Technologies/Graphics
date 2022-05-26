using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    class ShaderGraphRegistry
    {
        Registry registry;
        NodeUIInfo descriptors;
        GraphHandler topologies;

        void Register(RegistryKey key, INodeUIDescriptorBuilder descriptor) => descriptors.Register(key, descriptor);
        void Register(NodeDescriptor node, NodeUIDescriptor descriptor)
        {
            var key = registry.Register(node);
            descriptors.Register(key, new StaticNodeUIDescriptorBuilder(descriptor));
            topologies.AddNode(key, key.ToString());
        }
        void Register(FunctionDescriptor function, NodeUIDescriptor descriptor)
        {
            var key = registry.Register(function);
            descriptors.Register(key, new StaticNodeUIDescriptorBuilder(descriptor));
            topologies.AddNode(key, key.ToString());
        }
        void Register(FunctionDescriptor func)
        {
            var key = registry.Register(func);
            topologies.AddNode(key, key.ToString());
        }
        void Register(NodeDescriptor node)
        {
            var key = registry.Register(node);
            topologies.AddNode(key, key.ToString());
        }
        void Register(INodeDefinitionBuilder builder, INodeUIDescriptorBuilder descriptor = null)
        {
            var key = builder.GetRegistryKey();
            registry.Register(builder);
            if (descriptor != null)
                descriptors.Register(key, descriptor);
            topologies.AddNode(key, key.ToString());
        }
        void Register<T>() where T : IRegistryEntry => registry.Register<T>();

        RegistryKey ResolveKey<T>() where T : IRegistryEntry => Registry.ResolveKey<T>();

        NodeHandler GetDefaultTopology(RegistryKey key) => registry.GetDefaultTopology(key);
        NodeUIDescriptor GetNodeUIDescriptor(RegistryKey key, NodeHandler node) => descriptors.GetNodeUIDescriptor(key, node);
        INodeDefinitionBuilder GetNodeBuilder(RegistryKey key) => registry.GetNodeBuilder(key);
        ITypeDefinitionBuilder GetTypeBuilder(RegistryKey key) => registry.GetTypeBuilder(key);
        ICastDefinitionBuilder GetCastBuilder(RegistryKey key) => registry.GetCastBuilder(key);
        IContextDescriptor GetContextDescriptor(RegistryKey key) => registry.GetContextDescriptor(key);

        bool IsLatestVersion(RegistryKey key) => registry.IsLatestVersion(key);
    }
}
