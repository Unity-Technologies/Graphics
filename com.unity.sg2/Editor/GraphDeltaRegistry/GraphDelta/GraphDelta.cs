using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEngine;
using static UnityEditor.ShaderGraph.GraphDelta.GraphStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    [Serializable]
    internal sealed class GraphDelta
    {
        public const string k_concrete = "Concrete";
        public const string k_user = "User";

        [SerializeReference]
        internal GraphStorage m_data;

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

        //TODO: Come back to this and decide if this is the best way to handle it.
        internal Action<NodeHandler> onBuild;
        internal void AddBuildCallback(Action<NodeHandler> callback)
        {
            onBuild += callback;
        }

        internal void RemoveBuildCallback(Action<NodeHandler> callback)
        {
            onBuild -= callback;
        }

        private void BuildNode(NodeHandler node, INodeDefinitionBuilder builder, Registry registry)
        {
            node.DefaultLayer = k_concrete;
            builder.BuildNode(node, registry);
            node.DefaultLayer = k_user;
            onBuild?.Invoke(node);
        }

        private List<string> contextNodes;

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

            BuildNode(nodeHandler, builder, registry);

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
            BuildNode(nodeHandler, builder, registry);

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
            BuildNode(nodeHandler, builder, registry);

            return nodeHandler;

        }

        private void HookupToContextList(NodeHandler newContextNode)
        {
            if(contextNodes == null)
            {
                contextNodes = new List<string>();
            }

            if(contextNodes.Count == 0)
            {
                contextNodes.Add(newContextNode.ID.FullPath);
            }
            else
            {
                var last = contextNodes[^1];
                var tailHandler = m_data.GetHandler(last, this, newContextNode.Registry).ToNodeHandler();
                AddEdge(tailHandler.AddPort("Out", false, false).ID, newContextNode.AddPort("In", true, false).ID);
                contextNodes.Add(newContextNode.ID.FullPath);
            }
        }

        public bool ReconcretizeNode(ElementID id, Registry registry, bool propagate = true)
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
            var cpn = nodeHandler.GetField<string>("_CustomizationPointName");
            if (cpn == null) //Is this an old ContextNode?
            {
                nodeHandler.ClearLayerData(k_concrete);
                BuildNode(nodeHandler, builder, registry);
            }

            if (propagate)
            {
                try
                {
                    foreach (var downstream in GetConnectedDownstreamNodes(id, registry).ToList()) //we are modifying the collection, hence .ToList
                    {
                        ReconcretizeNode(downstream.ID, registry);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
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

        public void RemoveNode(ElementID id, Registry registry)
        {
            var node = GetNode(id, registry);
            foreach(var port in node.GetPorts())
            {
                var removedEdges = m_data.edges.Where(e => e.Output.Equals(port.ID) || e.Input.Equals(port.ID));
                foreach(var removedEdge in removedEdges)
                {
                    RemoveEdge(removedEdge.Output, removedEdge.Input, registry);
                }
                if (port.IsInput)
                {
                    var removedDefaults = m_data.defaultConnections.Where(c => c.Input.Equals(port.ID));
                    foreach(var removedDefault in removedDefaults)
                    {
                        RemoveDefaultConnection(removedDefault.Context, removedDefault.Input, registry);
                    }
                }
            }

            // Clear data from concrete layer as well
            node.ClearLayerData(k_concrete);

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
                // TODO (Brett) This is taken out because it was causing loop
                //ReconcretizeNode(port.GetNode().ID, registry);
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
                //ReconcretizeNode(port.GetNode().ID, registry);
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
            if(contextNodes != null)
            {
                foreach(var cn in contextNodes)
                {
                    yield return GetNode(cn, registry);
                }
                yield break;
            }

            NodeHandler step = null;
            foreach(var node in GetNodes(registry))
            {
                if (node.HasMetadata("_contextDescriptor") && node.GetPort("In") == null)
                {
                    step = node;
                    break;
                }
            }

            PortHandler outPort = null;
            contextNodes = new List<string>();
            while (step != null)
            {
                contextNodes.Add(step.ID.FullPath);
                yield return step;

                outPort = step.GetPort("Out");
                step = null;
                if(outPort != null)
                {
                    using(var connections = outPort.GetConnectedPorts().GetEnumerator())
                    {
                        if(connections.MoveNext())
                        {
                            step = connections.Current.GetNode();
                        }
                    }
                }

            }
        }

        internal PortHandler GetDefaultConnection(string contextEntryName, Registry registry)
        {
            foreach (var contextNode in GetContextNodesInOrder(registry))
            {
                var port = contextNode.GetPort($"out_{contextEntryName}");
                if(port != null && !port.IsInput && port.IsHorizontal)
                {
                    return port;
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

    }
}
