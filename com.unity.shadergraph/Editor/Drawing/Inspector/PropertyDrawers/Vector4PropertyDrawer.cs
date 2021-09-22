using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(Vector4))]
    class Vector4PropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Vector4 newValue);

        public Action preValueChangeCallback { get; set; }
        public Action postValueChangeCallback { get; set; }

        EventCallback<KeyDownEvent> m_KeyDownCallback;
        EventCallback<FocusOutEvent> m_FocusOutCallback;
        public int mUndoGroup { get; set; } = -1;

        void CreateCallbacks()
        {
            m_KeyDownCallback = new EventCallback<KeyDownEvent>(evt =>
            {
                // Record Undo for input field edit
                if (mUndoGroup == -1)
                {
                    mUndoGroup = Undo.GetCurrentGroup();
                    preValueChangeCallback?.Invoke();
                }
                // Handle escaping input field edit
                if (evt.keyCode == KeyCode.Escape && mUndoGroup > -1)
                {
                    Undo.RevertAllDownToGroup(mUndoGroup);
                    mUndoGroup = -1;
                    evt.StopPropagation();
                }
                // Dont record Undo again until input field is unfocused
                mUndoGroup++;
                postValueChangeCallback?.Invoke();
            });

            m_FocusOutCallback = new EventCallback<FocusOutEvent>(evt =>
            {
                // Reset UndoGroup when done editing input field
                mUndoGroup = -1;
            });
        }

        public Vector4PropertyDrawer()
        {
            CreateCallbacks();
        }

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Vector4 fieldToDraw,
            string labelName,
            out VisualElement propertyVec4Field,
            int indentLevel = 0)
        {
            var vector4Field = new Vector4Field { value = fieldToDraw };

            var inputFields = vector4Field.Query("unity-text-input").ToList();
            foreach (var inputField in inputFields)
            {
                inputField.RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
                inputField.RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            }

            vector4Field.RegisterValueChangedCallback(evt =>
            {
                // Only true when setting value via FieldMouseDragger
                // Undo recorded once per dragger release
                if (mUndoGroup == -1)
                    preValueChangeCallback?.Invoke();

                valueChangedCallback(evt.newValue);
                postValueChangeCallback?.Invoke();
            });

            propertyVec4Field = vector4Field;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyVec4Field);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] { newValue }),
                (Vector4)propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
