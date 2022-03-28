using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;
using System.Linq;

namespace UnityEditor.ShaderGraph.Utils
{
    public enum PropagationDirection
    {
        Upstream,
        Downstream
    }

    public static class GraphTraversalUtils
    {
        public static IEnumerable<NodeHandler> TraverseGraphFromNode(GraphHandler activeGraph, NodeHandler startingNode, PropagationDirection directionToTraverse)
        {
            var traversedNodes = new List<NodeHandler>();
            return traversedNodes;
        }


        public static IEnumerable<NodeHandler> GetUpstreamNodes(NodeHandler startingNode)
        {
            foreach (var inputPort in startingNode.GetPorts().Where(e => e.IsInput))
            {
                foreach (var connectedPort in inputPort.GetConnectedPorts())
                {
                    var connectedNodePath = connectedPort.ID.FullPath.Replace("." + connectedPort.ID.LocalPath, "");
                    var connectedNode = new NodeHandler(connectedNodePath, connectedPort.Owner);
                    foreach (var upstreamNode in GetUpstreamNodes(connectedNode))
                        yield return upstreamNode;
                }
            }

            yield return startingNode;
        }

        public static IEnumerable<NodeHandler> GetDownstreamNodes(NodeHandler startingNode)
        {
            foreach (var inputPort in startingNode.GetPorts().Where(e => !e.IsInput))
            {
                foreach (var connectedPort in inputPort.GetConnectedPorts())
                {
                    var connectedNodePath = connectedPort.ID.FullPath.Replace("." + connectedPort.ID.LocalPath, "");
                    var connectedNode = new NodeHandler(connectedNodePath, connectedPort.Owner);
                        foreach (var downstreamNodes in GetDownstreamNodes(connectedNode))
                        yield return downstreamNodes;
                }
            }

            yield return startingNode;
        }
    }
}
