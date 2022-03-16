using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphDelta
    {
        internal readonly GraphStorage m_data;
        private readonly List<string> contextNodes = new();
        private const string kRegistryKeyName = "_RegistryKey";

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

        public GraphDelta()
        {
            m_data = new GraphStorage();
        }

        public GraphDelta(string serializedData) : this()
        {
            EditorJsonUtility.FromJsonOverwrite(serializedData, m_data);
        }

        public string ToSerializedFormat()
        {
            return EditorJsonUtility.ToJson(m_data);
        }

        internal INodeWriter AddNode<T>(string name, Registry registry)  where T : INodeDefinitionBuilder
        {
            var key = Registry.ResolveKey<T>();
            return AddNode(key, name, registry);
        }

        public INodeWriter AddNode(RegistryKey key, string name, Registry registry)
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

        public INodeWriter AddContextNode(RegistryKey contextDescriptorKey, Registry registry)
        {
            var nodeWriter = AddNodeToLayer(GraphStorage.k_user, contextDescriptorKey.Name);
            var contextKey = Registry.ResolveKey<ContextBuilder>();
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

        public void SetupContextNodes(IEnumerable<IContextDescriptor> contextDescriptors, Registry registry)
        {
            foreach(var descriptor in contextDescriptors)
            {
                AppendContextBlockToStage(descriptor, registry);
            }
        }

        public void AppendContextBlockToStage(IContextDescriptor contextDescriptor, Registry registry)
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

        public bool ReconcretizeNode(string name, Registry registry)
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
            return m_data.GetAllChildReaders().Where(
                e => e != null && m_data.GetNodeReader(e.GetName()) != null
            );
        }

        public void RemoveNode(string id)
        {
            m_data.RemoveNode(id);
        }
    }
}
