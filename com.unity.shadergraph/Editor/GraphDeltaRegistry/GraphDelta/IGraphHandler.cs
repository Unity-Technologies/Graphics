using System.Collections.Generic;
using UnityEditor.ShaderGraph.Registry;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IGraphHandler
    {
        internal INodeWriter AddNode<T>(string name, Registry.Registry registry) where T : Registry.Defs.INodeDefinitionBuilder;
        public INodeWriter AddNode(RegistryKey key, string name, Registry.Registry registry);
        public bool ReconcretizeNode(string name, Registry.Registry registry);
        public INodeReader GetNodeReader(string name);
        public INodeWriter GetNodeWriter(string name);
        public void RemoveNode(string name);
        public IEnumerable<INodeReader> GetNodes();
        public IEnumerable<INodeReader> GetDownstreamNodes(INodeReader sourceNode);

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
