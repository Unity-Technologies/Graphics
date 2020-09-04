using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(Texture))]
    class Texture2DPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Texture newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Texture fieldToDraw,
            string labelName,
            out VisualElement propertyColorField,
            int indentLevel = 0)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Texture)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Texture) evt.newValue); });
            }

            propertyColorField = objectField;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyColorField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Texture) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
