using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for graph templates.
    /// </summary>
    public interface IGraphTemplate
    {
        /// <summary>
        /// The stencil type.
        /// </summary>
        Type StencilType { get; }

        /// <summary>
        /// Callback to initialize a new graph. Implement this method to add nodes and edges to new graphs.
        /// </summary>
        /// <param name="graphModel">The graph model to initialize.</param>
        void InitBasicGraph(IGraphModel graphModel);

        /// <summary>
        /// The graph type name.
        /// </summary>
        string GraphTypeName { get; }

        /// <summary>
        /// Default name for the graph asset.
        /// </summary>
        string DefaultAssetName { get; }
    }
}
