using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Registry
{
    public interface IRegistry
    {
        IEnumerable<RegistryKey> BrowseRegistryKeys();
        INodeDefinitionBuilder GetBuilder(RegistryKey key);
        RegistryFlags GetFlags(RegistryKey key);
        GraphDelta.INodeReader GetDefaultTopology(RegistryKey key);
        bool RegisterNodeBuilder<T>() where T : INodeDefinitionBuilder;
        INodeDefinitionBuilder ResolveBuilder<T>() where T : INodeDefinitionBuilder;
        RegistryKey ResolveKey<T>() where T : INodeDefinitionBuilder;
        RegistryFlags ResolveFlags<T>() where T : INodeDefinitionBuilder;
    }

    public interface INodeDefinitionBuilder
    {
        RegistryKey GetRegistryKey();
        RegistryFlags GetRegistryFlags();
        void BuildNode(GraphDelta.INodeReader userData, GraphDelta.INodeWriter concreteData, IRegistry registry);
        bool CanAcceptConnection(GraphDelta.INodeReader thisNode, GraphDelta.IPortReader thisPort, GraphDelta.IPortReader candidatePort);
    }

    public static class GraphDeltaExtensions
    {
        private const string kRegistryKeyName = "_RegistryKey";

        public static RegistryKey GetRegistryKey(this GraphDelta.INodeReader node)
        {
            node.TryGetField(kRegistryKeyName, out var fieldReader);
            fieldReader.TryGetValue<RegistryKey>(out var key);
            return key;
        }

        public static RegistryKey GetRegistryKey(this GraphDelta.IPortReader node)
        {
            node.TryGetField(kRegistryKeyName, out var fieldReader);
            fieldReader.TryGetValue<RegistryKey>(out var key);
            return key;
        }

        public static GraphDelta.INodeWriter AddNode<T>(this GraphDelta.IGraphHandler handler, string name, IRegistry registry) where T : INodeDefinitionBuilder
        {
            var nodeWriter = handler.AddNode(name);
            var nodeReader = handler.GetNode(name);
            var builder = registry.ResolveBuilder<T>();
            var key = builder.GetRegistryKey();

            builder.BuildNode(nodeReader, nodeWriter, registry);
            nodeWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var fieldWriter);
            fieldWriter.TryWriteData(key);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.IsType)
            {
                nodeWriter.AddPort<T>("Out", false, true, registry);
            }

            return nodeWriter;
        }

        public static GraphDelta.IPortWriter AddPort<T>(this GraphDelta.INodeWriter nodeWriter, string name, bool isInput, bool isHorz, IRegistry registry) where T : INodeDefinitionBuilder
        {
            nodeWriter.TryAddPort(name, isInput, isHorz, out var portWriter);
            portWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var fieldWriter);
            fieldWriter.TryWriteData(registry.ResolveKey<T>());
            return portWriter;
        }

        public static bool TestConnection(this GraphDelta.IGraphHandler handler, string dstNode, string dstPort, string srcNode, string srcPort, IRegistry registry)
        {
            var dstNodeReader = handler.GetNode(dstNode);
            dstNodeReader.TryGetPort(dstPort, out var dstPortReader);
            handler.GetNode(srcNode).TryGetPort(srcPort, out var srcPortReader);
            return dstNodeReader.TestConnection(dstPortReader, srcPortReader, registry);
        }

        public static bool TestConnection(this GraphDelta.INodeReader dstNode, GraphDelta.IPortReader dstPort, GraphDelta.IPortReader srcPort, IRegistry registry)
        {
            if (srcPort.GetFlags().isInput != dstPort.GetFlags().isInput)
            {
                var key = dstNode.GetRegistryKey();
                var builder = registry.GetBuilder(key);
                return builder.CanAcceptConnection(dstNode, dstPort, srcPort);
            }
            return false;
        }

        public static bool TryConnect(this GraphDelta.IGraphHandler handler, string dstNode, string dstPort, string srcNode, string srcPort, IRegistry registry)
        {
            var dstNodeWriter = handler.GetNodeWriter(dstNode);
            dstNodeWriter.TryGetPort(dstPort, out var dstPortWriter);
            handler.GetNodeWriter(srcNode).TryGetPort(srcPort, out var srcPortWriter);
            return dstPortWriter.TryAddConnection(srcPortWriter);
        }

        public static void SetField<T>(this GraphDelta.INodeWriter node, string fieldName, T value)
        {
            node.TryAddField<Box<T>>(fieldName, out var fieldWriter);
            fieldWriter.TryWriteData(new Box<T> { data = value });
        }
        public static void SetField<T>(this GraphDelta.IPortWriter port, string fieldName, T value)
        {
            port.TryAddField<Box<T>>(fieldName, out var fieldWriter);
            fieldWriter.TryWriteData(new Box<T> { data = value });
        }
    }
}
