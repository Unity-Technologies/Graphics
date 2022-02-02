using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphDelta : IGraphHandler
    {
        internal readonly GraphStorage m_data;

        public IEnumerable<INodeReader> ContextNodes
        {
            get
            {
                foreach(string name in contextNodes)
                {
                    yield return m_data.GetNodeReader(name);
                }
            }
        }

        private const string kRegistryKeyName = "_RegistryKey";
        public GraphDelta()
        {
            m_data = new GraphStorage();
        }

        private List<string> contextNodes = new List<string>();

         INodeWriter IGraphHandler.AddNode<T>(string name, Registry.Registry registry) // where T : Registry.Defs.INodeDefinitionBuilder
        {
            var key = Registry.Registry.ResolveKey<T>();
            return AddNode(key, name, registry);
        }

        public INodeWriter AddNode(RegistryKey key, string name, Registry.Registry registry)
        {
            var builder = registry.GetNodeBuilder(key);
            if (builder is ContextBuilder cb)
            {
                return AddContextNode(key, registry);
            }

            var nodeWriter = AddNodeToLayer(GraphStorage.k_user, name);
            nodeWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var fieldWriter);
            fieldWriter.TryWriteData(key);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.Type)
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

        public INodeWriter AddContextNode(RegistryKey contextDescriptorKey, Registry.Registry registry)
        {
            var nodeWriter = AddNodeToLayer(GraphStorage.k_user, contextDescriptorKey.Name);
            var contextKey = Registry.Registry.ResolveKey<ContextBuilder>();
            var builder = registry.GetNodeBuilder(contextKey);

            nodeWriter.TryAddField<RegistryKey>("_contextDescriptor", out var contextWriter);
            contextWriter.TryWriteData(contextDescriptorKey);

            nodeWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var fieldWriter);
            fieldWriter.TryWriteData(contextKey);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.Type)
            {
                nodeWriter.TryAddPort("Out", false, true, out var portWriter);
                portWriter.TryAddField<RegistryKey>(kRegistryKeyName, out var portFieldWriter);
                portFieldWriter.TryWriteData(contextKey);
            }

            var nodeReader = GetNodeReader(contextDescriptorKey.Name);
            var transientWriter = AddNodeToLayer(GraphStorage.k_concrete, contextDescriptorKey.Name);
            builder.BuildNode(nodeReader, transientWriter, registry);

            return nodeWriter;

        }

        public void SetupContextNodes(IEnumerable<IContextDescriptor> contextDescriptors, Registry.Registry registry)
        {
            foreach(var descriptor in contextDescriptors)
            {
                AppendContextBlockToStage(descriptor, registry);
            }
        }

        public void AppendContextBlockToStage(IContextDescriptor contextDescriptor, Registry.Registry registry)
        {
            var contextNodeWriter = AddContextNode(contextDescriptor.GetRegistryKey(), registry);

            HookupToContextList(contextNodeWriter, contextDescriptor.GetRegistryKey().Name);
            ReconcretizeNode(contextDescriptor.GetRegistryKey().Name, registry);
        }

        private void HookupToContextList(INodeWriter newContextNode, string name)
        {
            if(contextNodes.Count == 0)
            {
                contextNodes.Add(name);
            }
            else
            {
                var last = contextNodes[contextNodes.Count - 1];
                var tailWriter = GetNodeWriter(last);
                if(!tailWriter.TryGetPort("Out", out var lastOut))
                {
                    tailWriter.TryAddPort("Out", false, false, out lastOut);
                }
                newContextNode.TryAddPort("In", true, false, out var newLastIn);

                lastOut.TryAddConnection(newLastIn);

            }
        }

        public bool ReconcretizeNode(string name, Registry.Registry registry)
        {
            var nodeReader = GetNodeReader(name);
            var key = nodeReader.GetRegistryKey();
            var builder = registry.GetNodeBuilder(key);

            var transientWriter = GetNodeFromLayer(GraphStorage.k_concrete, name);
            if(transientWriter != null)
            {
                transientWriter.TryRemove();
            }
            transientWriter = AddNodeToLayer(GraphStorage.k_concrete, name);


            builder.BuildNode(nodeReader, transientWriter, registry);
            return builder != null;
        }



        public INodeWriter AddNode(string id)
        {
            return m_data.AddNodeWriterToLayer(GraphStorage.k_user, id);
        }

        internal INodeWriter AddNodeToLayer(string layerName, string id)
        {
            return m_data.AddNodeWriterToLayer(layerName, id);
        }

        internal INodeWriter GetNodeFromLayer(string layerName, string id)
        {
            return m_data.GetNodeWriterFromLayer(layerName, id);
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
            throw new System.Exception();
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
