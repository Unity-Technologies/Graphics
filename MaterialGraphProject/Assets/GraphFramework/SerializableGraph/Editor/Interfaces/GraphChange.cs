using System;

namespace UnityEngine.Graphing
{
    public abstract class GraphChange {}

    public sealed class NodeAddedGraphChange : GraphChange
    {
        public NodeAddedGraphChange(INode node)
        {
            this.node = node;
        }

        public INode node { get; private set; }
    }

    public sealed class NodeRemovedGraphChange : GraphChange
    {
        public NodeRemovedGraphChange(INode node)
        {
            this.node = node;
        }

        public INode node { get; private set; }
    }

    public sealed class EdgeAddedGraphChange : GraphChange
    {
        public EdgeAddedGraphChange(IEdge edge)
        {
            this.edge = edge;
        }

        public IEdge edge { get; private set; }
    }

    public sealed class EdgeRemovedGraphChange : GraphChange
    {
        public EdgeRemovedGraphChange(IEdge edge)
        {
            this.edge = edge;
        }

        public IEdge edge { get; private set; }
    }

    public static class GraphChangeExtensions
    {
        public static void Match(this GraphChange change,
            Action<NodeAddedGraphChange> nodeAdded = null,
            Action<NodeRemovedGraphChange> nodeRemoved = null,
            Action<EdgeAddedGraphChange> edgeAdded = null,
            Action<EdgeRemovedGraphChange> edgeRemoved = null)
        {
            if (change is NodeAddedGraphChange && nodeAdded != null)
                nodeAdded((NodeAddedGraphChange)change);
            else if (change is NodeRemovedGraphChange && nodeRemoved != null)
                nodeRemoved((NodeRemovedGraphChange)change);
            else if (change is EdgeAddedGraphChange && edgeAdded != null)
                edgeAdded((EdgeAddedGraphChange)change);
            else if (change is EdgeRemovedGraphChange && edgeRemoved != null)
                edgeRemoved((EdgeRemovedGraphChange)change);
        }
    }
}
