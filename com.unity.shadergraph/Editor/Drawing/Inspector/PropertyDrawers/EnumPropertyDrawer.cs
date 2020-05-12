using System;
using System.Reflection;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(Enum))]
    class EnumPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Enum newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Enum fieldToDraw,
            string labelName,
            Enum defaultValue,
            out VisualElement propertyVisualElement,
            int indentLevel = 0)
        {
            var row = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            propertyVisualElement = new EnumField(defaultValue);
            row.Add((EnumField)propertyVisualElement, (field) =>
            {
                field.value = fieldToDraw;
            });

            if (valueChangedCallback != null)
            {
                var enumField = (EnumField) propertyVisualElement;
                enumField.RegisterValueChangedCallback(evt => valueChangedCallback(evt.newValue));
            }

            return row;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            InspectableAttribute attribute)
        {
            return this.CreateGUI(newEnumValue =>
                    propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newEnumValue}),
                (Enum) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                (Enum) attribute.defaultValue,
                out var propertyVisualElement);
        }
    }
}
