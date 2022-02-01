using System;
using UnityEditor.UIElements;
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
        /// <param name="commandTargetView">The view hosting this field.</param>
        /// <param name="model">The model that owns the field.</param>
        /// <param name="fieldName">The field name.</param>
        public ModelSerializedFieldField(
            ICommandTarget commandTargetView,
            IGraphElementModel model,
            string fieldName)
            : base(commandTargetView, model, fieldName, null)
        {
            m_ValueGetter = MakeFieldValueGetter(Model, fieldName);

            switch (m_Field)
            {
                case null:
                    break;

                case PopupField<string> _:
                    m_Field.RegisterCallback<ChangeEvent<string>, ModelPropertyField<TValue>>(
                        (e, f) =>
                        {
                            var newValue = Enum.Parse(typeof(TValue), e.newValue);
                            var command = new SetModelFieldCommand(newValue, f.Model, fieldName);
                            f.CommandTargetView.Dispatch(command);
                        }, this);
                    break;

                default:
                    m_Field.RegisterCallback<ChangeEvent<TValue>, ModelPropertyField<TValue>>(
                        (e, f) =>
                        {
                            var command = new SetModelFieldCommand(e.newValue, f.Model, fieldName);
                            f.CommandTargetView.Dispatch(command);
                        }, this);
                    break;
            }
        }

        static Func<TValue> MakeFieldValueGetter(IGraphElementModel model, string fieldName)
        {
            var target = model is IHasInspectorSurrogate hasInspectorSurrogate ? hasInspectorSurrogate.Surrogate : model;
            var fieldInfo = SerializedFieldsInspector.GetInspectableField(target, fieldName);
            if (fieldInfo != null)
            {
                Debug.Assert(typeof(TValue) == fieldInfo.FieldType);
                return () => (TValue)fieldInfo.GetValue(target);
            }

            return null;
        }
    }
}
