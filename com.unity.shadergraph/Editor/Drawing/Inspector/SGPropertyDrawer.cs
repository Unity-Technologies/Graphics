using System;
using System.Reflection;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Drawing.Inspector
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SGPropertyDrawer : Attribute
    {
        public Type propertyType { get; private set; }

        public SGPropertyDrawer(Type propertyType)
        {
            this.propertyType = propertyType;
        }
    }

       // Interface that should be implemented by any property drawer for the inspector view
    interface IPropertyDrawer
    {
        PropertyRow HandleProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute);
    }

    [SGPropertyDrawer(typeof(Enum))]
    class EnumPropertyDrawer : IPropertyDrawer
    {
        private delegate void EnumValueSetter(Enum newValue);

        private PropertyRow CreatePropertyRowForField(EnumValueSetter valueChangedCallback, Enum fieldToDraw, string labelName, Enum defaultValue)
        {
            var row = new PropertyRow(new Label(labelName));
            row.Add(new EnumField(defaultValue), (field) =>
            {
                field.value = fieldToDraw;
                field.RegisterValueChangedCallback(evt => valueChangedCallback(evt.newValue));
            });

            return row;
        }

        public PropertyRow HandleProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            var newPropertyRow = this.CreatePropertyRowForField(newEnumValue =>
                propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newEnumValue}),
                (Enum) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                (Enum) attribute.defaultValue);

            return newPropertyRow;
        }
    }

    [SGPropertyDrawer(typeof(ToggleData))]
    class BoolPropertyDrawer : IPropertyDrawer
    {
        private delegate void BoolValueSetter(ToggleData newValue);

        private PropertyRow CreatePropertyRowForField(BoolValueSetter valueChangedCallback, ToggleData fieldToDraw, string labelName)
        {
            var row = new PropertyRow(new Label(labelName));
            row.Add(new Toggle(), (toggle) =>
            {
                toggle.value = fieldToDraw.isOn;
                toggle.OnToggleChanged(evt => valueChangedCallback(new ToggleData(evt.newValue)));
            });

            return row;
        }

        public PropertyRow HandleProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            var newPropertyRow = this.CreatePropertyRowForField(newBoolValue =>
                propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newBoolValue}),
                (ToggleData) propertyInfo.GetValue(actualObject),
                attribute.labelName);

            return newPropertyRow;
        }
    }
}
