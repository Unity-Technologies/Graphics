namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for the model of a port that can have reorderable edges.
    /// </summary>
    public interface IReorderableEdgesPortModel : IPortModel
    {
        /// <summary>
        /// Gets whether this port model has reorderable edges or not.
        /// </summary>
        bool HasReorderableEdges { get; }

        /// <summary>
        /// Changes the order of an edge among its siblings.
        /// </summary>
        /// <param name="edgeModel">The edge to move.</param>
        /// <param name="reorderType">The type of move to do.</param>
        void ReorderEdge(IEdgeModel edgeModel, ReorderType reorderType);

        /// <summary>
        /// Gets the order of the edge on this port.
        /// </summary>
        /// <param name="edge">The edge for with to get the order.</param>
        /// <returns>The edge order.</returns>
        int GetEdgeOrder(IEdgeModel edge);
    }
}
