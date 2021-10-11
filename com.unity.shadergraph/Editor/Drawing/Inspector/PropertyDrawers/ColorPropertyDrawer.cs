using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(Color))]
    class ColorPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Color newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Color fieldToDraw,
            string labelName,
            out VisualElement propertyColorField,
            int indentLevel = 0)
        {
            var colorField = new ColorField { value = fieldToDraw, showEyeDropper = false, hdr = false };

            if (valueChangedCallback != null)
            {
                colorField.RegisterValueChangedCallback(evt => { valueChangedCallback((Color)evt.newValue); });
            }

            propertyColorField = colorField;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyColorField);
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] { newValue }),
                (Color)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }

        void IPropertyDrawer.DisposePropertyDrawer() { }
    }
}
