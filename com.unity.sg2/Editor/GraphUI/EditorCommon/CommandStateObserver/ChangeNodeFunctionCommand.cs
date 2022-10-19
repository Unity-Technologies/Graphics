using Unity.GraphToolsFoundation.Editor;
using Unity.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ChangeNodeFunctionCommand : UndoableCommand
    {
        readonly SGNodeModel m_SGNodeModel;
        readonly string m_newFunctionName;
        readonly string m_previousFunctionName;

        public ChangeNodeFunctionCommand(
            SGNodeModel sgNodeModel,
            string newFunctionName,
            string previousFunctionName)
        {
            m_SGNodeModel = sgNodeModel;
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
                undoUpdater.SaveState(graphViewState);
            }
            command.m_SGNodeModel.ChangeNodeFunction(command.m_newFunctionName);
            using var graphUpdater = graphViewState.UpdateScope;
            graphUpdater.MarkChanged(command.m_SGNodeModel);
        }
    }

}
