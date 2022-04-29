using System.Collections.Generic;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ChangeTargetSettingsCommand : UndoableCommand
    {
        public ChangeTargetSettingsCommand()
        {
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            ShaderGraphAssetModel graphAsset,
            ChangeTargetSettingsCommand command)
        {
            // TODO: How to undo/redo? Do we need a state component to push on the stack with the current target list?

            Debug.Log("ChangeTargetSettingsCommand: Target Settings Change is unimplemented");

            graphAsset.MarkAsDirty(true);

            // TODO: Consequences of changing a target setting: Discovering any new context node ports, validating all nodes on the graph etc.
        }
    }
}
