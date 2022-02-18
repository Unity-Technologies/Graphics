using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class SetGraphTypeValueCommand : UndoableCommand
    {
        GraphDataNodeModel m_GraphDataNodeModel;
        string m_PortName;
        float[] m_Values;

        public SetGraphTypeValueCommand(GraphDataNodeModel graphDataNodeModel, string portName, float[] values)
        {
            m_GraphDataNodeModel = graphDataNodeModel;
            m_PortName = portName;
            m_Values = values;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphViewStateComponent graphViewState,
            PreviewManager previewManager,
            SetGraphTypeValueCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphViewState, command);
            }

            if (!command.m_GraphDataNodeModel.TryGetNodeWriter(out var nodeWriter)) return;

            for (var i = 0; i < command.m_Values.Length; i++)
            {
                nodeWriter.SetPortField(command.m_PortName,$"c{i}", command.m_Values[i]);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                graphUpdater.MarkChanged(command.m_GraphDataNodeModel);
            }
        }
    }
}
