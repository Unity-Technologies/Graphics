using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SetVariableSettingCommand : UndoableCommand
    {
        readonly GraphDataVariableDeclarationModel m_Model;
        readonly VariableSetting m_Setting;
        readonly object m_Value;

        public SetVariableSettingCommand(GraphDataVariableDeclarationModel model, VariableSetting setting, object value)
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
            command.m_Setting.Set(command.m_Model, command.m_Value);
            graphUpdater.MarkChanged(command.m_Model);
        }
    }
}
