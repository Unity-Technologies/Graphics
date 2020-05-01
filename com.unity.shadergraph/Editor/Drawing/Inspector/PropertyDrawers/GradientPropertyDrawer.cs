using System;
using System.Reflection;
using Data.Interfaces;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(Gradient))]
    class GradientPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Gradient newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Gradient fieldToDraw,
            string labelName,
            out VisualElement propertyGradientField,
            int indentLevel = 0)
        {
            var objectField = new GradientField { value = fieldToDraw};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Gradient) evt.newValue); });
            }

            propertyGradientField = objectField;

            // Any core widgets used by the inspector over and over should come from some kind of factory
            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyGradientField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Gradient) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
