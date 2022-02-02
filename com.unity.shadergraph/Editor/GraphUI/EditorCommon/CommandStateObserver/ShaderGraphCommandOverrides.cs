using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.Preview;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public static class ShaderGraphCommandOverrides
    {
        public static void HandleCreateEdge(
            UndoStateComponent undoState,
            GraphViewStateComponent graphViewState,
            PreviewManager previewManager,
            Preferences preferences,
            CreateEdgeCommand command)
        {
            CreateEdgeCommand.DefaultCommandHandler(undoState, graphViewState, preferences, command);

            var resolvedSource = command.FromPortModel;
            var resolvedDestinations = new List<IPortModel>();

            if (command.ToPortModel.NodeModel is RedirectNodeModel toRedir)
            {
                resolvedDestinations = toRedir.ResolveDestinations().ToList();

                // Update types of descendant redirect nodes.
                using var graphUpdater = graphViewState.UpdateScope;
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
            var shaderGraphModel = (ShaderGraphModel) graphViewState.GraphModel;
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
            GraphViewStateComponent graphViewState,
            PreviewManager previewManager,
            BypassNodesCommand command)
        {
            BypassNodesCommand.DefaultCommandHandler(undoState, graphViewState, command);

            var graphModel = (ShaderGraphModel)graphViewState.GraphModel;

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
            GraphViewStateComponent graphViewState,
            SelectionStateComponent selectionState,
            PreviewManager previewManager,
            DeleteElementsCommand command)
        {
            if (!command.Models.Any())
                return;

            undoState.UpdateScope.SaveSingleState(graphViewState, command);
            var graphModel = (ShaderGraphModel)graphViewState.GraphModel;

            // Partition out redirect nodes because they get special delete behavior.
            var redirects = new List<RedirectNodeModel>();
            var nonRedirects = new List<IGraphElementModel>();

            foreach (var model in command.Models)
            {
                if (model is RedirectNodeModel redirectModel) redirects.Add(redirectModel);
                else nonRedirects.Add(model);
            }

            using var selectionUpdater = selectionState.UpdateScope;
            using var graphUpdater = graphViewState.UpdateScope;

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
            deletedModels.AddRange(graphModel.DeleteElements(nonRedirects));

            var selectedModels = deletedModels.Where(m => selectionState.IsSelected(m)).ToList();
            if (selectedModels.Any())
            {
                selectionUpdater.SelectElements(selectedModels, false);
            }

            graphUpdater.MarkDeleted(deletedModels);

            foreach (var nodeModel in command.Models)
            {
                if (nodeModel is GraphDataNodeModel graphDataNodeModel)
                    previewManager.OnNodeFlowChanged(graphDataNodeModel.graphDataName);
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

        public static void HandleUpdateConstantValue(
            GraphViewStateComponent graphViewState,
            PreviewManager previewManager,
            UpdateConstantValueCommand updateConstantValueCommand)
        {
            // TODO: Handle Property values being changed when those come online
            // using var previewUpdater = graphPreviewState.UpdateScope;
            // {
            //     // Find all property nodes backed by this constant
            //     var graphNodes = graphViewState.GraphModel.NodeModels;
            //     foreach (var graphNode in graphNodes)
            //     {
            //         if (graphNode is IVariableNodeModel variableNodeModel &&
            //             Equals(variableNodeModel.VariableDeclarationModel.InitializationModel, updateConstantValueCommand.Constant))
            //         {
            //             previewUpdater.UpdateVariableConstantValue(updateConstantValueCommand.Value, variableNodeModel);
            //         }
            //     }
            // }
        }

        public static void HandleUpdatePortValue(
            GraphViewStateComponent graphViewState,
            PreviewManager previewManager,
            ShaderGraphModel shaderGraphModel,
            UpdatePortConstantCommand updatePortConstantCommand)
        {
            var portModel = updatePortConstantCommand.PortModel;
            if (portModel.NodeModel is GraphDataNodeModel nodeModel)
            {
                var nodeWriter = shaderGraphModel.GraphHandler.GetNodeWriter(nodeModel.graphDataName);
                if (nodeWriter != null)
                {
                    var vec4Value = updatePortConstantCommand.NewValue is Vector4 value ? value : default;
                    nodeWriter.SetPortField(portModel.UniqueName, "c0", vec4Value.x);
                    nodeWriter.SetPortField(portModel.UniqueName, "c1", vec4Value.y);
                    nodeWriter.SetPortField(portModel.UniqueName, "c2", vec4Value.z);
                    nodeWriter.SetPortField(portModel.UniqueName, "c3", vec4Value.w);

                    previewManager.OnLocalPropertyChanged(nodeModel.graphDataName, portModel.UniqueName, vec4Value);
                }
            }
        }
    }
}
