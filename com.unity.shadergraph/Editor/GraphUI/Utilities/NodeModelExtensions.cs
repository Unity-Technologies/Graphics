using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class NodeModelExtensions
    {
        public static IEnumerable<IEdgeModel> GetIncomingEdges(this IPortNodeModel nodeModel)
        {
            return nodeModel
                .Ports
                .Where(port => port.Direction == PortDirection.Input)
                .SelectMany(port => port.GetConnectedEdges());
        }

        public static IEnumerable<IEdgeModel> GetOutgoingEdges(this IPortNodeModel nodeModel)
        {
            return nodeModel
                .Ports
                .Where(port => port.Direction == PortDirection.Output)
                .SelectMany(port => port.GetConnectedEdges());
        }
    }
}
