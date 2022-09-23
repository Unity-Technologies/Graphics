using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;

namespace UnityEditor.ShaderGraph.GraphUI
{
    static class NodeModelExtensions
    {
        public static IEnumerable<WireModel> GetIncomingEdges(this PortNodeModel nodeModel)
        {
            return nodeModel
                .Ports
                .Where(port => port.Direction == PortDirection.Input)
                .SelectMany(port => port.GetConnectedWires());
        }

        public static IEnumerable<WireModel> GetOutgoingEdges(this PortNodeModel nodeModel)
        {
            return nodeModel
                .Ports
                .Where(port => port.Direction == PortDirection.Output)
                .SelectMany(port => port.GetConnectedWires());
        }
    }
}
