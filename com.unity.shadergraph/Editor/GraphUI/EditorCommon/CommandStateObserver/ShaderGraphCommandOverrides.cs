using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public static class ShaderGraphCommandOverrides
    {
        public static void HandleCreateEdge(
            BaseGraphTool graphTool,
            GraphViewModel graphView,
            PreviewManager previewManager,
            CreateEdgeCommand command)
        {
            var undoState = graphTool.UndoStateComponent;
            var graphModelState = graphView.GraphModelState;
            var selectionState = graphView.SelectionState;
            var preferences = graphTool.Preferences;

            CreateEdgeCommand.DefaultCommandHandler(undoState, graphModelState, selectionState, preferences, command);

            var resolvedSource = command.FromPortModel;
            var resolvedDestinations = new List<IPortModel>();

            if (command.ToPortModel.NodeModel is RedirectNodeModel toRedir)
            {
                resolvedDestinations = toRedir.ResolveDestinations().ToList();

                // Update types of descendant redirect nodes.
                using var graphUpdater = graphModelState.UpdateScope;
                foreach (var child in toRedir.GetRedirectTree(true))
                {
                    child.UpdateTypeFrom(command.FromPortModel);
                    graphUpdater.MarkChanged(child);
                }
            }
            else
            {
                resolvedDestinations.Add(command.ToPortModel);
            }

            if (command.FromPortModel.NodeModel is RedirectNodeModel fromRedir)
            {
                resolvedSource = fromRedir.ResolveSource();
            }

            if (resolvedSource is not GraphDataPortModel fromDataPort) return;

            // Make the corresponding connections in Shader Graph's data model.
            var shaderGraphModel = (ShaderGraphModel) graphModelState.GraphModel;
            foreach (var toDataPort in resolvedDestinations.OfType<GraphDataPortModel>())
            {
                // Validation should have already happened in GraphModel.IsCompatiblePort.
                Assert.IsTrue(shaderGraphModel.TryConnect(fromDataPort, toDataPort));

                // Notify preview manager that this nodes connections have changed
                previewManager.OnNodeFlowChanged(toDataPort.graphDataNodeModel.graphDataName);
            }
        }

        public static void HandleBypassNodes(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState,
            PreviewManager previewManager,
            BypassNodesCommand command)
        {
            BypassNodesCommand.DefaultCommandHandler(undoState, graphModelState, selectionState, command);

            var graphModel = (ShaderGraphModel)graphModelState.GraphModel;

            // Delete backing data for graph data nodes.
            foreach (var graphData in command.Models.OfType<GraphDataNodeModel>())
            {
                graphModel.GraphHandler.RemoveNode(graphData.graphDataName);

                // Need to update downstream nodes previews of the bypassed node
                previewManager.OnNodeFlowChanged(graphData.graphDataName);
            }
        }

        public static void HandleDeleteElements(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState,
            PreviewManager previewManager,
            DeleteElementsCommand command)
        {
            // Don't want to call base command handler as doing so wipes command from info. of objects being deleted
            //DeleteElementsCommand.DefaultCommandHandler(undoState, graphViewState, selectionState, command);

            if (!command.Models.Any())
                return;

            using var undoStateUpdater = undoState.UpdateScope;
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            var graphModel = (ShaderGraphModel)graphModelState.GraphModel;

            // Partition out redirect nodes because they get special delete behavior.
            var redirects = new List<RedirectNodeModel>();
            var nonRedirects = new List<IGraphElementModel>();

            foreach (var model in command.Models)
            {
                switch (model)
                {
                    case RedirectNodeModel redirectModel:
                        redirects.Add(redirectModel);
                        break;
                    default:
                        nonRedirects.Add(model);
                        break;
                }
            }

            using var selectionUpdater = selectionState.UpdateScope;
            using var graphUpdater = graphModelState.UpdateScope;
            {
                foreach (var model in nonRedirects)
                {
                    switch (model)
                    {
                        // Reset types on disconnected redirect nodes.
                        case IEdgeModel edge:
                        {
                            if (edge.ToPort.NodeModel is not RedirectNodeModel redirect) continue;

                            redirect.ClearType();

                            graphUpdater.MarkChanged(redirect);
                            break;
                        }

                        // Delete backing data for graph data nodes.
                        case GraphDataNodeModel graphData:
                        {
                            graphModel.GraphHandler.RemoveNode(graphData.graphDataName);
                            break;
                        }
                    }
                }

                // Bypass redirects in a similar manner to GTF's BypassNodesCommand.
                foreach (var redirect in redirects)
                {
                    var inputEdgeModel = redirect.GetIncomingEdges().FirstOrDefault();
                    var outputEdgeModels = redirect.GetOutgoingEdges().ToList();

                    graphModel.DeleteEdge(inputEdgeModel);
                    graphModel.DeleteEdges(outputEdgeModels);

                    graphUpdater.MarkDeleted(inputEdgeModel);
                    graphUpdater.MarkDeleted(outputEdgeModels);

                    if (inputEdgeModel == null || !outputEdgeModels.Any()) continue;

                    foreach (var outputEdgeModel in outputEdgeModels)
                    {
                        var edge = graphModel.CreateEdge(outputEdgeModel.ToPort, inputEdgeModel.FromPort);
                        graphUpdater.MarkNew(edge);
                    }
                }

                // Don't delete connections for redirects, because we may have made
                // edges we want to preserve. Edges we don't need were already
                // deleted in the above loop.
                var deletedModels = graphModel.DeleteNodes(redirects, false).ToList();

                // Delete everything else as usual.
                deletedModels.AddRange(graphModel.DeleteElements(nonRedirects).DeletedModels);

                var selectedModels = deletedModels.Where(m => selectionState.IsSelected(m)).ToList();
                if (selectedModels.Any())
                {
                    selectionUpdater.SelectElements(selectedModels, false);
                }

                // After all redirect nodes handling and deletion has been handled above, then process the new graph flow
                foreach (var model in deletedModels)
                {
                    switch (model)
                    {
                        case EdgeModel edgeModel:
                            if (edgeModel.ToPort.NodeModel is GraphDataNodeModel graphDataNodeModel)
                                previewManager.OnNodeFlowChanged(graphDataNodeModel.graphDataName);
                            break;
                        case GraphDataNodeModel deletedNode:
                            previewManager.OnNodeFlowChanged(deletedNode.graphDataName);
                            previewManager.OnNodeRemoved(deletedNode.graphDataName);
                            break;
                    }
                }

                graphUpdater.MarkDeleted(deletedModels);

                foreach (var nodeModel in command.Models)
                {
                    if (nodeModel is GraphDataNodeModel graphDataNodeModel)
                        previewManager.OnNodeFlowChanged(graphDataNodeModel.graphDataName);
                }
            }
        }

        public static void HandleGraphElementRenamed(
            GraphViewStateComponent graphViewState,
            PreviewManager previewManager,
            RenameElementCommand renameElementCommand
        )
        {
            // TODO: Handle Properties being renamed when those come online
            //if (renameElementCommand.Model is IVariableDeclarationModel variableDeclarationModel)
            //{
            //    // React to property being renamed by finding all linked property nodes and marking them as requiring recompile and also needing constant value update
            //    var graphNodes = graphViewState.GraphModel.NodeModels;
            //    foreach (var graphNode in graphNodes)
            //    {
            //        if (graphNode is IVariableNodeModel variableNodeModel && Equals(variableNodeModel.VariableDeclarationModel, variableDeclarationModel))
            //        {
            //            previewManager.NotifyNodeFlowChanged(variableNodeModel);
            //        }
            //    }
            //}
        }

        //public static void HandleUpdateConstantValue(
        //    GraphViewStateComponent graphViewState,
        //    PreviewManager previewManager,
        //    UpdateConstantValueCommand updateConstantValueCommand)
        //{
        //    // TODO: Handle Property values being changed when those come online
        //    // using var previewUpdater = graphPreviewState.UpdateScope;
        //    // {
        //    //     // Find all property nodes backed by this constant
        //    //     var graphNodes = graphViewState.GraphModel.NodeModels;
        //    //     foreach (var graphNode in graphNodes)
        //    //     {
        //    //         if (graphNode is IVariableNodeModel variableNodeModel &&
        //    //             Equals(variableNodeModel.VariableDeclarationModel.InitializationModel, updateConstantValueCommand.Constant))
        //    //         {
        //    //             previewUpdater.UpdateVariableConstantValue(updateConstantValueCommand.Value, variableNodeModel);
        //    //         }
        //    //     }
        //    // }
        //}

        public static void HandleUpdateConstantValue(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            PreviewManager previewManager,
            UpdateConstantValueCommand updateConstantValueCommand)
        {
            var shaderGraphModel = (ShaderGraphModel)graphModelState.GraphModel;
            if (updateConstantValueCommand.Constant is ICLDSConstant cldsConstant && cldsConstant.NodeName != "MaterialPropertyContext") // TODO:
            {
                var nodeWriter = shaderGraphModel.GraphHandler.GetNode(cldsConstant.NodeName);
                if (nodeWriter != null)
                {
                    previewManager.OnLocalPropertyChanged(cldsConstant.NodeName, cldsConstant.PortName, updateConstantValueCommand.Value);
                }
            }
        }
    }
}
