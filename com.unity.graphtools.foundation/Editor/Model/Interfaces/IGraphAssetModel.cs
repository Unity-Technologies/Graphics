using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for graph assets.
    /// </summary>
    public interface IGraphAssetModel
    {
        /// <summary>
        /// The name of the graph.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// A version of <see cref="Name"/> usable in C# scripts.
        /// </summary>
        string FriendlyScriptName { get; }

        /// <summary>
        /// The graph model stored in the asset.
        /// </summary>
        IGraphModel GraphModel { get; }

        /// <summary>
        /// The dirty state of the asset (true if it needs to be saved)
        /// </summary>
        bool Dirty { get; set; }

        /// <summary>
        /// Checks whether the graph is a Container Graph or not. If it is not a Container Graph, it is an Asset Graph.
        /// </summary>
        /// <remarks>
        /// A Container Graph is a graph asset that cannot be nested inside of another graph asset, and can be referenced by a game object or scene.
        /// An Asset Graph is a graph asset that can have exposed inputs/outputs, making it so that it can be nested inside of another graph asset, and can be referenced by a game object or scene.
        /// </remarks>
        /// <returns>True if the graph is a container graph, false otherwise.</returns>
        bool IsContainerGraph();

        /// <summary>
        /// Checks the conditions to specify whether the Asset Graph can be a subgraph or not.
        /// </summary>
        /// <remarks>
        /// A subgraph is an Asset Graph that is nested inside of another graph asset, and can be referenced by a game object or scene.
        /// </remarks>
        /// <returns>True if the Asset Graph can be a subgraph, false otherwise.</returns>
        bool CanBeSubgraph();

        /// <summary>
        /// Initializes <see cref="GraphModel"/> to a new graph.
        /// </summary>
        /// <param name="graphName">The name of the graph.</param>
        /// <param name="stencilType">The type of <see cref="IStencil"/> associated with the new graph.</param>
        /// <param name="markAssetDirty">Whether the asset should be marked dirty.</param>
        void CreateGraph(string graphName, Type stencilType = null, bool markAssetDirty = true);

        /// <summary>
        /// Called after an undo or redo was executed.
        /// </summary>
        void UndoRedoPerformed();
    }

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Helper methods for <see cref="IGraphAssetModel"/>.
    /// </summary>
    public static class IGraphAssetModelHelper
    {
        /// <summary>
        /// Creates a new graph asset model.
        /// </summary>
        /// <param name="assetName">The graph asset name.</param>
        /// <param name="assetPath">The graph asset path. If null or empty, the asset will not be written on disk.</param>
        /// <typeparam name="TAsset">The type of graph asset to create.</typeparam>
        /// <returns>A new graph asset.</returns>
        public static TAsset Create<TAsset>(string assetName, string assetPath) where TAsset : class, IGraphAssetModel
        {
            return (TAsset)Create(assetName, assetPath, typeof(TAsset));
        }

        /// <summary>
        /// Creates a new graph asset model.
        /// </summary>
        /// <param name="assetName">The graph asset name.</param>
        /// <param name="assetPath">The graph asset path. If null or empty, the asset will not be written on disk.</param>
        /// <param name="assetTypeToCreate">The type of graph asset to create.</param>
        /// <returns>A new graph asset.</returns>
        public static IGraphAssetModel Create(string assetName, string assetPath, Type assetTypeToCreate)
        {
            var asset = ScriptableObject.CreateInstance(assetTypeToCreate);
            if (!string.IsNullOrEmpty(assetPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "");
                if (File.Exists(assetPath))
                    AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            asset.name = assetName;
            return asset as IGraphAssetModel;
        }
    }
}
