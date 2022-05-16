using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    internal sealed class GraphDelta
    {
        public const string k_concrete = "Concrete";
        public const string k_user = "User";

        internal readonly GraphStorage m_data;

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

            var nodeHandler = m_data.AddNodeHandler(k_user, id, this, registry);
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
            var nodeHandler = m_data.AddNodeHandler(k_user, contextDescriptorKey.Name, this, registry);
            var contextKey = Registry.ResolveKey<ContextBuilder>();
            var builder = registry.GetNodeBuilder(contextKey);

            nodeHandler.SetMetadata("_contextDescriptor", contextDescriptorKey);

            nodeHandler.SetMetadata(kRegistryKeyName, contextKey);

            // Type nodes by default should have an output port of their own type.
            if (builder.GetRegistryFlags() == RegistryFlags.Type)
            {
                nodeHandler.AddPort("Out", false, true).SetMetadata(kRegistryKeyName, contextKey);
            }

            HookupToContextList(nodeHandler);
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
                var tailHandler = m_data.GetHandler(last, this, newContextNode.Registry).ToNodeHandler();
                AddEdge(tailHandler.AddPort("Out", false, false).ID, newContextNode.AddPort("In", true, false).ID);
            }
        }

        public bool ReconcretizeNode(ElementID id, Registry registry)
        {
            //temporary workaround for old code
            if(registry == null)
            {
                return true;
            }
            var nodeHandler = m_data.GetHandler(id, this, registry).ToNodeHandler();
            var key = nodeHandler.GetMetadata<RegistryKey>(kRegistryKeyName);
            var builder = registry.GetNodeBuilder(key);
            nodeHandler.ClearLayerData(k_concrete);
            nodeHandler.DefaultLayer = k_concrete;
            builder.BuildNode(nodeHandler, registry);
            foreach(var downstream in GetConnectedDownstreamNodes(id, registry).ToList())//we are modifying the collection, hence .ToList
            {
                ReconcretizeNode(downstream.ID, registry);
            }
            return builder != null;
        }

        public IEnumerable<NodeHandler> GetNodes(Registry registry)
        {
            return m_data.GetNodes(this, registry);
        }

        public NodeHandler GetNode(ElementID id, Registry registry)
        {
            return m_data.GetHandler(id, this, registry).ToNodeHandler();
        }

        public void RemoveNode(ElementID id)
        {
            m_data.RemoveHandler(k_user, id);
        }

        public EdgeHandler AddEdge(ElementID output, ElementID input)
        {
            m_data.edges.Add(new Edge(output, input));
            return new EdgeHandler(output, input, this, null);
        }


        public EdgeHandler AddEdge(ElementID output, ElementID input, Registry registry)
        {
            m_data.edges.Add(new Edge(output, input));
            PortHandler port = new PortHandler(input, this, registry);
            ReconcretizeNode(port.GetNode().ID, registry);
            return new EdgeHandler(output, input, this, registry);
        }

        public void RemoveEdge(ElementID output, ElementID input)
        {
            m_data.edges.RemoveAll(e => e.Output.Equals(output) && e.Input.Equals(input));
        }


        public void RemoveEdge(ElementID output, ElementID input, Registry registry)
        {
            m_data.edges.RemoveAll(e => e.Output.Equals(output) && e.Input.Equals(input));
            PortHandler port = new PortHandler(input, this, registry);
            ReconcretizeNode(port.GetNode().ID, registry);
        }

        internal IEnumerable<NodeHandler> GetConnectedDownstreamNodes(ElementID node, Registry registry)
        {
            var nodeHandler = m_data.GetHandler(node, this, registry)?.ToNodeHandler();
            if (nodeHandler != null)
            {
                foreach (var port in nodeHandler.GetPorts())
                {
                    if (!port.IsInput)
                    {
                        foreach (var connected in GetConnectedPorts(port.ID, registry))
                        {
                            yield return connected.GetNode();
                        }
                    }
                }
            }

        }

        public IEnumerable<NodeHandler> GetConnectedNodes(ElementID node, Registry registry)
        {
            var nodeHandler = m_data.GetHandler(node, this, registry)?.ToNodeHandler();
            if (nodeHandler != null)
            {
                foreach (var port in nodeHandler.GetPorts())
                {
                    foreach(var connected in GetConnectedPorts(port.ID, registry))
                    {
                        yield return connected.GetNode();
                    }
                }
            }
        }

        public IEnumerable<PortHandler> GetConnectedPorts(ElementID port, Registry registry)
        {
            bool isInput = m_data.GetMetadata<bool>(port, PortHeader.kInput);
            foreach(var edge in m_data.edges)
            {
                if(isInput && edge.Input.Equals(port))
                {
                    yield return new PortHandler(edge.Output, this, registry);
                }
                else if (!isInput && edge.Output.Equals(port))
                {
                    yield return new PortHandler(edge.Input, this, registry);
                }

            }
        }

        public void RemoveNode(string id)
        {
            m_data.RemoveHandler(id);
        }

    }
}
