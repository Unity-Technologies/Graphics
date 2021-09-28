using System;
using System.Linq;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor.ShaderGraph.Drawing.Inspector;

namespace UnityEditor.Rendering.HighDefinition
{
    [SGPropertyDrawer(typeof(DiffusionProfileSettings))]
    class ShaderGraphDiffusionProfilePropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(DiffusionProfileSettings newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            DiffusionProfileSettings fieldToDraw,
            string labelName,
            out VisualElement propertyColorField,
            int indentLevel = 0)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(DiffusionProfileSettings) };

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((DiffusionProfileSettings)evt.newValue); });
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
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] { newValue }),
                (DiffusionProfileSettings)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [CustomPropertyDrawer(typeof(DiffusionProfileSettings))]
    class UIDiffusionProfilePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label);
            DiffusionProfileMaterialUI.DrawDiffusionProfileWarning(property.objectReferenceValue as DiffusionProfileSettings);
        }
    }
}
