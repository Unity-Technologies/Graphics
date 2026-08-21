

namespace Unity.GraphCommon.LowLevel.Editor
{
    /*public*/ partial class GraphTraverser
    {
        /// <summary>
        /// Delegate called when visiting a node.
        /// </summary>
        /// <typeparam name="TNode">The node type being visited (<see cref="DataNode"/> or <see cref="TaskNode"/>).</typeparam>
        /// <param name="node">The node being visited.</param>
        /// <returns>true to continue traversal, false to stop.</returns>
        public delegate bool OnVisitNode<in TNode>(TNode node);

        /// <summary>
        /// Delegate called to filter a node.
        /// </summary>
        /// <typeparam name="TNode">The node type being filtered (<see cref="DataNode"/> or <see cref="TaskNode"/>).</typeparam>
        /// <param name="node">The node being filtered.</param>
        /// <returns>The traversal control used to determine whether to visit the node and/or continue graph traversal.</returns>
        public delegate TraversalControl OnFilterNode<in TNode>(TNode node);
    }
}
