using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    public interface IGraphHandler
    {
        //public TargetRef AddTarget(TargetType targetType)

        //public void RemoveTarget(TargetRef targetRef)

        //public List<TargetSetting> GetTargetSettings(TargetRef targetRef)

        //public NodeRef AddNode(NodeType nodeType)

        //public void RemoveNode(NodeRef nodeRef)

        //public NodeType GetNodeType(NodeRef nodeRef)

        public IEnumerable<NodeRef> GetNodes();

        //public List<PortRef> GetInputPorts(NodeRef nodeRef)

        //public List<PortRef> GetOutputPorts(NodeRef nodeRef)

        //public bool CanConnect(PortRef outputPort, PortRef inputPort)

        //public ConnectionRef Connect(PortRef outputPort, PortRef inputPort)

        //public ConnectionRef ForceConnect(PortRef outputPort, PortRef inputPort)

        //public List<ConnectionRef> GetConnections(PortRef portRef)

        //public void RemoveConnection(ConnectionRef connectionRef)
    }
}
