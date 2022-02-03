using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    class AddPortCommand : ModelCommand<IVariableNodeModel>
    {
        const string k_UndoStringSingular = "Add Port";

        readonly PortDirection m_PortDirection;
        readonly PortOrientation m_PortOrientation;
        readonly TypeHandle m_Type;

        public AddPortCommand(PortDirection direction, PortOrientation orientation, IVariableNodeModel node, TypeHandle type = default)
            : base(k_UndoStringSingular, k_UndoStringSingular, new[] { node })
        {
            m_PortDirection = direction;
            m_PortOrientation = orientation;
            m_Type = type;
        }

        public static void DefaultHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, AddPortCommand command)
        {
            if (!command.Models.Any() || command.m_PortDirection == PortDirection.None)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var updater = graphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                    nodeModel.AddPort(command.m_PortOrientation, command.m_PortDirection, command.m_Type);

                updater.MarkChanged(command.Models.Cast<NodeModel>());
            }
        }
    }
}
