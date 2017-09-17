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
}
