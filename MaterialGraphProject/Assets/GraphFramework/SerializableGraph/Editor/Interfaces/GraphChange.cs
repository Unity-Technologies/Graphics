using System;

namespace UnityEditor.Graphing
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
}
