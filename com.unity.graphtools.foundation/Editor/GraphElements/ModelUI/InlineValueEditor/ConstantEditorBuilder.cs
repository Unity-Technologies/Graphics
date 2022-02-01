using System;
using System.Reflection;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information needed when building a constant editor.
    /// </summary>
    public class ConstantEditorBuilder : IConstantEditorBuilder
    {
        public Action<IChangeEvent> OnValueChanged { get; }
        public Dispatcher CommandDispatcher { get; }
        public bool ConstantIsLocked { get; }
        public IPortModel PortModel { get; }

        public ConstantEditorBuilder(Action<IChangeEvent> onValueChanged,
                                     Dispatcher commandDispatcher,
                                     bool constantIsLocked, IPortModel portModel)
        {
            OnValueChanged = onValueChanged;
            CommandDispatcher = commandDispatcher;
            ConstantIsLocked = constantIsLocked;
            PortModel = portModel;
        }

        // Looking for methods like : VisualElement MyFunctionName(IConstantEditorBuilder builder, <NodeTypeToBuild> node)
        public static bool FilterMethods(MethodInfo x)
        {
            var parameters = x.GetParameters();
            return x.ReturnType == typeof(VisualElement)
                && parameters.Length == 2
                && parameters[0].ParameterType == typeof(IConstantEditorBuilder);
        }

        public static Type KeySelector(MethodInfo x)
        {
            return x.GetParameters()[1].ParameterType;
        }
    }
}
