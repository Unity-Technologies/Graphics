using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class DismissNodeUpgradeCommand : UndoableCommand
    {
        public readonly GraphDataNodeModel NodeModel;

        public DismissNodeUpgradeCommand(GraphDataNodeModel nodeModel)
        {
            NodeModel = nodeModel;
            UndoString = $"Dismiss {nodeModel.DisplayTitle} Node Upgrade Flag";
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            DismissNodeUpgradeCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphModelState, command);
            }

            using var graphUpdater = graphModelState.UpdateScope;
            var node = command.NodeModel;
            node.dismissedUpgradeVersion = node.latestAvailableVersion;
            graphUpdater.MarkChanged(node);
        }
    }

    class UpgradeNodeCommand : UndoableCommand
    {
        public readonly GraphDataNodeModel NodeModel;

        public UpgradeNodeCommand(GraphDataNodeModel nodeModel)
        {
            NodeModel = nodeModel;
            UndoString = $"Upgrade {nodeModel.DisplayTitle} Node";
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            UpgradeNodeCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphModelState, command);
            }

            var nodeModel = command.NodeModel;
            using var graphUpdater = graphModelState.UpdateScope;
            nodeModel.UpgradeToLatestVersion();
            graphUpdater.MarkChanged(nodeModel);
        }
    }
}
