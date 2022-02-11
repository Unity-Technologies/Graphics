using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class AddPortCommand : ModelCommand<CustomizableNodeModel>
    {
        bool m_Output;
        string m_Name;
        TypeHandle m_Type;

        public AddPortCommand(bool output, string name, TypeHandle type, IReadOnlyList<CustomizableNodeModel> models)
            : base("Add Port", "Add Ports", models)
        {
            m_Output = output;
            m_Name = name;
            m_Type = type;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphViewStateComponent graphViewState,
            AddPortCommand command
        )
        {
            undoState.UpdateScope.SaveSingleState(graphViewState, command);
            using var graphUpdater = graphViewState.UpdateScope;

            foreach (var customizableNodeModel in command.Models)
            {
                if (command.m_Output)
                {
                    customizableNodeModel.AddCustomDataOutputPort(command.m_Name, command.m_Type);
                }
                else
                {
                    customizableNodeModel.AddCustomDataInputPort(command.m_Name, command.m_Type);
                }

                graphUpdater.MarkChanged(customizableNodeModel);
            }
        }
    }
}
