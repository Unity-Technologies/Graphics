using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for graph assets.
    /// </summary>
    public interface IGraphAsset
    {
        /// <summary>
        /// The name of the graph.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// The graph model stored in the asset.
        /// </summary>
        IGraphModel GraphModel { get; }

        /// <summary>
        /// The dirty state of the asset (true if it needs to be saved)
        /// </summary>
        bool Dirty { get; set; }

        /// <summary>
        /// Version tracking for changes occuring externally.
        /// </summary>
        uint Version { get; }

        /// <summary>
        /// Initializes <see cref="GraphModel"/> to a new graph.
        /// </summary>
        /// <param name="stencilType">The type of <see cref="IStencil"/> associated with the new graph.</param>
        void CreateGraph(Type stencilType = null);
    }
}
