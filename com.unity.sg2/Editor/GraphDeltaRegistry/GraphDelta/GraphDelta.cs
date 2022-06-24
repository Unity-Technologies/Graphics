using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEngine;
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
                return AddContextNode(key.Name, registry);
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

            foreach(var port in nodeHandler.GetPorts())
            {
                if (!port.IsInput || !port.IsHorizontal)
                    continue;

                if(port.HasMetadata(PortHandler.kDefaultConnection))
                {
                    var contextName = port.GetMetadata<string>(PortHandler.kDefaultConnection);
                    var contextConnection = new ContextConnection(contextName, port.ID);
                    m_data.defaultConnections.Add(contextConnection);
                }
            }

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


        public NodeHandler AddContextNode(string name, Registry registry)
        {
            var nodeHandler = m_data.AddNodeHandler(k_user, name, this, registry);
            var contextKey = Registry.ResolveKey<ContextBuilder>();
            var builder = registry.GetNodeBuilder(contextKey);

            nodeHandler.SetMetadata("_contextDescriptor", true);

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
            if (registry == null)
            {
                return true;
            }

            var nodeHandler = m_data.GetHandler(id, this, registry).ToNodeHandler();
            if (nodeHandler == null)
                throw new InvalidOperationException("Failed to retrieve node handle with name: " + id.FullPath);
            var key = nodeHandler.GetMetadata<RegistryKey>(kRegistryKeyName);
            var builder = registry.GetNodeBuilder(key);
            if (!nodeHandler.HasMetadata("_CustomizationPointName"))
            {
                nodeHandler.ClearLayerData(k_concrete);
                nodeHandler.DefaultLayer = k_concrete;
                builder.BuildNode(nodeHandler, registry);
                nodeHandler.DefaultLayer = k_user;
            }

            foreach (var downstream in GetConnectedDownstreamNodes(id, registry).ToList()) //we are modifying the collection, hence .ToList
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
            try
            {
                ReconcretizeNode(port.GetNode().ID, registry);
            }
            catch (Exception e)
            {
                //Not going to change this now, but this should probably be done
                //m_data.edges.Remove(new Edge(output, input));
                Debug.LogException(e);
                Debug.LogError("Failed to add edge.");
            }

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
            try
            {
                ReconcretizeNode(port.GetNode().ID, registry);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError("Failed to remove edge.");
            }
        }

        public void AddDefaultConnection(string contextEntryName, ElementID input, Registry registry)
        {
            var newConnection = new ContextConnection(contextEntryName, input);
            m_data.defaultConnections.Add(newConnection);
            PortHandler port = new PortHandler(input, this, registry);
            try
            {
                ReconcretizeNode(port.GetNode().ID, registry);
            }
            catch (Exception e)
            {
                m_data.defaultConnections.Remove(newConnection);
                Debug.LogException(e);
                Debug.LogError("Failed to add default context connection.");
            }
        }

        public void RemoveDefaultConnection(string contextEntryName, ElementID input, Registry registry)
        {
            var newConnection = new ContextConnection(contextEntryName, input);
            m_data.defaultConnections.Remove(newConnection);
            PortHandler port = new PortHandler(input, this, registry);
            try
            {
                ReconcretizeNode(port.GetNode().ID, registry);
            }
            catch (Exception e)
            {
                m_data.defaultConnections.Add(newConnection);
                Debug.LogException(e);
                Debug.LogError("Failed to remove default context connection.");
            }

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

        private IEnumerable<NodeHandler> GetConnectedNodesInternal(ElementID node, Registry registry, bool includeInputs, bool includeOutputs)
        {
            var nodeHandler = m_data.GetHandler(node, this, registry)?.ToNodeHandler();
            if (nodeHandler != null)
            {
                foreach (var port in nodeHandler.GetPorts())
                {
                    if (includeInputs && port.IsInput)
                    {
                        foreach (var connected in GetConnectedPorts(port.ID, registry))
                        {
                            yield return connected.GetNode();
                        }
                    }
                    else if(includeOutputs && !port.IsInput)
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
            return GetConnectedNodesInternal(node, registry, true, true);
        }

        public IEnumerable<NodeHandler> GetConnectedIncomingNodes(ElementID node, Registry registry)
        {
            return GetConnectedNodesInternal(node, registry, true, false);
        }

        public IEnumerable<PortHandler> GetConnectedPorts(ElementID port, Registry registry)
        {
            bool isInput = m_data.GetMetadata<bool>(port, PortHeader.kInput);
            foreach(var edge in m_data.edges)
            {
                if(isInput && edge.Input.Equals(port))
                {
                    yield return new PortHandler(edge.Output, this, registry);
                    yield break; // only one input connection is allowed - break on the first input
                }
                else if (!isInput && edge.Output.Equals(port))
                {
                    yield return new PortHandler(edge.Input, this, registry);
                }
            }

            foreach(var defConnection in m_data.defaultConnections)
            {
                PortHandler def = GetDefaultConnection(defConnection.Context, registry);
                if (def == null)
                    continue;
                if(isInput && defConnection.Input.Equals(port))
                {
                    yield return def;
                    yield break; // only one input connection is allowed - break on the first input
                }

                if(!isInput && def.Equals(port))
                {
                    //only valid if no other connection exists to this port
                    if(!m_data.edges.Any(e => e.Input.Equals(defConnection.Input)))
                    {
                        yield return new PortHandler(defConnection.Input, this, registry);
                    }
                }
            }
        }

        private IEnumerable<NodeHandler> GetContextNodesInOrder(Registry registry)
        {
            NodeHandler step = null;
            foreach(var node in GetNodes(registry))
            {
                if (node.HasMetadata("_contextDescriptor") && node.GetPort("In") == null)
                {
                    step = node;
                    break;
                }
            }

            while (step != null)
            {
                yield return step;
                step = step.GetPort("Out")?.GetConnectedPorts().First()?.GetNode();
            }
        }

        private PortHandler GetDefaultConnection(string contextEntryName, Registry registry)
        {
            foreach (var contextNode in GetContextNodesInOrder(registry))
            {
                foreach(var port in contextNode.GetPorts())
                {
                    if (!port.IsInput && port.IsHorizontal)
                    {
                        if (port.ID.LocalPath.Equals($"out_{contextEntryName}"))
                        {
                            return port;
                        }
                    }
                }
            }
            return null;
        }

        public NodeHandler DuplicateNode(NodeHandler sourceNode, Registry registry)
        {
            return DuplicateNode(sourceNode, registry, m_data.GetLayerRoot(k_user).GetUniqueLocalID(sourceNode.ID.LocalPath));
        }

        public NodeHandler DuplicateNode(NodeHandler sourceNode, Registry registry, ElementID copiedNodeID)
        {
            NodeHandler output = AddNode(sourceNode.GetRegistryKey(), copiedNodeID, registry);
            m_data.CopyDataBranch(sourceNode, output);
            return output;
        }

        public void RemoveNode(string id)
        {
            m_data.RemoveHandler(id);
        }

    }
}
