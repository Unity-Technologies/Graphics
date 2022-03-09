using System;
using System.Linq;
using System.Text;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to load a graph asset.
    /// </summary>
    public class LoadGraphAssetCommand : ICommand
    {
        /// <summary>
        /// The type of loading.
        /// </summary>
        public enum LoadStrategies
        {
            /// <summary>
            /// Clears the history of loaded stack.
            /// </summary>
            Replace,
            /// <summary>
            /// Keeps the history and push the currently loaded graph to it.
            /// </summary>
            PushOnStack,
            /// <summary>
            /// Keeps the history and do not modify it.
            /// </summary>
            KeepHistory
        }

        /// <summary>
        /// The graph asset model to load. If this is null, <see cref="AssetPath"/> will be used.
        /// </summary>
        public readonly IGraphAssetModel Asset;
        /// <summary>
        /// The path of the asset to load.
        /// </summary>
        public readonly string AssetPath;
        /// <summary>
        /// The GameObject to which to bind the graph.
        /// </summary>
        public readonly GameObject BoundObject;
        /// <summary>
        /// The type of loading. Affects the stack of loaded assets.
        /// </summary>
        public readonly LoadStrategies LoadStrategy;
        /// <summary>
        /// The sub-asset file id. If 0, the first GraphAssetModel found in the file will be loaded.
        /// </summary>
        public readonly long FileId;
        /// <summary>
        /// The plugin repository.
        /// </summary>
        public readonly PluginRepository PluginRepository;
        /// <summary>
        /// The index at which the history should be truncated.
        /// </summary>
        public readonly int TruncateHistoryIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadGraphAssetCommand"/> class.
        /// </summary>
        /// <param name="assetPath">The path of the asset to load.</param>
        /// <param name="assetLocalId">The subasset file id. If 0, the first GraphAssetModel found in the file will be loaded.</param>
        /// <param name="pluginRepository">The plugin repository.</param>
        /// <param name="boundObject">The game object to which the graph should be bound.</param>
        /// <param name="loadStrategy">The type of loading and how it should affect the stack of loaded assets.</param>
        /// <param name="truncateHistoryIndex">Truncate the stack of loaded assets at this index.</param>
        public LoadGraphAssetCommand(string assetPath, long assetLocalId, PluginRepository pluginRepository, GameObject boundObject = null,
                                     LoadStrategies loadStrategy = LoadStrategies.Replace, int truncateHistoryIndex = -1)
        {
            Asset = null;
            AssetPath = assetPath;
            BoundObject = boundObject;
            LoadStrategy = loadStrategy;
            FileId = assetLocalId;
            PluginRepository = pluginRepository;
            TruncateHistoryIndex = truncateHistoryIndex;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadGraphAssetCommand"/> class.
        /// </summary>
        /// <param name="assetModel">The asset model to load.</param>
        /// <param name="boundObject">The game object to which the graph should be bound.</param>
        /// <param name="loadStrategy">The type of loading and how it should affect the stack of loaded assets.</param>
        public LoadGraphAssetCommand(IGraphAssetModel assetModel, GameObject boundObject = null,
                                     LoadStrategies loadStrategy = LoadStrategies.Replace)
        {
            AssetPath = null;
            Asset = assetModel;
            BoundObject = boundObject;
            LoadStrategy = loadStrategy;
            TruncateHistoryIndex = -1;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="toolState">The tool state.</param>
        /// <param name="graphProcessingState">The graph processing state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(ToolStateComponent toolState, GraphProcessingStateComponent graphProcessingState, LoadGraphAssetCommand command)
        {
            if (ReferenceEquals(Selection.activeObject, toolState.AssetModel))
                Selection.activeObject = null;

            if (toolState.GraphModel != null)
            {
                // force queued graph processing to happen now when unloading a graph
                if (graphProcessingState.GraphProcessingPending)
                {
                    // Do not force graph processing if it's the same graph
                    if ((command.AssetPath != null && toolState.AssetModel.GetPath() != command.AssetPath) ||
                        (command.Asset != null && toolState.AssetModel != command.Asset))
                    {
                        GraphProcessingHelper.ProcessGraph(toolState.GraphModel, command.PluginRepository,
                            RequestGraphProcessingOptions.Default);
                    }
                }
            }

            using (var toolStateUpdater = toolState.UpdateScope)
            {
                if (command.TruncateHistoryIndex >= 0)
                    toolStateUpdater.TruncateHistory(command.TruncateHistoryIndex);

                switch (command.LoadStrategy)
                {
                    case LoadStrategies.Replace:
                        toolStateUpdater.ClearHistory();
                        break;
                    case LoadStrategies.PushOnStack:
                        toolStateUpdater.PushCurrentGraph();
                        break;
                    case LoadStrategies.KeepHistory:
                        break;
                }

                var asset = command.Asset;
                if (asset == null)
                {
                    asset = OpenedGraph.Load(command.AssetPath, command.FileId);
                }

                if (asset == null)
                {
                    Debug.LogError($"Could not load visual scripting asset at path '{command.AssetPath}'");
                    return;
                }

                toolStateUpdater.LoadGraphAsset(asset, command.BoundObject);

                var graphModel = toolState.GraphModel;

                if (graphModel != null)
                {
                    ((Stencil)graphModel.Stencil)?.PreProcessGraph(graphModel);

                    foreach (var edgeModel in graphModel.EdgeModels.Select(e => e as EdgeModel))
                        edgeModel?.InitAssetModel(asset);
                    foreach (var nodeModel in graphModel.NodeModels)
                        nodeModel?.DefineNode();
                }

                CheckGraphIntegrity(graphModel);
            }

            using (var graphProcessingStateUpdater = graphProcessingState.UpdateScope)
            {
                graphProcessingStateUpdater.Clear();
            }
        }

        static void CheckGraphIntegrity(IGraphModel graphModel)
        {
            if (graphModel == null)
                return;

            var invalidNodeCount = graphModel.NodeModels.Count(n => n == null);
            var invalidEdgeCount = graphModel.EdgeModels.Count(n => n == null);
            var invalidStickyCount = graphModel.StickyNoteModels.Count(n => n == null);

            var countMessage = new StringBuilder();
            countMessage.Append(invalidNodeCount == 0 ? string.Empty : $"{invalidNodeCount} invalid node(s) found.\n");
            countMessage.Append(invalidEdgeCount == 0 ? string.Empty : $"{invalidEdgeCount} invalid edge(s) found.\n");
            countMessage.Append(invalidStickyCount == 0 ? string.Empty : $"{invalidStickyCount} invalid sticky note(s) found.\n");

            if (countMessage.ToString() != string.Empty)
                if (EditorUtility.DisplayDialog("Invalid graph",
                    $"Invalid elements found:\n{countMessage}\n" +
                    $"Click the Clean button to remove all the invalid elements from the graph.",
                    "Clean",
                    "Cancel"))
                    graphModel.Repair();
        }
    }

    public class UnloadGraphAssetCommand : ICommand
    {
        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="toolState">The tool state.</param>
        /// <param name="graphProcessingState">The graph processing state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(ToolStateComponent toolState, GraphProcessingStateComponent graphProcessingState, UnloadGraphAssetCommand command)
        {
            if (toolState.GraphModel != null)
            {
                // force queued graph processing to happen now when unloading a graph
                if (graphProcessingState.GraphProcessingPending)
                {
                    GraphProcessingHelper.ProcessGraph(toolState.GraphModel, null, RequestGraphProcessingOptions.Default);
                }
            }

            using (var toolStateUpdater = toolState.UpdateScope)
            {
                toolStateUpdater.ClearHistory();
                toolStateUpdater.LoadGraphAsset(null, null);
            }

            using (var graphProcessingStateUpdater = graphProcessingState.UpdateScope)
            {
                graphProcessingStateUpdater.Clear();
            }
        }
    }
}
