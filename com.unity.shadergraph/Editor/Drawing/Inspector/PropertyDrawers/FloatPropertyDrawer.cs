using System;
using System.Reflection;
using Data.Interfaces;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(float))]
    class FloatPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(float newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            float fieldToDraw,
            string labelName,
            out VisualElement propertyFloatField,
            int indentLevel = 0)
        {
            var floatField = new FloatField {value = fieldToDraw};

            if (valueChangedCallback != null)
            {
                floatField.RegisterValueChangedCallback(evt => { valueChangedCallback((float) evt.newValue); });
            }

            propertyFloatField = floatField;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyFloatField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (float) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
