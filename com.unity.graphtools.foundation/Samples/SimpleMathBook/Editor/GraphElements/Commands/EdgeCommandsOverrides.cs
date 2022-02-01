using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    /// <summary>
    /// Reacts as an edge gets connected.
    /// </summary>
    public interface IRebuildNodeOnConnection
    {
        bool RebuildOnEdgeConnected(IEdgeModel connectedEdge);
    }

    /// <summary>
    /// Reacts as an edge gets disconnected.
    /// </summary>
    public interface IRebuildNodeOnDisconnection
    {
        bool RebuildOnEdgeDisconnected(IEdgeModel disconnectedEdge);
    }

    public static class EdgeCommandOverrides
    {
        public static void HandleCreateEdge(UndoStateComponent undoState, GraphViewStateComponent graphViewState, Preferences preferences, CreateEdgeCommand command)
        {
            CreateEdgeCommand.DefaultCommandHandler(undoState, graphViewState, preferences, command);

            var createdEdge = command.ToPortModel.GraphModel.GetEdgeConnectedToPorts(command.ToPortModel, command.FromPortModel);

            if (createdEdge != null
                && createdEdge.ToPort.NodeModel is IRebuildNodeOnConnection needsRebuild
                && needsRebuild.RebuildOnEdgeConnected(createdEdge))
                using (var graphUpdater = graphViewState.UpdateScope)
                    graphUpdater.MarkChanged(command.ToPortModel.NodeModel);
        }

        public static void HandleDeleteEdge(UndoStateComponent undoState, GraphViewStateComponent graphViewState, SelectionStateComponent selectionState, DeleteElementsCommand command)
        {
            foreach (var edgeModel in command.Models.OfType<IEdgeModel>())
            {
                if (edgeModel.ToPort.NodeModel is IRebuildNodeOnDisconnection needsRebuild
                    && needsRebuild.RebuildOnEdgeDisconnected(edgeModel))
                    using (var graphUpdater = graphViewState.UpdateScope)
                        graphUpdater.MarkChanged(edgeModel.ToPort.NodeModel);
            }

            DeleteElementsCommand.DefaultCommandHandler(undoState, graphViewState, selectionState, command);
        }
    }
}
