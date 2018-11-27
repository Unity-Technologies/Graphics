using System;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Graphing
{
    interface IGraph : IOnAssetEnabled
    {
        IEnumerable<T> GetNodes<T>() where T : INode;
        IEnumerable<ShaderEdge> edges { get; }
        void AddNode(INode node);
        void RemoveNode(INode node);
        ShaderEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef);
        void RemoveEdge(ShaderEdge e);
        void RemoveElements(IEnumerable<INode> nodes, IEnumerable<ShaderEdge> edges, IEnumerable<GroupData> groups);
        INode GetNodeFromGuid(Guid guid);
        bool ContainsNodeGuid(Guid guid);
        T GetNodeFromGuid<T>(Guid guid) where T : INode;
        void GetEdges(SlotReference s, List<ShaderEdge> foundEdges);
        void ValidateGraph();
        void ReplaceWith(IGraph other);
        IGraphObject owner { get; set; }
        IEnumerable<INode> addedNodes { get; }
        IEnumerable<INode> removedNodes { get; }
        IEnumerable<ShaderEdge> addedEdges { get; }
        IEnumerable<ShaderEdge> removedEdges { get; }
        IEnumerable<GroupData> groups { get; }
        void ClearChanges();
    }

    static class GraphExtensions
    {
        public static IEnumerable<ShaderEdge> GetEdges(this IGraph graph, SlotReference s)
        {
            var edges = new List<ShaderEdge>();
            graph.GetEdges(s, edges);
            return edges;
        }
    }
}
