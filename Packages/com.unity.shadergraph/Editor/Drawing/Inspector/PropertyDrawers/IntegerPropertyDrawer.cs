using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(int))]
    class IntegerPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(int newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            int fieldToDraw,
            string labelName,
            out VisualElement propertyFloatField,
            int indentLevel = 0,
            string tooltip = null)
        {
            var integerField = new IntegerField { value = fieldToDraw };

            if (valueChangedCallback != null)
            {
                integerField.RegisterValueChangedCallback(evt => { valueChangedCallback(evt.newValue); });
            }

            propertyFloatField = integerField;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.tooltip = tooltip;
            defaultRow.Add(propertyFloatField);

            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] { newValue }),
                (int)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }

        void IPropertyDrawer.DisposePropertyDrawer() { }
    }
}
