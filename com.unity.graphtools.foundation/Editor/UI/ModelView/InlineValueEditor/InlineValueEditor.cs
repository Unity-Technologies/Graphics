using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Utility to build a value editor.
    /// </summary>
    public static class InlineValueEditor
    {
        public static readonly string ussClassName = "ge-inline-value-editor";

        // PF TODO:
        // - It is hard to put additional information in ConstantEditorBuilder
        // - Maybe the constant editor should be a IModelView?

        /// <summary>
        /// Creates an editor for a constant.
        /// </summary>
        /// <param name="uiContext">The view in which the constant editor will be displayed.</param>
        /// <param name="ownerModel">The graph element model that owns the constant, if any.</param>
        /// <param name="constant">The constant.</param>
        /// <param name="modelIsLocked">Whether the node owning the constant, if any, is locked.</param>
        /// <param name="label">The label to display in front of the editor.</param>
        /// <returns>A VisualElement that contains an editor for the constant.</returns>
        public static BaseModelPropertyField CreateEditorForConstant(
            IRootView uiContext, IGraphElementModel ownerModel, IConstant constant,
            bool modelIsLocked, string label = null)
        {
            var ext = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(
                uiContext.GetType(), constant.GetType(),
                ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

            if (ext != null)
            {
                var constantBuilder = new ConstantEditorBuilder(uiContext, modelIsLocked, ownerModel, label);
                var editor = (BaseModelPropertyField)ext.Invoke(null, new object[] { constantBuilder, constant });
                if (editor != null)
                {
                    editor.AddToClassList(ussClassName);
                    return editor;
                }
            }

            Debug.Log($"Could not draw Editor GUI for node of type {constant.Type}");
            return new MissingFieldEditor(uiContext, label ?? $"<Unknown> {constant.GetType()}");
        }

        /// <summary>
        /// Creates an editor for a constant.
        /// </summary>
        /// <param name="uiContext">The view in which the constant editor will be displayed.</param>
        /// <param name="ownerModel">The graph element model that owns the constant, if any.</param>
        /// <param name="constant">The constant.</param>
        /// <param name="onValueChanged">An action to call when the user has finished editing the value.</param>
        /// <param name="modelIsLocked">Whether the node owning the constant, if any, is locked.</param>
        /// <param name="label">The label to display in front of the editor.</param>
        /// <returns>A VisualElement that contains an editor for the constant.</returns>
        [Obsolete]
        public static VisualElement CreateEditorForConstant(
            IRootView uiContext, IPortModel ownerModel, IConstant constant,
            Action<IChangeEvent> onValueChanged, bool modelIsLocked, string label = null)
        {
            var ext = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(
                uiContext.GetType(), constant.GetType(),
                ConstantEditorBuilder.OldFilterMethods, ConstantEditorBuilder.KeySelector);

            if (ext != null)
            {
                var constantBuilder = new ConstantEditorBuilder(onValueChanged, uiContext, modelIsLocked, ownerModel, label);
                var editor = (VisualElement)ext.Invoke(null, new object[] { constantBuilder, constant });
                editor.AddToClassList(ussClassName);
                return editor;
            }

            Debug.Log($"Could not draw Editor GUI for node of type {constant.Type}");
            return new Label(label ?? "<Unknown>");
        }
    }
}
