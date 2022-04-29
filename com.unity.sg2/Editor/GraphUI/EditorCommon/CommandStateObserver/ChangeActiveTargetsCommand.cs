using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ChangeActiveTargetsCommand : UndoableCommand
    {
        public ChangeActiveTargetsCommand()
        {
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            ShaderGraphAssetModel graphAsset,
            ChangeActiveTargetsCommand command)
        {
            // TODO: How to undo/redo? Do we need a state component to push on the stack with the current target list?

            Debug.Log("ChangeActiveTargetsCommand: Target Settings Change is unimplemented");

            graphAsset.MarkAsDirty(true);

            // TODO: Consequences of adding a target: Discovering any new context node ports, validating all nodes on the graph etc.
        }
    }
}
