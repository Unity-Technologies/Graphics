using System;
using System.Reflection;
// ReSharper disable once RedundantUsingDirective
using UnityEditor.UIElements; // This is needed for 2020.3
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A UI field to display a field from a <see cref="IGraphElementModel"/> or its surrogate, if it implements <see cref="IHasInspectorSurrogate"/>. Used by the <see cref="SerializedFieldsInspector"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value to display.</typeparam>
    class ModelSerializedFieldField<TValue> : ModelPropertyField<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelSerializedFieldField{TValue}"/> class.
        /// </summary>
        /// <param name="commandTarget">The dispatcher to use to dispatch commands when the field is edited.</param>
        /// <param name="model">The inspected model.</param>
        /// <param name="inspectedObject">The model that owns the field.</param>
        /// <param name="inspectedField">The inspected field.</param>
        /// <param name="fieldTooltip">The tooltip for the field.</param>
        public ModelSerializedFieldField(
            ICommandTarget commandTarget,
            IModel model,
            object inspectedObject,
            FieldInfo inspectedField,
            string fieldTooltip)
            : base(commandTarget, model, inspectedField.Name, null, fieldTooltip)
        {
            m_ValueGetter = MakeFieldValueGetter(inspectedField, inspectedObject);

            switch (m_Field)
            {
                case null:
                    break;

                case PopupField<string> _:
                    RegisterChangedCallback<string>(evt => Enum.Parse(typeof(TValue), evt.newValue),
                        inspectedObject, inspectedField);
                    break;

                case BaseField<Enum> _:
                    RegisterChangedCallback<Enum>(evt => evt.newValue, inspectedObject, inspectedField);
                    break;

                default:
                    RegisterChangedCallback<TValue>(evt => evt.newValue, inspectedObject, inspectedField);
                    break;
            }
        }

        void RegisterChangedCallback<TCallbackValue>(Func<ChangeEvent<TCallbackValue>, object> valueExtractor,
            object inspectedObject, FieldInfo inspectedField)
        {
            if (Model is IGraphElementModel graphElementModel)
            {
                m_Field.RegisterCallback<ChangeEvent<TCallbackValue>, ModelPropertyField<TValue>>(
                    (e, f) =>
                    {
                        var newValue = valueExtractor(e);
                        var command = new SetInspectedGraphElementModelFieldCommand(newValue, graphElementModel, inspectedObject, inspectedField);
                        f.CommandTarget.Dispatch(command);
                    }, this);
            }
            else if (Model is IGraphModel graphModel)
            {
                m_Field.RegisterCallback<ChangeEvent<TCallbackValue>, ModelPropertyField<TValue>>(
                    (e, f) =>
                    {
                        var newValue = valueExtractor(e);
                        var command = new SetInspectedGraphModelFieldCommand(newValue, graphModel, inspectedObject, inspectedField);
                        f.CommandTarget.Dispatch(command);
                    }, this);
            }
        }

        static Func<TValue> MakeFieldValueGetter(FieldInfo fieldInfo, object inspectedObject)
        {
            if (fieldInfo != null && inspectedObject != null)
            {
                var usePropertyAttribute = fieldInfo.GetCustomAttribute<InspectorUsePropertyAttribute>();
                if (usePropertyAttribute != null)
                {
                    var propertyInfo = inspectedObject.GetType().GetProperty(usePropertyAttribute.PropertyName);
                    if (propertyInfo != null)
                    {
                        Debug.Assert(typeof(TValue) == propertyInfo.PropertyType);
                        return () => (TValue)propertyInfo.GetMethod.Invoke(inspectedObject, null);
                    }
                }

                Debug.Assert(typeof(TValue) == fieldInfo.FieldType);
                return () => (TValue)fieldInfo.GetValue(inspectedObject);
            }

            return null;
        }
    }
}
