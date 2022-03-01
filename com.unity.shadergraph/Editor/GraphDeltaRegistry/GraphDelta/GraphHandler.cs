using System.Collections.Generic;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;

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

        public string ToSerializedFormat()
        {
            return EditorJsonUtility.ToJson(graphDelta.m_data, true);
        }

        internal NodeHandler AddNode<T>(string name, Registry.Registry registry) where T : Registry.Defs.INodeDefinitionBuilder
        {
            return graphDelta.AddNode(Registry.Registry.ResolveKey<T>(), name, registry);
        }

        public NodeHandler AddNode(RegistryKey key, string name, Registry.Registry registry) => graphDelta.AddNode(key, name, registry);
        public NodeHandler AddContextNode(RegistryKey key, Registry.Registry registry) => graphDelta.AddContextNode(key, registry);
        public bool ReconcretizeNode(string name, Registry.Registry registry) => graphDelta.ReconcretizeNode(name, registry);
        public NodeHandler GetNodeReader(string name) => GetNode(name);
        public NodeHandler GetNodeWriter(string name) => GetNode(name);

        public NodeHandler GetNode(string name) => graphDelta.GetNode(name);
        public IEdgeHandler AddEdge(ElementID portA, ElementID portB) => graphDelta.AddEdge(portA, portB);

        public void RemoveNode(string name) => graphDelta.RemoveNode(name);
        public IEnumerable<NodeHandler> GetNodes() => graphDelta.GetNodes();

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
