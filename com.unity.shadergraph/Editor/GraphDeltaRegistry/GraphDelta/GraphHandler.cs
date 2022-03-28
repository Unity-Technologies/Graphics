using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public class GraphHandler
    {
        internal GraphDelta graphDelta;

        public GraphHandler()
        {
            graphDelta = new GraphDelta();
        }

        public GraphHandler(string serializedData)
        {
            graphDelta = new GraphDelta(serializedData);
        }

        static public GraphHandler FromSerializedFormat(string json)
        {
            return new GraphHandler(json);
        }

        public string ToSerializedFormat()
        {
            return EditorJsonUtility.ToJson(graphDelta.m_data, true);
        }

        internal NodeHandler AddNode<T>(string name, Registry registry) where T : INodeDefinitionBuilder =>
            graphDelta.AddNode<T>(name, registry);

        public NodeHandler AddNode(RegistryKey key, string name, Registry registry) =>
            graphDelta.AddNode(key, name, registry);

        public NodeHandler AddContextNode(RegistryKey key, Registry registry) =>
            graphDelta.AddContextNode(key, registry);

        public bool ReconcretizeNode(string name, Registry registry) =>
            graphDelta.ReconcretizeNode(name, registry);

        [Obsolete("GetNodeReader is obsolete - Use GetNode now", false)]
        public NodeHandler GetNodeReader(string name) =>
            graphDelta.GetNode(name);

        [Obsolete("GetNodeWriter is obselete - Use GetNode now", false)]
        public NodeHandler GetNodeWriter(string name) =>
            graphDelta.GetNode(name);

        public NodeHandler GetNode(ElementID name) =>
            graphDelta.GetNode(name);

        public void RemoveNode(string name) =>
            graphDelta.RemoveNode(name);

        public IEnumerable<NodeHandler> GetNodes() =>
            graphDelta.GetNodes();

		public EdgeHandler AddEdge(ElementID output, ElementID input) =>
            graphDelta.AddEdge(output, input);

        public void RemoveEdge(ElementID output, ElementID input) =>
            graphDelta.RemoveEdge(output, input);

        public void ReconcretizeAll(Registry registry)
        {
            foreach (var name in GetNodes().Select(e => e.GetName()).ToList())
            {
                var node = GetNodeReader(name);
                if (node != null)
                {
                    var builder = registry.GetNodeBuilder(node.GetRegistryKey());
                    if (builder != null)
                    {
                        if (builder.GetRegistryFlags() == RegistryFlags.Func)
                        {
                            ReconcretizeNode(node.GetName(), registry);
                        }
                    }

                }
            }
        }

        public IEnumerable<PortHandler> GetConnectedPorts(ElementID portID) => graphDelta.GetConnectedPorts(portID);

        public IEnumerable<NodeHandler> GetConnectedNodes(ElementID nodeID) => graphDelta.GetConnectedNodes(nodeID);
        //public TargetRef AddTarget(TargetType targetType)

        //public void RemoveTarget(TargetRef targetRef)

        //public List<TargetSetting> GetTargetSettings(TargetRef targetRef)

        //public INodeWriter AddNode(NodeType nodeType)

        //public void RemoveNode(INodeRef nodeRef);

        //public NodeType GetNodeType(NodeRef nodeRef)

        //public IEnumerable<INodeReader> GetNodes();

        //public IEnumerable<IPortReader> GetOutputPorts(INodeReader nodeRef);

        //public bool CanConnect(PortRef outputPort, PortRef inputPort)

        //public ConnectionRef Connect(PortRef outputPort, PortRef inputPort)

        //public ConnectionRef ForceConnect(PortRef outputPort, PortRef inputPort)

        //public List<ConnectionRef> GetConnections(PortRef portRef)

        //public void RemoveConnection(ConnectionRef connectionRef)
    }
}
