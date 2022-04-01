using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class SGModelPropertyField<TValue> : ModelPropertyField<TValue>
    {
        protected SGModelPropertyField(
            ICommandTarget commandTarget,
            IModel model,
            string propertyName,
            string label,
            string fieldTooltip)
            : base(commandTarget, model, propertyName, label, fieldTooltip)
        {

        }

        public SGModelPropertyField(
            ICommandTarget commandTarget,
            IModel model,
            string propertyName,
            string label,
            string fieldTooltip,
            Action<TValue, ModelPropertyField<TValue>> onValueChanged = null,
            Func<IModel, TValue> valueGetter = null)
            : base(commandTarget, model, propertyName, label, fieldTooltip, onValueChanged, valueGetter)
        {

        }

        public SGModelPropertyField(
            ICommandTarget commandTarget,
            IModel model,
            string propertyName,
            string label,
            string fieldTooltip,
            Type commandType,
            Func<IModel, TValue> valueGetter = null)
            : base(commandTarget, model, propertyName, label, fieldTooltip, commandType, valueGetter)
        {

        }

        public VisualElement PropertyField
        {
            get => m_Field;
            set => m_Field = value;
        }
    }
}
