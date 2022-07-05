using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class SetShaderDeclarationCommand : UndoableCommand
    {
        readonly ContextEntryEnumTags.DataSource m_DataSource;
        readonly GraphDataVariableDeclarationModel m_Model;

        // Note: ModelPropertyField expects this signature
        public SetShaderDeclarationCommand(
            ContextEntryEnumTags.DataSource dataSource,
            GraphDataVariableDeclarationModel model)
        {
            m_DataSource = dataSource;
            m_Model = model;
            UndoString = "Set Shader Declaration";
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SetShaderDeclarationCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                command.m_Model.ShaderDeclaration = command.m_DataSource;
                graphUpdater.MarkChanged(command.m_Model);
            }
        }
    }
}
