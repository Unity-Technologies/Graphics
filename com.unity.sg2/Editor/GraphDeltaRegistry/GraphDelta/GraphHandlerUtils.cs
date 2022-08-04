using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;

namespace UnityEditor.ShaderGraph.GraphDelta
{
    static class GraphHandlerUtils
    {
        private static void TopoSort(
            GraphHandler graph,
            ElementID nodeID,
            ref HashSet<ElementID> visited,
            ref List<ElementID> result,
            in Dictionary<ElementID, HashSet<ElementID>> dependencyList,
            bool otherContextAreLeaf)
        {
            if (!visited.Contains(nodeID))
                visited.Add(nodeID);
            if (dependencyList.TryGetValue(nodeID, out var upstreamNodeIDs))
            {
                foreach (var upstreamNodeID in upstreamNodeIDs)
                {
                    if (!visited.Contains(upstreamNodeID))
                    {
                        if (otherContextAreLeaf == false || !graph.GetNode(upstreamNodeID).HasMetadata("_contextDescriptor"))
                        {
                            TopoSort(graph, upstreamNodeID, ref visited, ref result, dependencyList, otherContextAreLeaf);
                        }
                        else // add the context node, but don't recurse- we're treating it as a leaf.
                        {
                            // The intended usage of this case assumes that Context Nodes are monadic,
                            // and that you're processing context nodes to generate blocks-- each context node
                            // owns its own subtree/graph evaluation of nodes that feed into it, so we don't
                            // need to know about anything upstream of this.

                            // We still need to include the context node though, since its ports typically represent the
                            // input structure for the previous context node-- see Interpreter.cs:214, EvaluateGraphAndPopulateDescriptors(...)
                            visited.Add(upstreamNodeID);
                            result.Add(upstreamNodeID);
                        }
                    }
                }
            }
            result.Add(nodeID);
        }

        private static ElementID FindContextEntryOwner(GraphHandler graph, string entryName)
        {
            return graph.graphDelta.GetDefaultConnection(entryName, graph.registry)?.GetNode()?.ID ?? "";
        }

        internal static Dictionary<ElementID, HashSet<ElementID>> BuildDependencyList(GraphHandler graph)
        {
            var comparer = new ElementIDComparer();
            Dictionary<ElementID, HashSet<ElementID>> dependencyList = new(comparer);
            foreach (var edge in graph.graphDelta.m_data.edges)
            {
                var key = new ElementID(edge.Input.ParentPath);
                var toAdd = new ElementID(edge.Output.ParentPath);
                if (!dependencyList.ContainsKey(key))
                    dependencyList.Add(key, new(comparer));
                dependencyList[key].Add(toAdd);
            }

            foreach (var edge in graph.graphDelta.m_data.defaultConnections)
            {
                var key = new ElementID(edge.Input.ParentPath);
                var toAdd = FindContextEntryOwner(graph, edge.Context);

                if (toAdd.FullPath == "")
                    continue;

                if (!dependencyList.ContainsKey(key))
                    dependencyList.Add(key, new(comparer));
                dependencyList[key].Add(toAdd);
            }

            return dependencyList;
        }

        internal static List<ElementID> GetUpstreamNodesTopologically(
                GraphHandler graph,
                ElementID node,
                in Dictionary<ElementID, HashSet<ElementID>> depList = null,
                bool otherContextAreLeaf = false
            )
        {
            List<ElementID> result = new();
            HashSet<ElementID> visited = new(new ElementIDComparer());
            var dependencyList = depList ?? BuildDependencyList(graph);

            TopoSort(graph, node, ref visited, ref result, depList, otherContextAreLeaf);
            return result;
        }

        internal static List<ElementID> GetNodesTopologically(
            GraphHandler graph,
            in Dictionary<ElementID, HashSet<ElementID>> depList = null,
            bool otherContextAreLeaf = false)
        {
            List<ElementID> result = new();
            HashSet<ElementID> visited = new(new ElementIDComparer());
            var dependencyList = depList ?? BuildDependencyList(graph);

            foreach (var nodeID in graph.GetNodes().Select(e => e.ID.FullPath))
                if (!visited.Contains(nodeID))
                    TopoSort(graph, nodeID, ref visited, ref result, dependencyList, otherContextAreLeaf);

            return result;
        }
    }
}
