using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public static class ShaderGraphCommandOverrides
    {
        public static void HandleCreateEdge(GraphToolState state, CreateEdgeCommand command)
        {
            CreateEdgeCommand.DefaultCommandHandler(state, command);

            var resolvedSource = command.FromPortModel;
            var resolvedDestinations = new List<IPortModel>();

            if (command.ToPortModel.NodeModel is RedirectNodeModel toRedir)
            {
                resolvedDestinations = toRedir.ResolveDestinations().ToList();

                // Update types of descendant redirect nodes.
                using var graphUpdater = state.GraphViewState.UpdateScope;
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
            var shaderGraphModel = (ShaderGraphModel) state.GraphViewState.GraphModel;
            foreach (var toDataPort in resolvedDestinations.OfType<GraphDataPortModel>())
            {
                // Validation should have already happened in GraphModel.IsCompatiblePort.
                Assert.IsTrue(shaderGraphModel.TryConnect(fromDataPort, toDataPort));
            }
        }

        public static void HandleBypassNodes(GraphToolState state, BypassNodesCommand command)
        {
            BypassNodesCommand.DefaultCommandHandler(state, command);

            var graphModel = (ShaderGraphModel)state.GraphViewState.GraphModel;

            // Delete backing data for graph data nodes.
            foreach (var graphData in command.Models.OfType<GraphDataNodeModel>())
            {
                graphModel.GraphHandler.RemoveNode(graphData.graphDataName);
            }
        }

        public static void HandleDeleteElements(GraphToolState state, DeleteElementsCommand command)
        {
            if (!command.Models.Any())
                return;

            state.PushUndo(command);
            var graphModel = (ShaderGraphModel)state.GraphViewState.GraphModel;

            // Partition out redirect nodes because they get special delete behavior.
            var redirects = new List<RedirectNodeModel>();
            var nonRedirects = new List<IGraphElementModel>();

            foreach (var model in command.Models)
            {
                if (model is RedirectNodeModel redirectModel) redirects.Add(redirectModel);
                else nonRedirects.Add(model);
            }

            using var selectionUpdater = state.SelectionState.UpdateScope;
            using var graphUpdater = state.GraphViewState.UpdateScope;

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

            // Don't delete connections for redirects, because we may have made edges we want to preserve. Edges we
            // don't need were already deleted in the above loop.
            var deletedModels = graphModel.DeleteNodes(redirects, false).ToList();

            // Delete everything else as usual.
            deletedModels.AddRange(graphModel.DeleteElements(nonRedirects));

            var selectedModels = deletedModels.Where(m => state.SelectionState.IsSelected(m)).ToList();
            if (selectedModels.Any())
            {
                selectionUpdater.SelectElements(selectedModels, false);
            }

            graphUpdater.MarkDeleted(deletedModels);

            if (state is ShaderGraphState shaderGraphState)
            {
                using var previewUpdater = shaderGraphState.GraphPreviewState.UpdateScope;
                {
                    foreach (var nodeModel in command.Models)
                    {
                        if(nodeModel is GraphDataNodeModel graphDataNodeModel)
                            previewUpdater.GraphDataNodeRemoved(graphDataNodeModel);
                    }
                }
            }

        }

        // Currently this is unused because we don't take advantage of GTFs ability for models to be enabled/disabled
        public static void HandleNodeStateChanged(GraphToolState graphToolState, ChangeNodeStateCommand changeNodeStateCommand)
        {
            if (graphToolState is ShaderGraphState shaderGraphState)
            {
                using var previewUpdater = shaderGraphState.GraphPreviewState.UpdateScope;
                {
                    foreach (var nodeModel in changeNodeStateCommand.Models)
                    {
                        previewUpdater.UpdateNodeState(nodeModel.Guid.ToString(), changeNodeStateCommand.Value);
                    }
                }
            }
        }

        public static void HandleGraphElementRenamed(GraphToolState graphToolState, RenameElementCommand renameElementCommand)
        {
            if (graphToolState is ShaderGraphState shaderGraphState)
            {
                using var previewUpdater = shaderGraphState.GraphPreviewState.UpdateScope;
                {
                    if (renameElementCommand.Model is IVariableDeclarationModel variableDeclarationModel)
                    {
                        previewUpdater.MarkNodeNeedingRecompile(variableDeclarationModel.Guid.ToString(), null);

                        // TODO: Handle this in a similar way to HandleUpdateConstantValue, but also accounting for recompiles

                        // React to property being renamed by finding all linked property nodes and marking them as requiring recompile and also needing constant value update
                        var graphNodes = graphToolState.GraphViewState.GraphModel.NodeModels;
                        foreach (var graphNode in graphNodes)
                        {
                            if (graphNode is IVariableNodeModel variableNodeModel && Equals(variableNodeModel.VariableDeclarationModel, variableDeclarationModel))
                            {
                                previewUpdater.MarkNodeNeedingRecompile(variableNodeModel.Guid.ToString(), null);
                                previewUpdater.UpdateVariableConstantValue(variableDeclarationModel.InitializationModel.ObjectValue, variableNodeModel);
                            }
                        }
                    }
                }
            }
        }

        public static void HandleUpdateConstantValue(GraphToolState graphToolState, UpdateConstantValueCommand updateConstantValueCommand)
        {
            if (graphToolState is ShaderGraphState shaderGraphState)
            {
                using var previewUpdater = shaderGraphState.GraphPreviewState.UpdateScope;
                {
                    // Find all property nodes backed by this constant
                    var graphNodes = graphToolState.GraphViewState.GraphModel.NodeModels;
                    foreach (var graphNode in graphNodes)
                    {
                        if (graphNode is IVariableNodeModel variableNodeModel && Equals(variableNodeModel.VariableDeclarationModel.InitializationModel, updateConstantValueCommand.Constant))
                        {
                            previewUpdater.UpdateVariableConstantValue(updateConstantValueCommand.Value, variableNodeModel);
                        }
                    }
                }
            }
        }
    }
}
