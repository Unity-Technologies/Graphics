using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface to provide the data needed to create a node in a graph.
    /// </summary>
    public interface IGraphNodeCreationData
    {
        /// <summary>
        /// Option used on node creation.
        /// </summary>
        SpawnFlags SpawnFlags { get; }
        /// <summary>
        /// Graph where to create the node.
        /// </summary>
        IGraphModel GraphModel { get; }
        /// <summary>
        /// Position where to create the node.
        /// </summary>
        Vector2 Position { get; }
        /// <summary>
        /// Guid to give to the node on creation.
        /// </summary>
        SerializableGUID Guid { get; }
    }
}
