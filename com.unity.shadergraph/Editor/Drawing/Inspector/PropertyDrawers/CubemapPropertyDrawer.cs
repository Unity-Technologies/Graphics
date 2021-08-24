using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(Cubemap))]
    class CubemapPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Cubemap newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Cubemap fieldToDraw,
            string labelName,
            out VisualElement propertyCubemapField,
            int indentLevel = 0)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Cubemap) };

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Cubemap)evt.newValue); });
            }

            propertyCubemapField = objectField;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyCubemapField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] { newValue }),
                (Cubemap)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
