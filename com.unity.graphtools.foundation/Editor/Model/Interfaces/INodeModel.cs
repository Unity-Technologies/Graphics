using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for a model that represents a node in a graph.
    /// </summary>
    public interface INodeModel : IMovable, IDestroyable
    {
        /// <summary>
        /// Does the node allow to be connected to itself.
        /// </summary>
        bool AllowSelfConnect { get; }

        /// <summary>
        /// Type of the node icon as a string.
        /// </summary>
        string IconTypeString { get; }

        /// <summary>
        /// State of the node model.
        /// </summary>
        ModelState State { get; set; }

        /// <summary>
        /// Tooltip to display.
        /// </summary>
        string Tooltip { get; }

        /// <summary>
        /// Gets all edges connected to this node.
        /// </summary>
        /// <returns>All <see cref="IEdgeModel"/> connected to this node.</returns>
        IEnumerable<IEdgeModel> GetConnectedEdges();

        /// <summary>
        /// Called on creation of the node.
        /// </summary>
        void OnCreateNode();

        /// <summary>
        /// Called on duplication of the node.
        /// </summary>
        /// <param name="sourceNode">Model of the node duplicated.</param>
        void OnDuplicateNode(INodeModel sourceNode);
    }
}
