using System;
using System.Collections.Generic;
using Unity.GraphToolsFoundation.Editor;
using Unity.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class SGModelPropertyField<TValue> : ModelPropertyField<TValue>
    {
        protected SGModelPropertyField(
            ICommandTarget commandTarget,
            IEnumerable<Model> models,
            string propertyName,
            string label,
            string fieldTooltip)
            : base(commandTarget, models, propertyName, label, fieldTooltip)
        {

        }

        public SGModelPropertyField(
            ICommandTarget commandTarget,
            IEnumerable<Model> models,
            string propertyName,
            string label,
            string fieldTooltip,
            Action<TValue, ModelPropertyField<TValue>> onValueChanged = null,
            Func<Model, TValue> valueGetter = null)
            : base(commandTarget, models, propertyName, label, fieldTooltip, onValueChanged, valueGetter)
        {

        }

        public SGModelPropertyField(
            ICommandTarget commandTarget,
            IEnumerable<Model> models,
            string propertyName,
            string label,
            string fieldTooltip,
            Type commandType,
            Func<Model, TValue> valueGetter = null)
            : base(commandTarget, models, propertyName, label, fieldTooltip, commandType, valueGetter)
        {

        }

        public VisualElement PropertyField
        {
            get => m_Field;
            set => m_Field = value;
        }
    }
}
