using System.Collections.Generic;
using UnityEditor.ShaderGraph.Registry;
using System.Linq;
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

        static public GraphHandler FromSerializedFormat(string json)
        {
            return new GraphHandler(json);
        }

        public string ToSerializedFormat()
        {
            return EditorJsonUtility.ToJson(graphDelta.m_data, true);
        }

        internal INodeWriter AddNode<T>(string name, Registry.Registry registry) where T : Registry.Defs.INodeDefinitionBuilder => graphDelta.AddNode<T>(name, registry);
        public INodeWriter AddNode(RegistryKey key, string name, Registry.Registry registry) => graphDelta.AddNode(key, name, registry);
        public INodeWriter AddContextNode(RegistryKey key, Registry.Registry registry) => graphDelta.AddContextNode(key, registry);
        public bool ReconcretizeNode(string name, Registry.Registry registry) => graphDelta.ReconcretizeNode(name, registry);
        public INodeReader GetNodeReader(string name) => graphDelta.GetNodeReader(name);
        public INodeWriter GetNodeWriter(string name) => graphDelta.GetNodeWriter(name);
        public void RemoveNode(string name) => graphDelta.RemoveNode(name);
        public IEnumerable<INodeReader> GetNodes() => graphDelta.GetNodes();
        public void ReconcretizeAll(Registry.Registry registry)
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
