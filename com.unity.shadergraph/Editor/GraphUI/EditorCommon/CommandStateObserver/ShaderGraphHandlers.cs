using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.DataModel;
using UnityEditor.ShaderGraph.GraphUI.Utilities;
using UnityEngine.Assertions;

namespace UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver
{
    public static class ShaderGraphHandlers
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

        public static void HandleDeleteElements(GraphToolState state, DeleteElementsCommand command)
        {
            if (!command.Models.Any())
                return;

            state.PushUndo(command);
            var graphModel = state.GraphViewState.GraphModel;

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

            // Reset types on disconnected redirect nodes.
            foreach (var edge in nonRedirects.OfType<IEdgeModel>())
            {
                if (edge.ToPort.NodeModel is not RedirectNodeModel redirect) continue;

                redirect.ClearType();
                graphUpdater.MarkChanged(redirect);
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
        }
    }
}
