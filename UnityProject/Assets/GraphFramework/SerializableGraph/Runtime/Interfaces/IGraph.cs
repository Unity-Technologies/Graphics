using System;
using System.Collections.Generic;

namespace UnityEngine.Graphing
{
    public interface IGraph
    {
        IEnumerable<INode> nodes { get; }
        IEnumerable<IEdge> edges { get; }
        void AddNode(INode node);
        void RemoveNode(INode node);
        IEdge Connect(SlotReference fromSlotRef, SlotReference toSlotRef);
        void RemoveEdge(IEdge e);
        void RemoveElements(IEnumerable<INode> nodes, IEnumerable<IEdge> edges);
        INode GetNodeFromGuid(Guid guid);
        IEnumerable<IEdge> GetEdges(SlotReference s);
        void ValidateGraph();
    }
}
