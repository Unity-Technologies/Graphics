using System.Collections.Generic;
using UnityEditor.ShaderGraph.GraphDelta;

namespace UnityEditor.ShaderGraph.Utils
{
    public enum PropagationDirection
    {
        Upstream,
        Downstream
    }

    public static class GraphTraversalUtils
    {
        public static IEnumerable<INodeReader> TraverseGraphFromNode(GraphHandler activeGraph, INodeReader startingNode, PropagationDirection directionToTraverse)
        {
            var traversedNodes = new List<INodeReader>();
            return traversedNodes;
        }


        public static IEnumerable<INodeReader> GetUpstreamNodes(INodeReader startingNode)
        {
            foreach (var inputPort in startingNode.GetInputPorts())
            {
                foreach (var connectedPort in inputPort.GetConnectedPorts())
                {
                    foreach (var upstreamNode in GetUpstreamNodes(connectedPort.GetNode()))
                        yield return upstreamNode;
                }
            }

            yield return startingNode;
        }

        public static IEnumerable<INodeReader> GetDownstreamNodes(INodeReader startingNode)
        {
            foreach (var inputPort in startingNode.GetOutputPorts())
            {
                foreach (var connectedPort in inputPort.GetConnectedPorts())
                {
                    foreach (var downstreamNodes in GetDownstreamNodes(connectedPort.GetNode()))
                        yield return downstreamNodes;
                }
            }

            yield return startingNode;
        }
    }
}
