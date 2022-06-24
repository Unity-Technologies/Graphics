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
            GraphModelStateComponent graphModelState,
            ChangeActiveTargetsCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState , command);
            }

            Debug.Log("ChangeActiveTargetsCommand: Target Settings Change is unimplemented");

            var shaderGraphModel = graphModelState.GraphModel as ShaderGraphModel;
            foreach (var target in shaderGraphModel.Targets)
            {
                foreach (var node in shaderGraphModel.NodeModels)
                {
                    if (node is GraphDataContextNodeModel nodeModel)
                    {
                        // TODO: How to get template name and CustomizationPoint name from target?
                        shaderGraphModel.GraphHandler.RebuildContextData(nodeModel.graphDataName, target.value, "UniversalPipeline", "SurfaceDescription", true);
                        nodeModel.DefineNode();
                    }
                }
            }

            // TODO: Consequences of adding a target: Discovering any new context node ports, validating all nodes on the graph etc.
        }
    }
}
