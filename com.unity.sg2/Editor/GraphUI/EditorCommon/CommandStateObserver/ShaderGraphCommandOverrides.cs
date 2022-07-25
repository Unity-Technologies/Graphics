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
        public static void HandleDeleteNodesAndEdges(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState,
            PreviewManager previewManager,
            DeleteElementsCommand command)
        {
            var modelsToDelete = command.Models.ToList();
            if (modelsToDelete.Count == 0)
                return;
            // We want to override base handling here
            // DeleteElementsCommand.DefaultCommandHandler(undoState, graphModelState, selectionState, command);

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(new IUndoableStateComponent[] { graphModelState, selectionState }, command);
            }

            var graphModel = (ShaderGraphModel)graphModelState.GraphModel;

            // Partition out redirect nodes because they get special delete behavior.
            var redirects = new List<RedirectNodeModel>();
            var nonRedirects = new List<IGraphElementModel>();

            foreach (var model in modelsToDelete)
            {
                switch (model)
                {
                    case RedirectNodeModel redirectModel:
                        redirects.Add(redirectModel);
                        break;
                    case GraphDataVariableDeclarationModel:
                        // We handle variables in HandleDeleteBlackboardItems so can be skipped here
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

                // Update previews
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
                        case GraphDataVariableNodeModel variableNode:
                            previewManager.OnNodeFlowChanged(variableNode.graphDataName, true);
                            break;
                    }
                }

                // Remove CLDS data
                foreach (var model in deletedModels)
                {
                    switch (model)
                    {
                        // Delete backing data for graph data nodes.
                        case GraphDataNodeModel graphDataNode:
                            graphModel.GraphHandler.RemoveNode(graphDataNode.graphDataName);
                            break;
                        // Delete backing data for variable nodes.
                        case GraphDataVariableNodeModel variableNode:
                            var declarationModel = variableNode.DeclarationModel as GraphDataVariableDeclarationModel;
                            graphModel.GraphHandler.RemoveReferenceNode(variableNode.graphDataName, declarationModel.contextNodeName, declarationModel.graphDataName);
                            break;
                    }
                }

                graphUpdater.MarkDeleted(deletedModels);
            }
        }

        public static void HandleDeleteBlackboardItems(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState,
            PreviewManager previewManager,
            DeleteElementsCommand command)
        {
            var modelsToDelete = command.Models.ToList();
            if (modelsToDelete.Count == 0)
                return;

            var graphModel = (ShaderGraphModel)graphModelState.GraphModel;

            // Update previews
            foreach (var model in modelsToDelete)
            {
                switch (model)
                {
                    case GraphDataVariableDeclarationModel variableDeclarationModel:

                        // Gather all variable nodes linked to this blackboard item
                        var linkedVariableNodes = graphModel.GetLinkedVariableNodes(variableDeclarationModel.graphDataName);
                        foreach (var linkedVariableNode in linkedVariableNodes)
                        {
                            var graphDataVariableNode = linkedVariableNode as GraphDataVariableNodeModel;
                            // Notify downstream nodes to update previews
                            previewManager.OnNodeFlowChanged(graphDataVariableNode.graphDataName);
                        }
                        break;
                }
            }

            // Delete GTF data and linked edges, variable nodes
            DeleteElementsCommand.DefaultCommandHandler(undoState, graphModelState, selectionState, command);

            var selectionStateComponents = undoState.State.AllStateComponents.OfType<SelectionStateComponent>();

            foreach (var selection in selectionStateComponents)
            {
                using (var selectionStateUpdater = selection.UpdateScope)
                {
                    selectionStateUpdater.SelectElements(modelsToDelete, false);
                }
            }

            // Remove CLDS data
            foreach (var model in modelsToDelete)
            {
                switch (model)
                {
                    case GraphDataVariableDeclarationModel variableDeclarationModel:
                        graphModel.GraphHandler.RemoveReferableEntry(variableDeclarationModel.contextNodeName, variableDeclarationModel.graphDataName);
                        break;
                }
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
