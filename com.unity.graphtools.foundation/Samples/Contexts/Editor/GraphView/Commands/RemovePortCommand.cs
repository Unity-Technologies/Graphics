using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    class RemovePortCommand : ModelCommand<IVariableNodeModel>
    {
        const string k_UndoStringSingular = "Remove Port";

        readonly PortDirection m_PortDirection;
        readonly PortOrientation m_PortOrientation;

        public RemovePortCommand(PortDirection direction, PortOrientation orientation, params IVariableNodeModel[] nodes)
            : base(k_UndoStringSingular, k_UndoStringSingular, nodes)
        {
            m_PortDirection = direction;
            m_PortOrientation = orientation;
        }

        public static void DefaultHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, RemovePortCommand command)
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
                    nodeModel.RemovePort(command.m_PortOrientation, command.m_PortDirection);

                updater.MarkChanged(command.Models.Cast<NodeModel>());
            }
        }
    }
}
