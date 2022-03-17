using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// Reacts as an edge gets connected.
    /// </summary>
    public interface IRebuildNodeOnConnection
    {
        /// <summary>
        /// Callback for when an edge is connected.
        /// </summary>
        /// <param name="connectedEdge">The connected edge.</param>
        /// <returns>The edges that were deleted as the result of the new connection.</returns>
        IEnumerable<IEdgeModel> RebuildOnEdgeConnected(IEdgeModel connectedEdge);
    }

    /// <summary>
    /// Reacts as an edge gets disconnected.
    /// </summary>
    public interface IRebuildNodeOnDisconnection
    {
        /// <summary>
        /// Callback for when an edge is disconnected.
        /// </summary>
        /// <param name="disconnectedEdge">The disconnected edge.</param>
        /// <returns>The edges that were deleted as the result of the disconnection.</returns>
        IEnumerable<IEdgeModel> RebuildOnEdgeDisconnected(IEdgeModel disconnectedEdge);
    }

    public static class EdgeCommandOverrides
    {
        public static void HandleCreateEdge(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, Preferences preferences, CreateEdgeCommand command)
        {
            CreateEdgeCommand.DefaultCommandHandler(undoState, graphModelState, selectionState, preferences, command);

            var createdEdge = command.ToPortModel.GraphModel.GetEdgeConnectedToPorts(command.ToPortModel, command.FromPortModel);

            if (createdEdge?.ToPort.NodeModel is IRebuildNodeOnConnection needsRebuild)
            {
                var deletedEdges = needsRebuild.RebuildOnEdgeConnected(createdEdge);
                using (var graphUpdater = graphModelState.UpdateScope)
                {
                    graphUpdater.MarkDeleted(deletedEdges);
                    graphUpdater.MarkChanged(command.ToPortModel.NodeModel, ChangeHint.GraphTopology);
                }
            }
        }

        public static void HandleDeleteEdge(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState, DeleteElementsCommand command)
        {
            foreach (var edgeModel in command.Models.OfType<IEdgeModel>())
            {
                if (edgeModel.ToPort.NodeModel is IRebuildNodeOnDisconnection needsRebuild)
                {
                    var deletedEdges = needsRebuild.RebuildOnEdgeDisconnected(edgeModel);
                    using (var graphUpdater = graphModelState.UpdateScope)
                    {
                        graphUpdater.MarkDeleted(deletedEdges);
                        graphUpdater.MarkChanged(edgeModel.ToPort.NodeModel, ChangeHint.GraphTopology);
                    }
                }
            }

            DeleteElementsCommand.DefaultCommandHandler(undoState, graphModelState, selectionState, command);
        }
    }
}
