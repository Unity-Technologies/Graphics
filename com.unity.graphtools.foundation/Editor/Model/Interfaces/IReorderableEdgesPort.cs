using System;

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
        /// Moves an edge to the first position.
        /// </summary>
        /// <param name="edge">The model of the edge to move.</param>
        void MoveEdgeFirst(IEdgeModel edge);
        /// <summary>
        /// Moves an edge up in the order.
        /// </summary>
        /// <param name="edge">The model of the edge to move.</param>
        void MoveEdgeUp(IEdgeModel edge);
        /// <summary>
        /// Moves an edge down in the order.
        /// </summary>
        /// <param name="edge">The model of the edge to move.</param>
        void MoveEdgeDown(IEdgeModel edge);
        /// <summary>
        /// Moves an edge to the last position.
        /// </summary>
        /// <param name="edge">The model of the edge to move.</param>
        void MoveEdgeLast(IEdgeModel edge);

        /// <summary>
        /// Gets the order of the edge on this port.
        /// </summary>
        /// <param name="edge">The edge for with to get the order.</param>
        /// <returns>The edge order.</returns>
        int GetEdgeOrder(IEdgeModel edge);
    }
}
