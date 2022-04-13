using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ChangeNodeFunctionCommand : UndoableCommand
    {
        readonly GraphDataNodeModel m_GraphDataNodeModel;
        readonly string m_newFunctionName;
        readonly string m_previousFunctionName;

        public ChangeNodeFunctionCommand(
            GraphDataNodeModel graphDataNodeModel,
            string previousFunctionName,
            string newFunctionName)
        {
            m_GraphDataNodeModel = graphDataNodeModel;
            m_previousFunctionName = previousFunctionName;
            m_newFunctionName = newFunctionName;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphViewState,
            ChangeNodeFunctionCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphViewState, command);
            }
            command.m_GraphDataNodeModel.ChangeNodeFunction(command.m_newFunctionName);
            using var graphUpdater = graphViewState.UpdateScope;
            graphUpdater.MarkChanged(command.m_GraphDataNodeModel);
        }
    }

}
