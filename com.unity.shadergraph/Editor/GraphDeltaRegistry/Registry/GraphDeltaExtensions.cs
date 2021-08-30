using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Registry
{
    //public interface IRegistry
    //{
    //    IEnumerable<RegistryKey> BrowseRegistryKeys();
    //    Defs.INodeDefinitionBuilder GetBuilder(RegistryKey key);
    //    RegistryFlags GetFlags(RegistryKey key);
    //    GraphDelta.INodeReader GetDefaultTopology(RegistryKey key);
    //    bool RegisterNodeBuilder<T>() where T : Defs.INodeDefinitionBuilder;
    //    Defs.INodeDefinitionBuilder ResolveBuilder<T>() where T : Defs.INodeDefinitionBuilder;
    //    RegistryKey ResolveKey<T>() where T : Defs.IRegistryEntry;
    //    RegistryFlags ResolveFlags<T>() where T : Defs.IRegistryEntry;
    //}

    public static class GraphDeltaExtensions
    {

        private const string kRegistryKeyName = "_RegistryKey";
        public static RegistryKey GetRegistryKey(this GraphDelta.INodeReader node)
        {
            node.TryGetField(kRegistryKeyName, out var fieldReader);
            fieldReader.TryGetValue<RegistryKey>(out var key);
            return key;
        }

        public static RegistryKey GetRegistryKey(this GraphDelta.IPortReader port)
        {
            port.TryGetField(kRegistryKeyName, out var fieldReader);
            fieldReader.TryGetValue<RegistryKey>(out var key);
            return key;
        }

        public static RegistryKey GetRegistryKey(this GraphDelta.IFieldReader field)
        {
            field.GetField(kRegistryKeyName, out RegistryKey key);
            return key;

        }

        public static void SetupContext(this GraphDelta.IGraphHandler handler, IEnumerable<Defs.IContextDescriptor> contexts, Registry registry)
        {
            GraphDelta.INodeWriter previousContext = null;
            foreach(var context in contexts)
            {
                var node = handler.AddNode<Defs.ContextBuilder>(context.GetRegistryKey().Name + "_Context", registry);
                node.SetField("_contextDescriptor", context.GetRegistryKey());
                registry.GetNodeBuilder(Defs.ContextBuilder.kRegistryKey).BuildNode(null, node, registry);
            }
        }

        public static bool TestConnection(this GraphDelta.IGraphHandler handler, string srcNode, string srcPort, string dstNode, string dstPort, Registry registry)
        {
            var dstNodeReader = handler.GetNodeReader(dstNode);
            dstNodeReader.TryGetPort(dstPort, out var dstPortReader);
            handler.GetNodeReader(srcNode).TryGetPort(srcPort, out var srcPortReader);
            return registry.CastExists(dstPortReader.GetRegistryKey(), srcPortReader.GetRegistryKey());
        }

        public static bool TryConnect(this GraphDelta.IGraphHandler handler, string srcNode, string srcPort, string dstNode, string dstPort, Registry registry)
        {
            var dstNodeWriter = handler.GetNodeWriter(dstNode);
            dstNodeWriter.TryGetPort(dstPort, out var dstPortWriter);
            handler.GetNodeWriter(srcNode).TryGetPort(srcPort, out var srcPortWriter);
            return dstPortWriter.TryAddConnection(srcPortWriter);
        }


        public static void SetPortField<T>(this GraphDelta.INodeWriter node, string portName, string fieldName, T value)
        {
            if (!node.TryGetPort(portName, out var pw))
                node.TryAddPort(portName, true, true, out pw);

            pw.SetField(fieldName, value);
        }


        public static void SetField<T>(this GraphDelta.INodeWriter node, string fieldName, T value)
        {
            GraphDelta.IFieldWriter<Box<T>> fieldWriter;
            if (!node.TryGetField(fieldName, out fieldWriter))
                node.TryAddField(fieldName, out fieldWriter);
            fieldWriter.TryWriteData(new Box<T> { data = value });
        }
        public static void SetField<T>(this GraphDelta.IPortWriter port, string fieldName, T value)
        {
            GraphDelta.IFieldWriter<Box<T>> fieldWriter;
            if (!port.TryGetField(fieldName, out fieldWriter))
                port.TryAddField(fieldName, out fieldWriter);
            fieldWriter.TryWriteData(new Box<T> { data = value });
        }
        public static void SetField<T>(this GraphDelta.IFieldWriter field, string fieldName, T value)
        {
            GraphDelta.IFieldWriter<Box<T>> fieldWriter;
            if (!field.TryGetSubField(fieldName, out fieldWriter))
                field.TryAddSubField(fieldName, out fieldWriter);
            fieldWriter.TryWriteData(new Box<T> { data = value });
        }

        public static bool GetField<T>(this GraphDelta.INodeReader node, string fieldName, out T value)
        {
            node.TryGetField(fieldName, out var fieldReader);
            bool result = fieldReader.TryGetValue<Box<T>>(out var boxedValue);
            value = boxedValue.data;
            return result;
        }
        public static bool GetField<T>(this GraphDelta.IPortReader port, string fieldName, out T value)
        {
            port.TryGetField(fieldName, out var fieldReader);
            bool result = fieldReader.TryGetValue<Box<T>>(out var boxedValue);
            value = boxedValue.data;
            return result;
        }
        public static bool GetField<T>(this GraphDelta.IFieldReader field, string fieldName, out T value)
        {
            field.TryGetSubField(fieldName, out var fieldReader);
            bool result = fieldReader.TryGetValue<Box<T>>(out var boxedValue);
            value = boxedValue.data;
            return result;
        }

        public static GraphDelta.IPortWriter AddPort<T>(this GraphDelta.INodeWriter node, GraphDelta.INodeReader userData, string name, bool isInput, Registry registry) where T : Defs.ITypeDefinitionBuilder
        {
            return AddPort(node, userData, name, isInput, Registry.ResolveKey<T>(), registry);
        }

        public static GraphDelta.IPortWriter AddPort(this GraphDelta.INodeWriter node, GraphDelta.INodeReader userData, string name, bool isInput, RegistryKey key, Registry registry)
        {
            node.TryAddPort(name, isInput, true, out var portWriter);
            portWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var fieldWriter);
            fieldWriter.TryWriteData(key);
            userData.TryGetPort(name, out var userPort);

            var builder = registry.GetTypeBuilder(key);
            builder.BuildType((GraphDelta.IFieldReader)userPort, (GraphDelta.IFieldWriter)portWriter, registry);
            return portWriter;
        }
    }
}
