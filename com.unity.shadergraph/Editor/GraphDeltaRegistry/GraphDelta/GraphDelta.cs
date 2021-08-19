using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph.Registry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphDelta : IGraphHandler
    {
        internal readonly GraphStorage m_data;

        private const string kRegistryKeyName = "_RegistryKey";
        public GraphDelta()
        {
            m_data = new GraphStorage();
        }

        public INodeWriter AddNode<T>(string name, IRegistry registry) where T : INodeDefinitionBuilder
        {
            var nodeWriter = AddNodeToLayer(GraphStorage.k_user, name);
            var builder = registry.ResolveBuilder<T>();
            var key = builder.GetRegistryKey();

            nodeWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var fieldWriter);
            fieldWriter.TryWriteData(key);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.IsType)
            {
                nodeWriter.AddPort<T>("Out", false, true, registry);
            }

            var nodeReader = GetNodeReader(name);
            var transientWriter = AddNodeToLayer(GraphStorage.k_concrete, name);
            builder.BuildNode(nodeReader, transientWriter, registry);

            return nodeWriter;
        }

        public INodeWriter AddNode(RegistryKey key, string name, IRegistry registry) 
        {
            var nodeWriter = AddNodeToLayer(GraphStorage.k_user, name);
            var builder = registry.GetBuilder(key);

            nodeWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var fieldWriter);
            fieldWriter.TryWriteData(key);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.IsType)
            {
                nodeWriter.TryAddPort("Out", false, true, out var portWriter);
                portWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var portFieldWriter);
                portFieldWriter.TryWriteData(key);
            }

            var nodeReader = GetNodeReader(name);
            var transientWriter = AddNodeToLayer(GraphStorage.k_concrete, name);
            builder.BuildNode(nodeReader, transientWriter, registry);

            return nodeWriter;
        }



        public INodeWriter AddNode(string id)
        {
            return m_data.AddNodeWriterToLayer(GraphStorage.k_user, id);
        }

        internal INodeWriter AddNodeToLayer(string layerName, string id)
        {
            return m_data.AddNodeWriterToLayer(layerName, id);
        }

        public INodeReader GetNodeReader(string id)
        {
            return m_data.GetNodeReader(id);
        }

        public INodeWriter GetNodeWriter(string id)
        {
            return m_data.GetNodeWriterFromLayer(GraphStorage.k_user, id);
        }

        public IEnumerable<INodeReader> GetNodes()
        {
            return m_data.GetNodes();
        }

        
        public void RemoveNode(string id)
        {
            m_data.RemoveNode(id);
        }

        /*
        public void RemoveNode(INodeRef node)
        {
            node.Remove();
        }

        public bool TryMakeConnection(IPortRef output, IPortRef input)
        {
            return m_data.TryConnectPorts(output, input);
        }
        */
    }
}
