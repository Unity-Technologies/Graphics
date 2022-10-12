using Unity.GraphToolsFoundation.Editor;
using UnityEngine;
using Unity.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ChangeActiveTargetsCommand : UndoableCommand
    {
        public ChangeActiveTargetsCommand()
        {
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            ChangeActiveTargetsCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveState(graphModelState);
            }

            Debug.Log("ChangeActiveTargetsCommand: Target Settings Change is unimplemented");

            var shaderGraphModel = graphModelState.GraphModel as ShaderGraphModel;
            foreach (var target in shaderGraphModel.Targets)
            {
                shaderGraphModel.InitializeContextFromTarget(target);
            }

            // TODO: Consequences of adding a target: Discovering any new context node ports, validating all nodes on the graph etc.
        }
    }
}
