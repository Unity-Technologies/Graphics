using System.Collections.Generic;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
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
            GraphModelStateComponent graphModelState,
            ChangeTargetSettingsCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState , command);
            }

            Debug.Log("ChangeTargetSettingsCommand: Target Settings Change is unimplemented");

            // TODO: Consequences of changing a target setting: Discovering any new context node ports, validating all nodes on the graph etc.
        }
    }
}
