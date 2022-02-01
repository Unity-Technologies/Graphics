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
        // PF TODO: This API and ConstantEditorBuilder is badly designed.

        /// <summary>
        /// Creates an editor for a constant.
        /// </summary>
        /// <param name="uiContext">The view in which the editor constant will be displayed.</param>
        /// <param name="portModel">The port that owns the constant, if any.</param>
        /// <param name="constant">The constant.</param>
        /// <param name="onValueChanged">An action to call when the user has finished editing the value.</param>
        /// <param name="modelIsLocked">Whether the node owning the constant, if any, is locked.</param>
        /// <returns>A VisualElement that contains an editor for the constant.</returns>
        public static VisualElement CreateEditorForConstant(
            IModelView uiContext, IPortModel portModel, IConstant constant,
            Action<IChangeEvent, object> onValueChanged, bool modelIsLocked)
        {
            Action<IChangeEvent> myValueChanged = evt =>
            {
                if (evt != null) // Enum editor sends null
                {
                    var p = evt.GetType().GetProperty("newValue");
                    var newValue = p.GetValue(evt);
                    if (constant is IStringWrapperConstantModel stringWrapperConstantModel)
                        stringWrapperConstantModel.StringValue = (string)newValue;
                    else
                        onValueChanged(evt, newValue);
                }
            };

            var ext = ExtensionMethodCache<IConstantEditorBuilder>.GetExtensionMethod(
                uiContext.GetType(), constant.GetType(),
                ConstantEditorBuilder.FilterMethods, ConstantEditorBuilder.KeySelector);

            if (ext != null)
            {
                var constantBuilder = new ConstantEditorBuilder(myValueChanged, uiContext.GraphTool?.Dispatcher, modelIsLocked, portModel);
                return (VisualElement)ext.Invoke(null, new object[] { constantBuilder, constant });
            }

            Debug.Log($"Could not draw Editor GUI for node of type {constant.Type}");
            return new Label("<Unknown>");
        }
    }
}
