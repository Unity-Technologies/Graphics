using System;
using System.Reflection;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(ToggleData))]
    class ToggleDataPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(ToggleData newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            ToggleData fieldToDraw,
            string labelName,
            out VisualElement propertyToggle,
            int indentLevel = 0)
        {
            var row = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            // Create and assign toggle as out variable here so that callers can also do additional work with enabling/disabling if needed
            propertyToggle = new Toggle();
            row.Add((Toggle)propertyToggle, (toggle) =>
            {
                toggle.value = fieldToDraw.isOn;
            });

            if (valueChangedCallback != null)
            {
                var toggle = (Toggle)propertyToggle;
                toggle.OnToggleChanged(evt => valueChangedCallback(new ToggleData(evt.newValue)));
            }

            row.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return row;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newBoolValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] { newBoolValue }),
                (ToggleData)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
