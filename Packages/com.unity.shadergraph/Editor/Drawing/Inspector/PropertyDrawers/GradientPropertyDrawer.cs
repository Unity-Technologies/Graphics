using System;
using System.Reflection;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(Gradient))]
    class GradientPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Gradient newValue);

        ValueChangedCallback m_ValueChangedCallback;
        GradientObject m_GradientObject;
        SerializedObject m_SerializedObject;
        GradientField m_Field;

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Gradient fieldToDraw,
            string labelName,
            out VisualElement propertyGradientField,
            int indentLevel = 0)
        {
            m_ValueChangedCallback = valueChangedCallback;

            m_GradientObject = ScriptableObject.CreateInstance<GradientObject>();
            m_GradientObject.gradient = new Gradient();
            m_GradientObject.gradient.SetKeys(fieldToDraw.colorKeys, fieldToDraw.alphaKeys);
            m_GradientObject.gradient.mode = fieldToDraw.mode;
            m_SerializedObject = new SerializedObject(m_GradientObject);

            m_Field = new GradientField { value = m_GradientObject.gradient, colorSpace = ColorSpace.Linear, hdr = true };
            m_Field.RegisterValueChangedCallback(OnFieldValueChanged);

            propertyGradientField = m_Field;

            // Any core widgets used by the inspector over and over should come from some kind of factory
            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyGradientField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));

            defaultRow.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            defaultRow.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            return defaultRow;
        }

        void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnFieldValueChanged(ChangeEvent<Gradient> evt)
        {
            m_SerializedObject.Update();
            if (evt.newValue.Equals(m_GradientObject.gradient))
                return;

            // Register undo on the temporary GradientObject rather than the graph so the open picker isn't torn down on undo.
            Undo.RegisterCompleteObjectUndo(m_GradientObject, "Modify Gradient Stop");

            m_GradientObject.gradient.SetKeys(evt.newValue.colorKeys, evt.newValue.alphaKeys);
            m_GradientObject.gradient.mode = evt.newValue.mode;
            m_SerializedObject.ApplyModifiedProperties();

            m_ValueChangedCallback?.Invoke(m_GradientObject.gradient);
        }

        void OnUndoRedoPerformed()
        {
            if (m_GradientObject == null || m_Field == null)
                return;

            m_SerializedObject.Update();
            m_ValueChangedCallback?.Invoke(m_GradientObject.gradient);
            m_Field.SetValueWithoutNotify(m_GradientObject.gradient);
        }

        void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] { newValue }),
                (Gradient)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }

        void IPropertyDrawer.DisposePropertyDrawer() { }
    }
}
