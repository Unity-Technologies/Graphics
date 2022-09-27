using System;
using System.Collections.Generic;
using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.GraphUI
{
    static class ShaderGraphCommandOverrides
    {
        public static void HandleBypassNodes(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState,
            PreviewUpdateDispatcher previewUpdateDispatcher,
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
                previewUpdateDispatcher.OnListenerConnectionChanged(graphData.graphDataName);
            }
        }

        // TODO: (Sai) Move this stuff into ShaderGraphModel as much as possible
        // there are things that GTF is doing in the base command handler that we shouldn't be disabling or affecting at all
        public static void HandleDeleteNodesAndEdges(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState,
            DeleteElementsCommand command)
        {
            var modelsToDelete = command.Models.ToList();
            if (modelsToDelete.Count == 0)
                return;

            // We want to override base handling here
            // DeleteElementsCommand.DefaultCommandHandler(undoState, graphModelState, selectionState, command);

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(graphModelState, selectionState);
            }

            var graphModel = (ShaderGraphModel)graphModelState.GraphModel;

            // Partition out redirect nodes because they get special delete behavior.
            var redirects = new List<RedirectNodeModel>();
            var nonRedirects = new List<GraphElementModel>();

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
                        case WireModel edge:
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

                graphUpdater.MarkDeleted(deletedModels);
            }
        }

        static List<GraphElementModel> HandleRedirectNodes(List<RedirectNodeModel> redirects, ShaderGraphModel graphModel, GraphModelStateComponent.StateUpdater graphUpdater)
        {
            foreach (var redirect in redirects)
            {
                var inputEdgeModel = redirect.GetIncomingEdges().FirstOrDefault();
                var outputEdgeModels = redirect.GetOutgoingEdges().ToList();

                graphModel.DeleteWire(inputEdgeModel);
                graphModel.DeleteWires(outputEdgeModels);

                graphUpdater.MarkDeleted(inputEdgeModel);
                graphUpdater.MarkDeleted(outputEdgeModels);

                if (inputEdgeModel == null || !outputEdgeModels.Any()) continue;

                foreach (var outputEdgeModel in outputEdgeModels)
                {
                    var edge = graphModel.CreateWire(outputEdgeModel.ToPort, inputEdgeModel.FromPort);
                    graphUpdater.MarkNew(edge);
                }
            }

            // Don't delete connections for redirects, because we may have made
            // edges we want to preserve. Edges we don't need were already
            // deleted in the above loop.
            var deletedModels = graphModel.DeleteNodes(redirects, false).ToList();
            return deletedModels;
        }

        internal static void HandlePasteSerializedData(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, PasteSerializedDataCommand command)
        {
            if (!command.Data.IsEmpty())
            {
                // Mapping of <original node guid> to <incoming edge list for the node>
                Dictionary<string, IEnumerable<WireModel>> nodeGuidToEdges = new();
                var wires = command.Data.Wires;
                var nodes = command.Data.Nodes;
                if (nodes != null && wires != null)
                {
                    foreach (var node in nodes)
                    {
                        // Get all input edges on the node being duplicated
                        var connectedEdges = wires.Where(edgeModel => edgeModel.ToNodeGuid == node.Guid);
                        if (connectedEdges.Any())
                            nodeGuidToEdges.Add(node.Guid.ToString(), connectedEdges);
                    }
                }

                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    var undoableStates = selectionHelper.UndoableSelectionStates.Append(graphModelState);
                    undoStateUpdater.SaveStates(undoableStates);
                }

                var graphModel = graphModelState.GraphModel;
                using (var graphModelStateUpdater = graphModelState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                {
                    foreach (var selectionUpdater in selectionUpdaters)
                    {
                        selectionUpdater.ClearSelection();
                    }

                    var pastedElementsMapping = CopyPasteData.PasteSerializedData(
                        command.Operation, command.Delta, graphModelStateUpdater,
                        null, selectionUpdaters.MainUpdateScope, command.Data, graphModelState.GraphModel, command.SelectedGroup);

                    if (pastedElementsMapping.Count != 0)
                    {
                        try
                        {
                            // Key is the original node, Value is the duplicated node
                            foreach (var (key, value) in pastedElementsMapping)
                            {
                                if (value is not AbstractNodeModel nodeModel)
                                    continue;

                                if (!nodeGuidToEdges.TryGetValue(key, out var originalNodeConnections))
                                    continue;
                                foreach (var originalNodeEdge in originalNodeConnections)
                                {
                                    var duplicatedIncomingNode = pastedElementsMapping.FirstOrDefault(pair => pair.Key == originalNodeEdge.FromNodeGuid.ToString()).Value;
                                    WireModel edgeModel = null;

                                    // If any node that was copied has an incoming edge from a node that was ALSO
                                    // copied, then we need to find the duplicated copy of the incoming node
                                    // and create the edge between these new duplicated nodes instead
                                    if (duplicatedIncomingNode is NodeModel duplicatedIncomingNodeModel)
                                    {
                                        var fromPort = ShaderGraphModel.FindOutputPortByName(duplicatedIncomingNodeModel, originalNodeEdge.FromPortId);
                                        var toPort = ShaderGraphModel.FindInputPortByName(nodeModel, originalNodeEdge.ToPortId);
                                        Assert.IsNotNull(fromPort);
                                        Assert.IsNotNull(toPort);
                                        edgeModel = graphModel.CreateWire(toPort, fromPort);
                                    }
                                    else // Just copy that connection over to the new duplicated node
                                    {
                                        var toPort = ShaderGraphModel.FindInputPortByName(nodeModel, originalNodeEdge.ToPortId);
                                        var fromNodeModel = graphModel.NodeModels.FirstOrDefault(model => model.Guid == originalNodeEdge.FromNodeGuid);
                                        if (fromNodeModel != null)
                                        {
                                            var fromPort = ShaderGraphModel.FindOutputPortByName(fromNodeModel, originalNodeEdge.FromPortId);
                                            Assert.IsNotNull(fromPort);
                                            Assert.IsNotNull(toPort);
                                            edgeModel = graphModel.CreateWire(toPort, fromPort);
                                        }
                                    }

                                    if (edgeModel != null)
                                    {
                                        graphModelStateUpdater?.MarkNew(edgeModel);
                                    }
                                }
                            }
                        }
                        catch (Exception edgeFixupException)
                        {
                            Debug.Log("Exception Thrown while trying to handle post copy-paste edge fixup." + edgeFixupException);
                        }
                    }
                }
            }
        }
    }
}
