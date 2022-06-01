using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

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
                if (!graphData.IsDeletable()) continue;

                graphModel.GraphHandler.RemoveNode(graphData.graphDataName);

                // Need to update downstream nodes previews of the bypassed node
                previewManager.OnNodeFlowChanged(graphData.graphDataName);
            }
        }

        // TODO: (Sai) Move this stuff into ShaderGraphModel as much as possible
        // there are things that GTF is doing in the base command handler that we shouldn't be disabling or affecting at all
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

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(new IUndoableStateComponent[] { graphModelState, selectionState }, command);
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
                    if (!model.IsDeletable()) continue;

                    switch (model)
                    {
                        case IEdgeModel edge:
                            if (edge.ToPort.NodeModel is RedirectNodeModel redirect)
                            {
                                // Reset types on disconnected redirect nodes.
                                redirect.ClearType();
                                graphUpdater.MarkChanged(redirect);
                            }
                            break;
                    }
                }

                // Bypass redirects in a similar manner to GTF's BypassNodesCommand.
                var deletedModels = HandleRedirectNodes(redirects, graphModel, graphUpdater);

                // Delete everything else as usual.
                deletedModels.AddRange(graphModel.DeleteElements(nonRedirects).DeletedModels);

                var selectedModels = deletedModels.Where(m => selectionState.IsSelected(m)).ToList();
                if (selectedModels.Any())
                {
                    selectionUpdater.SelectElements(selectedModels, false);
                }

                foreach (var model in deletedModels)
                {
                    switch (model)
                    {
                        case IEdgeModel edge:
                            // If its an actual edge, remove from CLDS
                            if(edge.FromPort is GraphDataPortModel sourcePort && edge.ToPort is GraphDataPortModel destPort)
                                graphModel.Disconnect(sourcePort, destPort);
                            break;

                        // Delete backing data for graph data nodes.
                        case GraphDataNodeModel graphDataNode:
                            graphModel.GraphHandler.RemoveNode(graphDataNode.graphDataName);
                            break;
                    }
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
                            previewManager.OnNodeFlowChanged(deletedNode.graphDataName, true);
                            break;
                    }
                }

                graphUpdater.MarkDeleted(deletedModels);
            }
        }

        static List<IGraphElementModel> HandleRedirectNodes(List<RedirectNodeModel> redirects, ShaderGraphModel graphModel, GraphModelStateComponent.StateUpdater graphUpdater)
        {
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
            return deletedModels;
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

        public static void HandleUpdateConstantValue(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            PreviewManager previewManager,
            UpdateConstantValueCommand updateConstantValueCommand)
        {
            var shaderGraphModel = (ShaderGraphModel)graphModelState.GraphModel;
            if (updateConstantValueCommand.Constant is not BaseShaderGraphConstant cldsConstant) return;

            if (cldsConstant.NodeName == Registry.ResolveKey<PropertyContext>().Name)
            {
                previewManager.OnGlobalPropertyChanged(cldsConstant.PortName, updateConstantValueCommand.Value);
                return;
            }

            var nodeWriter = shaderGraphModel.GraphHandler.GetNode(cldsConstant.NodeName);
            if (nodeWriter != null)
            {
                previewManager.OnLocalPropertyChanged(cldsConstant.NodeName, cldsConstant.PortName, updateConstantValueCommand.Value);
            }
        }
    }
}
