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

            command.NodeModel.optedOutOfUpgrade = true;
            graphUpdater.MarkChanged(command.NodeModel);
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
            if (!nodeModel.isUpgradeable)
            {
                Debug.LogWarning($"Attempted to upgrade {nodeModel}, which is already at the latest version");
                return;
            }

            using var graphUpdater = graphModelState.UpdateScope;
            nodeModel.UpgradeNode();
            graphUpdater.MarkChanged(nodeModel);
        }
    }
}
