using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Types;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SetGraphTypeValueCommand : UndoableCommand
    {
        GraphDataNodeModel m_GraphDataNodeModel;
        string m_PortName;

        GraphType.Length m_Length;
        GraphType.Height m_Height;
        float[] m_Values;

        public SetGraphTypeValueCommand(GraphDataNodeModel graphDataNodeModel,
            string portName,
            GraphType.Length length,
            GraphType.Height height,
            params float[] values)
        {
            m_GraphDataNodeModel = graphDataNodeModel;
            m_PortName = portName;

            m_Length = length;
            m_Height = height;
            m_Values = values;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphViewState,
            PreviewManager previewManager,
            SetGraphTypeValueCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphViewState, command);
            }

            if (!command.m_GraphDataNodeModel.TryGetNodeWriter(out var nodeWriter)) return;
            var field = nodeWriter.GetPort(command.m_PortName).GetTypeField();
            GraphTypeHelpers.SetComponents(field, 0, command.m_Values);

            object propertyBlockValue = command.m_Values[0];

            // Handle matrices
            if (command.m_Height > GraphType.Height.One)
            {
                // Square matrix, fit it inside of a Matrix4x4 since that's what MaterialPropertyBlock wants

                var matrixValue = Matrix4x4.zero;
                var size = (int)command.m_Height;

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

    class SetGradientTypeValueCommand : UndoableCommand
    {
        GraphDataNodeModel m_GraphDataNodeModel;
        string m_PortName;
        Gradient m_Value;

        public SetGradientTypeValueCommand(GraphDataNodeModel graphDataNodeModel, string portName, Gradient value)
        {
            m_GraphDataNodeModel = graphDataNodeModel;
            m_PortName = portName;
            m_Value = value;
        }

        public static void DefaultCommandHandler(UndoStateComponent undoState,
            GraphModelStateComponent graphViewState,
            PreviewManager previewManager,
            SetGradientTypeValueCommand command
        )
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveSingleState(graphViewState, command);
            }

            if (!command.m_GraphDataNodeModel.TryGetNodeWriter(out var nodeWriter)) return;
            var portWriter = nodeWriter.GetPort(command.m_PortName);

            GradientTypeHelpers.SetGradient(portWriter.GetTypeField(), command.m_Value);
            previewManager.OnLocalPropertyChanged(command.m_GraphDataNodeModel.graphDataName, command.m_PortName, command.m_Value);

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                graphUpdater.MarkChanged(command.m_GraphDataNodeModel);
            }
        }
    }
}
