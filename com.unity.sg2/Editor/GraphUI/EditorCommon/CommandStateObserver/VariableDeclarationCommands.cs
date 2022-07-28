using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class SetVariableSettingsCommand : UndoableCommand
    {
        readonly GraphDataVariableDeclarationModel m_Model;

        // TODO: This doesn't seem right.
        // Really the solution is probably to move this further into the model and not the command handler.
        readonly VariableUpdater m_Callback;

        public delegate void VariableUpdater(GraphDataVariableDeclarationModel model);

        public SetVariableSettingsCommand(
            GraphDataVariableDeclarationModel model,
            VariableUpdater callback
        )
        {
            m_Model = model;
            m_Callback = callback;
        }

        public static SetVariableSettingsCommand SetTypeSubField<T>(
            GraphDataVariableDeclarationModel model,
            string key,
            T value
        ) => new(model, m => m.SetTypeSubField(key, value));

        public static SetVariableSettingsCommand SetPropertyDescriptionSubField<T>(
            GraphDataVariableDeclarationModel model,
            string key,
            T value
        ) => new(model, m => m.SetPropSubField(key, value));

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphModelState,
            SetVariableSettingsCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                command.m_Callback(command.m_Model);
                graphUpdater.MarkChanged(command.m_Model);
            }
        }
    }
}
