using System;
using System.Reflection;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(string))]
    class TextPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(string newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            string fieldToDraw,
            string labelName,
            out VisualElement propertyTextField,
            int indentLevel = 0)
        {
            var propertyRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            propertyTextField = new TextField(512, false, false, ' ') { isDelayed = true };
            propertyRow.Add((TextField)propertyTextField,
                textField =>
                {
                    textField.value = fieldToDraw;
                });

            if (valueChangedCallback != null)
            {
                var textField = (TextField) propertyTextField;
                textField.RegisterValueChangedCallback(evt => valueChangedCallback(evt.newValue));
            }

            propertyRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return propertyRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newStringValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newStringValue}),
                (string) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
