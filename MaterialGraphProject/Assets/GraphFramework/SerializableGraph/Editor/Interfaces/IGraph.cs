using System;
using System.Collections.Generic;

namespace UnityEngine.Graphing
{
    public delegate void OnGraphChange(GraphChange change);

    public interface IGraph : IOnAssetEnabled
    {
        IEnumerable<T> GetNodes<T>() where T : INode;
        IEnumerable<IEdge> edges { get; }
        void AddNode(INode node);
        void RemoveNode(INode node);
        IEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef);
        void RemoveEdge(IEdge e);
        void RemoveElements(IEnumerable<INode> nodes, IEnumerable<IEdge> edges);
        INode GetNodeFromGuid(Guid guid);
        bool ContainsNodeGuid(Guid guid);
        T GetNodeFromGuid<T>(Guid guid) where T : INode;
        IEnumerable<IEdge> GetEdges(SlotReference s);
        void ValidateGraph();
        void ReplaceWith(IGraph other);
        OnGraphChange onChange { get; set; }
        IGraphObject owner { get; set; }
    }
}
