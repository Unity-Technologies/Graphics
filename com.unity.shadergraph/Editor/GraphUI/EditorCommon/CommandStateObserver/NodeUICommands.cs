using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    internal class SetGraphTypeValueCommand : UndoableCommand
    {
        GraphDataNodeModel m_GraphDataNodeModel;
        string m_PortName;

        GraphType.Length m_Length;
        GraphType.Height m_Height;
        float[] m_Values;

        public SetGraphTypeValueCommand(GraphDataNodeModel graphDataNodeModel, string portName, GraphType.Length length, GraphType.Height height, float[] values)
        {
            m_GraphDataNodeModel = graphDataNodeModel;
            m_PortName = portName;

            m_Length = length;
            m_Height = height;
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
                nodeWriter.SetPortField(command.m_PortName, $"c{i}", command.m_Values[i]);
            }

            object propertyBlockValue = command.m_Values[0];

            // Handle matrices
            if (command.m_Height > GraphType.Height.One)
            {
                // Square matrix, fit it inside of a Matrix4x4 since that's what MaterialPropertyBlock wants

                var matrixValue = Matrix4x4.zero;
                var size = (int)command.m_Length;

                for (var i = 0; i < size; i++)
                {
                    for (var j = 0; j < size; j++)
                    {
                        matrixValue[i, j] = command.m_Values[i * size + j];
                    }
                }

                propertyBlockValue = matrixValue;
            }

            previewManager.OnLocalPropertyChanged(command.m_GraphDataNodeModel.graphDataName, command.m_PortName, propertyBlockValue);

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                graphUpdater.MarkChanged(command.m_GraphDataNodeModel);
            }
        }
    }
}
