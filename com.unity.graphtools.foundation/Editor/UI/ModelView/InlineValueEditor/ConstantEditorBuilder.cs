using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds information needed when building a constant editor.
    /// </summary>
    public class ConstantEditorBuilder : IConstantEditorBuilder
    {
        /// <inheritdoc />
        [Obsolete]
        public Action<IChangeEvent> OnValueChanged { get; }

        /// <inheritdoc />
        public IRootView CommandTarget { get; }

        /// <inheritdoc />
        public bool ConstantIsLocked { get; }

        /// <inheritdoc />
        public IGraphElementModel ConstantOwner { get; }

        /// <inheritdoc />
        public string Label { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantEditorBuilder"/> class.
        /// </summary>
        /// <param name="commandTarget">The view used to dispatch commands.</param>
        /// <param name="constantIsLocked">Whether the constant is locked.</param>
        /// <param name="constantOwner">The graph element model that owns the constant, if any.</param>
        /// <param name="label">The label to display in front of the field.</param>
        public ConstantEditorBuilder(IRootView commandTarget,
            bool constantIsLocked, IGraphElementModel constantOwner, string label)
        {
            CommandTarget = commandTarget;
            ConstantIsLocked = constantIsLocked;
            ConstantOwner = constantOwner;
            Label = label;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantEditorBuilder"/> class.
        /// </summary>
        /// <param name="onValueChanged">The callback for when the editor value changes.</param>
        /// <param name="commandTarget">The view used to dispatch commands.</param>
        /// <param name="constantIsLocked">Whether the constant is locked.</param>
        /// <param name="portModel">The port model that owns the constant, if any.</param>
        /// <param name="label">The label to display in front of the field.</param>
        [Obsolete]
        public ConstantEditorBuilder(Action<IChangeEvent> onValueChanged,
            IRootView commandTarget,
            bool constantIsLocked, IPortModel portModel, string label)
        {
            OnValueChanged = onValueChanged;
            CommandTarget = commandTarget;
            ConstantIsLocked = constantIsLocked;
            ConstantOwner = portModel;
            Label = label;
        }

        /// <summary>
        /// Filters candidate methods for the one that satisfy the signature VisualElement MyFunctionName(IConstantEditorBuilder builder, ...).
        /// </summary>
        /// <remarks>For use with <see cref="ExtensionMethodCache{IConstantEditorBuilder}.GetExtensionMethod"/>.</remarks>
        /// <param name="method">The method.</param>
        /// <returns>True if the method satisfies the signature, false otherwise.</returns>
        public static bool FilterMethods(MethodInfo method)
        {
            // Looking for methods like : BaseModelPropertyField MyFunctionName(IConstantEditorBuilder builder, <NodeTypeToBuild> node)
            var parameters = method.GetParameters();
            return method.ReturnType == typeof(BaseModelPropertyField)
                && parameters.Length == 2
                && parameters[0].ParameterType == typeof(IConstantEditorBuilder);
        }

        internal static bool OldFilterMethods(MethodInfo method)
        {
            // Looking for methods like : VisualElement MyFunctionName(IConstantEditorBuilder builder, <NodeTypeToBuild> node)
            var parameters = method.GetParameters();
            return method.ReturnType == typeof(VisualElement)
                && parameters.Length == 2
                && parameters[0].ParameterType == typeof(IConstantEditorBuilder);
        }

        /// <summary>
        /// Selects the second parameter of the extension method as a key.
        /// </summary>
        /// <remarks>For use with <see cref="ExtensionMethodCache{IConstantEditorBuilder}.GetExtensionMethod"/>.</remarks>
        /// <param name="method">The method.</param>
        /// <returns>The second parameter of the method.</returns>
        public static Type KeySelector(MethodInfo method)
        {
            return method.GetParameters()[1].ParameterType;
        }
    }
}
