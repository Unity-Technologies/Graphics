using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Implements a title and a display title.
    /// </summary>
    public interface IHasTitle
    {
        /// <summary>
        /// Title of the declaration model.
        /// </summary>
        string Title { get; set; }

        /// <summary>
        /// Version of the title to display.
        /// </summary>
        string DisplayTitle { get; }
    }

    /// <summary>
    /// Interface for elements that can track their progression.
    /// </summary>
    public interface IHasProgress
    {
        /// <summary>
        /// Whether the element have some way to track progression.
        /// </summary>
        bool HasProgress { get; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be collapsed.
    /// </summary>
    public interface ICollapsible
    {
        /// <summary>
        /// Whether the element is collapsed.
        /// </summary>
        bool Collapsed { get; set; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be resized.
    /// </summary>
    public interface IResizable
    {
        /// <summary>
        /// The position and size of the element.
        /// </summary>
        Rect PositionAndSize { get; set; }
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be moved.
    /// </summary>
    public interface IMovable
    {
        /// <summary>
        /// The position of the element.
        /// </summary>
        Vector2 Position { get; set; }

        /// <summary>
        /// Moves the element.
        /// </summary>
        /// <param name="delta">The amount of the move in the x and y directions.</param>
        void Move(Vector2 delta);
    }

    // TODO Consider moving this functionality to GraphElement since we have capabilities to gate the action.
    /// <summary>
    /// An element that can be renamed.
    /// </summary>
    public interface IRenamable
    {
        /// <summary>
        /// Change the name of the declaration model.
        /// </summary>
        /// <param name="name">New name to give to the model.</param>
        void Rename(string name);
    }

    /// <summary>
    /// Interface for temporary edges.
    /// </summary>
    public interface IGhostEdge : IEdgeModel
    {
        /// <summary>
        /// The position of the end of the edge.
        /// </summary>
        Vector2 EndPoint { get; }
    }
}
