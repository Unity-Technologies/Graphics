using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(IEnumerable<string>))]
    class DropdownPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(int newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            IEnumerable<string> fieldToDraw,
            string labelName,
            out VisualElement textArrayField,
            int indentLevel = 0)
        {
            var propertyRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            textArrayField = new PopupField<string>(fieldToDraw.ToList(), 0);
            propertyRow.Add(textArrayField);
            var popupField = (PopupField<string>)textArrayField;
            popupField.RegisterValueChangedCallback(evt =>
            {
                valueChangedCallback(popupField.index);
            });
            propertyRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));

            return propertyRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newSelectedIndex => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newSelectedIndex}),
                (IEnumerable<string>)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var textArrayField);
        }
    }
}
