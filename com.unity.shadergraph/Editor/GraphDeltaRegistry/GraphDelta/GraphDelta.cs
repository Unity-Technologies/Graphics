using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphDelta
    {
        public const string k_concrete = "Concrete";
        public const string k_user = "User";

        internal readonly GraphStorage m_data;
        public IEnumerable<NodeHandler> ContextNodes
        {
            get
            {
                foreach(var id in contextNodes)
                {
                    yield return m_data.GetHandler(id).ToNodeHandler();
                }
            }
        }

        internal const string kRegistryKeyName = "_RegistryKey";
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

        private readonly List<string> contextNodes = new();

        public NodeHandler AddNode<T>(string name, Registry registry)  where T : INodeDefinitionBuilder
        {
           var key = Registry.ResolveKey<T>();
           return AddNode(key, name, registry);
        }

        public NodeHandler AddNode(RegistryKey key, ElementID id, Registry registry)
        {
            var builder = registry.GetNodeBuilder(key);
            if (builder is ContextBuilder)
            {
                return AddContextNode(key, registry);
            }

            var nodeHandler = m_data.AddNodeHandler(k_user, id);
            nodeHandler.SetMetadata(kRegistryKeyName, key);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.Type)
            {
                var portHandler = nodeHandler.AddPort("Out", false, true);
                portHandler.SetMetadata(kRegistryKeyName, key);
            }

            nodeHandler.DefaultLayer = k_concrete;
            builder.BuildNode(nodeHandler, registry);
            nodeHandler.DefaultLayer = k_user;

            return nodeHandler;
        }

        public NodeHandler AddContextNode(RegistryKey contextDescriptorKey, Registry registry)
        {
            var nodeHandler = m_data.AddNodeHandler(k_user, contextDescriptorKey.Name);
            var contextKey = Registry.ResolveKey<ContextBuilder>();
            var builder = registry.GetNodeBuilder(contextKey);

            nodeHandler.SetMetadata("_contextDescriptor", contextDescriptorKey);

            nodeHandler.SetMetadata(kRegistryKeyName, contextKey);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.Type)
            {
                nodeHandler.AddPort("Out", false, true).SetMetadata(kRegistryKeyName, contextKey);
            }

            nodeHandler.DefaultLayer = k_concrete;
            builder.BuildNode(nodeHandler, registry);
            nodeHandler.DefaultLayer = k_user;

            return nodeHandler;

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
            var contextNodeHandler = AddContextNode(contextDescriptor.GetRegistryKey(), registry);

            HookupToContextList(contextNodeHandler);
            ReconcretizeNode(contextNodeHandler.ID, registry);
        }

        private void HookupToContextList(NodeHandler newContextNode)
        {
            if(contextNodes.Count == 0)
            {
                contextNodes.Add(newContextNode.ID.FullPath);
            }
            else
            {
                var last = contextNodes[^1];
                var tailHandler = m_data.GetHandler(last) as NodeHandler;
                tailHandler.AddPort("Out", false, false);
                newContextNode.AddPort("In", true, false);
            }
        }

        public bool ReconcretizeNode(ElementID id, Registry registry)
        {
            var nodeHandler = m_data.GetHandler(id).ToNodeHandler();
            var key = nodeHandler.GetMetadata<RegistryKey>(kRegistryKeyName);
            var builder = registry.GetNodeBuilder(key);
            nodeHandler.ClearLayerData(k_concrete);
            nodeHandler.DefaultLayer = k_concrete;
            builder.BuildNode(nodeHandler, registry);
            foreach(var downstream in GetConnectedDownstreamNodes(id))
            {
                ReconcretizeNode(downstream.ID, registry);
            }
            return builder != null;
        }

        public IEnumerable<NodeHandler> GetNodes()
        {
            return m_data.GetNodes();
        }

        public NodeHandler GetNode(ElementID id)
        {
            return m_data.GetHandler(id).ToNodeHandler();
        }

        public void RemoveNode(ElementID id)
        {
            m_data.RemoveHandler(k_user, id);
        }

        public EdgeHandler AddEdge(ElementID output, ElementID input, Registry registry)
        {
            m_data.edges.Add(new Edge(output, input));
            PortHandler port = new PortHandler(input, m_data);
            ReconcretizeNode(port.GetNode().ID, registry);
            return new EdgeHandler(output, input, m_data);
        }

        public void RemoveEdge(ElementID output, ElementID input, Registry registry)
        {
            m_data.edges.RemoveAll(e => e.Output.Equals(output) && e.Input.Equals(input));
            PortHandler port = new PortHandler(input, m_data);
            ReconcretizeNode(port.GetNode().ID, registry);
        }

        internal IEnumerable<NodeHandler> GetConnectedDownstreamNodes(ElementID node)
        {
            var nodeHandler = m_data.GetHandler(node)?.ToNodeHandler();
            if (nodeHandler != null)
            {
                foreach (var port in nodeHandler.GetPorts())
                {
                    if (!port.IsInput)
                    {
                        foreach (var connected in GetConnectedPorts(port.ID))
                        {
                            yield return connected.GetNode();
                        }
                    }
                }
            }

        }

        public IEnumerable<NodeHandler> GetConnectedNodes(ElementID node)
        {
            var nodeHandler = m_data.GetHandler(node)?.ToNodeHandler();
            if (nodeHandler != null)
            {
                foreach (var port in nodeHandler.GetPorts())
                {
                    foreach(var connected in GetConnectedPorts(port.ID))
                    {
                        yield return connected.GetNode();
                    }
                }
            }
        }

        public IEnumerable<PortHandler> GetConnectedPorts(ElementID port)
        {
            bool isInput = m_data.GetMetadata<bool>(port, PortHeader.kInput);
            foreach(var edge in m_data.edges)
            {
                if(isInput && edge.Input.Equals(port))
                {
                    yield return new PortHandler(edge.Output, m_data);
                }
                else if (!isInput && edge.Output.Equals(port))
                {
                    yield return new PortHandler(edge.Input, m_data);
                }

            }
        }

        public void RemoveNode(string id)
        {
            m_data.RemoveHandler(id);
        }

    }
}
