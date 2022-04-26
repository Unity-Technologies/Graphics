using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a subgraph.
    /// </summary>
    public class CreateSubgraphCommand : UndoableCommand
    {
        const string k_PromptToCreateTitle = "Create {0}";
        const string k_PromptToCreate = "Create a new {0}";

        /// <summary>
        /// The GraphView in charge of aligning the nodes.
        /// </summary>
        public readonly GraphView GraphView;

        /// <summary>
        /// The graph elements that need to be recreated in the subgraph.
        /// </summary>
        public List<IGraphElementModel> ElementsToAddToSubgraph;

        /// <summary>
        /// The SerializableGUID to assign to the newly subgraph node.
        /// </summary>
        public SerializableGUID Guid;

        /// <summary>
        /// The type of the asset.
        /// </summary>
        public Type AssetType;

        /// <summary>
        /// The template to create the subgraph.
        /// </summary>
        public IGraphTemplate Template;

        /// <summary>
        /// The path of the newly created asset when there is no prompt to create the subgraph.
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphCommand"/>.
        /// </summary>
        public CreateSubgraphCommand()
        {
            UndoString = "Create Subgraph";
        }

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphCommand"/>.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="assetType">The type of the asset.</param>
        /// <param name="elementsToCreate">The graph elements that need to be created in the subgraph.</param>
        /// <param name="template">The template of the subgraph.</param>
        /// <param name="graphView">The current graph view.</param>
        public CreateSubgraphCommand(Type assetType, List<IGraphElementModel> elementsToCreate, IGraphTemplate template, GraphView graphView)
            : this()
        {
            AssetType = assetType;
            Guid = SerializableGUID.Generate();
            GraphView = graphView;
            ElementsToAddToSubgraph = elementsToCreate;
            Template = template;
        }

        /// <summary>
        /// Initializes a new <see cref="CreateSubgraphCommand"/>.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="assetType">The type of the asset.</param>
        /// <param name="elementsToCreate">The graph elements that need to be created in the subgraph.</param>
        /// <param name="template">The template of the subgraph.</param>
        /// <param name="graphView">The current graph view.</param>
        /// <param name="assetPath">The path of the asset.</param>
        public CreateSubgraphCommand(Type assetType, List<IGraphElementModel> elementsToCreate, IGraphTemplate template, GraphView graphView, string assetPath)
            : this(assetType, elementsToCreate, template, graphView)
        {
            AssetType = assetType;
            Guid = SerializableGUID.Generate();
            GraphView = graphView;
            ElementsToAddToSubgraph = elementsToCreate;
            Template = template;
            AssetPath = assetPath;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, CreateSubgraphCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            ISubgraphNodeModel subgraphNodeModel;
            using (var graphUpdater = graphModelState.UpdateScope)
            {
                var elementsToAddToSubgraph = SubgraphCreationHelpers.GraphElementsToAddToSubgraph.ConvertToGraphElementsToAdd(command.ElementsToAddToSubgraph);

                // Get source edge models AND edge models that aren't source edge models but are connected to source nodes.
                var allEdgeModels = elementsToAddToSubgraph.EdgeModels;
                foreach (var nodeModel in elementsToAddToSubgraph.NodeModels)
                    allEdgeModels.UnionWith(nodeModel.GetConnectedEdges());

                // Get edge connections (edge to subgraph node port unique name) to keep after the subgraph node creation
                var inputEdgeConnections = new Dictionary<IEdgeModel, string>();
                var outputEdgeConnections = new Dictionary<IEdgeModel, string>();
                foreach (var edgeModel in allEdgeModels)
                {
                    if (!elementsToAddToSubgraph.NodeModels.Contains(edgeModel.FromPort.NodeModel))
                        inputEdgeConnections.Add(edgeModel, "");
                    else if (!elementsToAddToSubgraph.NodeModels.Contains(edgeModel.ToPort.NodeModel))
                        outputEdgeConnections.Add(edgeModel, "");
                }

                // Create the subgraph
                IGraphAsset graphAsset;
                if (command.AssetPath == null)
                {
                    var promptTitle = string.Format(k_PromptToCreateTitle, command.Template.GraphTypeName);
                    var prompt = string.Format(k_PromptToCreate, command.Template.GraphTypeName);
                    graphAsset = GraphAssetCreationHelpers.PromptToCreateGraphAsset(command.AssetType, command.Template, promptTitle, prompt);
                }
                else
                {
                    graphAsset = GraphAssetCreationHelpers.CreateGraphAsset(command.AssetType, command.Template.StencilType, command.Template.GraphTypeName, command.AssetPath, command.Template);
                }

                if (graphAsset as Object == null)
                    return;

                SubgraphCreationHelpers.PopulateSubgraph(graphAsset.GraphModel, elementsToAddToSubgraph, allEdgeModels, inputEdgeConnections, outputEdgeConnections);

                // Delete the graph elements that will be created in the local subgraph
                var deletedModels = new List<IGraphElementModel>();

                var graphModel = graphModelState.GraphModel;
                deletedModels.AddRange(graphModel.DeletePlacemats(elementsToAddToSubgraph.PlacematModels));
                deletedModels.AddRange(graphModel.DeleteStickyNotes(elementsToAddToSubgraph.StickyNoteModels));
                deletedModels.AddRange(graphModel.DeleteNodes(elementsToAddToSubgraph.NodeModels, true));
                deletedModels.AddRange(graphModel.DeleteEdges(allEdgeModels));

                graphUpdater.MarkDeleted(deletedModels);

                // Create the subgraph node
                var position = SubgraphNode.ComputeSubgraphNodePosition(command.ElementsToAddToSubgraph, command.GraphView);
                subgraphNodeModel = graphModel.CreateSubgraphNode(graphAsset.GraphModel, position, command.Guid);
                graphUpdater.MarkNew(subgraphNodeModel);

                // Create new edges linking the subgraph node to other nodes
                graphUpdater.MarkNew(SubgraphCreationHelpers.CreateEdgesConnectedToSubgraphNode(graphModel, subgraphNodeModel, inputEdgeConnections, outputEdgeConnections));
            }

            if (subgraphNodeModel != null)
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var updater in selectionUpdaters)
                        updater.ClearSelection(graphModelState.GraphModel);
                    selectionUpdaters.MainUpdateScope.SelectElement(subgraphNodeModel, true);
                }
            }
        }
    }
}
