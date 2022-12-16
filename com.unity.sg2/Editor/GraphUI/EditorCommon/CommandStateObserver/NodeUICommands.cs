using Unity.GraphToolsFoundation.Editor;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine;
using Unity.CommandStateObserver;
using UnityEditor.ShaderGraph.Defs;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SetGraphTypeValueCommand : UndoableCommand
    {
        readonly SGNodeModel m_SGNodeModel;
        readonly string m_PortName;
        readonly GraphType.Length m_Length;
        readonly GraphType.Height m_Height;
        readonly float[] m_Values;

        public SetGraphTypeValueCommand(SGNodeModel sgNodeModel,
            string portName,
            GraphType.Length length,
            GraphType.Height height,
            params float[] values)
        {
            m_SGNodeModel = sgNodeModel;
            m_PortName = portName;

            m_Length = length;
            m_Height = height;
            m_Values = values;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphViewState,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            SetGraphTypeValueCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveState(graphViewState);
            }

            if (!command.m_SGNodeModel.TryGetNodeHandler(out var nodeHandler)) return;
            var field = nodeHandler.GetPort(command.m_PortName).GetTypeField();
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

            previewUpdateDispatcher.OnLocalPropertyChanged(command.m_SGNodeModel.graphDataName, command.m_PortName, propertyBlockValue);

            using var graphUpdater = graphViewState.UpdateScope;
            graphUpdater.MarkChanged(command.m_SGNodeModel);
        }
    }

    class SetGradientTypeValueCommand : UndoableCommand
    {
        readonly SGNodeModel m_SGNodeModel;
        readonly string m_PortName;
        readonly Gradient m_Value;

        public SetGradientTypeValueCommand(
            SGNodeModel sgNodeModel,
            string portName,
            Gradient value)
        {
            m_SGNodeModel = sgNodeModel;
            m_PortName = portName;
            m_Value = value;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphViewState,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            SetGradientTypeValueCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveState(graphViewState);
            }

            if (!command.m_SGNodeModel.TryGetNodeHandler(out var nodeHandler)) return;
            var portWriter = nodeHandler.GetPort(command.m_PortName);

            GradientTypeHelpers.SetGradient(portWriter.GetTypeField(), command.m_Value);
            previewUpdateDispatcher.OnLocalPropertyChanged(command.m_SGNodeModel.graphDataName, command.m_PortName, command.m_Value);

            using var graphUpdater = graphViewState.UpdateScope;
            graphUpdater.MarkChanged(command.m_SGNodeModel);
        }
    }

    class SetPortOptionCommand : UndoableCommand
    {
        readonly SGNodeModel m_SGNodeModel;
        readonly string m_PortName;
        readonly int m_OptionIndex;

        public SetPortOptionCommand(SGNodeModel sgNodeModel, string portName, int optionIndex)
        {
            m_SGNodeModel = sgNodeModel;
            m_PortName = portName;
            m_OptionIndex = optionIndex;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphViewState,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            SetPortOptionCommand command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveState(graphViewState);
            }

            command.m_SGNodeModel.SetPortOption(command.m_PortName, command.m_OptionIndex);
            previewUpdateDispatcher.OnListenerConnectionChanged(command.m_SGNodeModel.graphDataName);

            using var graphUpdater = graphViewState.UpdateScope;
            graphUpdater.MarkChanged(command.m_SGNodeModel);
        }
    }

    abstract class SetNodeFieldCommand<T> : UndoableCommand
    {
        readonly SGNodeModel m_SGNodeModel;
        readonly string m_FieldName;
        readonly T m_Value;

        public SetNodeFieldCommand(SGNodeModel SGNodeModel, string fieldName, T value)
        {
            m_SGNodeModel = SGNodeModel;
            m_FieldName = fieldName;
            m_Value = value;
        }

        public static void DefaultCommandHandler(
            UndoStateComponent undoState,
            GraphModelStateComponent graphViewState,
            PreviewUpdateDispatcher previewUpdateDispatcher,
            SetNodeFieldCommand<T> command)
        {
            using (var undoUpdater = undoState.UpdateScope)
            {
                undoUpdater.SaveState(graphViewState);
            }

            if (!command.m_SGNodeModel.TryGetNodeHandler(out var nodeHandler)) return;
            var field = nodeHandler.GetField<T>(command.m_FieldName);
            field.SetData(command.m_Value);

            previewUpdateDispatcher.OnListenerConnectionChanged(command.m_SGNodeModel.graphDataName);

            using var graphUpdater = graphViewState.UpdateScope;
            graphUpdater.MarkChanged(command.m_SGNodeModel);
        }
    }

    class SetSwizzleMaskCommand : SetNodeFieldCommand<string>
    {
        public SetSwizzleMaskCommand(SGNodeModel SGNodeModel, string fieldName, string value)
            : base(SGNodeModel, fieldName, value) { }
    }

    class SetCoordinateSpaceCommand : SetNodeFieldCommand<CoordinateSpace>
    {
        public SetCoordinateSpaceCommand(SGNodeModel SGNodeModel, string fieldName, CoordinateSpace value)
            : base(SGNodeModel, fieldName, value) { }
    }

    class SetConversionTypeCommand : SetNodeFieldCommand<GraphDelta.ConversionType>
    {
        public SetConversionTypeCommand(SGNodeModel SGNodeModel, string fieldName, GraphDelta.ConversionType value)
            : base(SGNodeModel, fieldName, value) { }
    }
}
