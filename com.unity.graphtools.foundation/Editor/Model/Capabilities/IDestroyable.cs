using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface to mark a graph element as deleted from the graph.
    /// </summary>
    public interface IDestroyable : IGraphElementModel
    {
        /// <summary>
        /// Whether the object was deleted from the graph.
        /// </summary>
        bool Destroyed { get; }

        /// <summary>
        /// Marks the object as being deleted from the graph.
        /// </summary>
        void Destroy();
    }
}
