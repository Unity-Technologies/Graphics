using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SetVariableSettingCommand : UndoableCommand
    {
        readonly GraphDataVariableDeclarationModel m_Model;
        readonly GraphDataVariableSetting m_Setting;
        readonly object m_Value;

        public SetVariableSettingCommand(GraphDataVariableDeclarationModel model, GraphDataVariableSetting setting, object value)
        {
            m_Model = model;
            m_Setting = setting;
            m_Value = value;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SetVariableSettingCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using var graphUpdater = graphModelState.UpdateScope;
            // TODO: The model can and should do this
            command.m_Setting.Setter(command.m_Value);
            graphUpdater.MarkChanged(command.m_Model);
        }
    }
}
