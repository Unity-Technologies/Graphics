using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using FloatField = UnityEditor.ShaderGraph.Drawing.FloatField;

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
    public interface IPropertyDrawer
    {
        Action inspectorUpdateDelegate { get; set; }

        VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute);
    }

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
                var toggle = (Toggle) propertyToggle;
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
                newBoolValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newBoolValue}),
                (ToggleData) propertyInfo.GetValue(actualObject),
                 attribute.labelName,
                 out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(bool))]
    class BoolPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(bool newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            bool fieldToDraw,
            string labelName,
            out VisualElement propertyToggle,
            int indentLevel = 0)
        {
            var row = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            // Create and assign toggle as out variable here so that callers can also do additional work with enabling/disabling if needed
            propertyToggle = new Toggle();
            row.Add((Toggle)propertyToggle, (toggle) =>
            {
                toggle.value = fieldToDraw;
            });

            if (valueChangedCallback != null)
            {
                var toggle = (Toggle) propertyToggle;
                toggle.OnToggleChanged(evt => valueChangedCallback(evt.newValue));
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
                newBoolValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newBoolValue}),
                (bool) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

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

    [SGPropertyDrawer(typeof(IEnumerable<string>))]
    class TextArrayPropertyDrawer : IPropertyDrawer
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
            var popupField = (PopupField<string>) textArrayField;
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
                (IEnumerable<string>) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var textArrayField);
        }
    }

    [SGPropertyDrawer(typeof(int))]
    class IntegerPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(int newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            int fieldToDraw,
            string labelName,
            out VisualElement propertyFloatField,
            int indentLevel = 0)
        {
            var integerField = new IntegerField {value = fieldToDraw};

            if (valueChangedCallback != null)
            {
                integerField.RegisterValueChangedCallback(evt => { valueChangedCallback(evt.newValue); });
            }

            propertyFloatField = integerField;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyFloatField);

            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (int) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(float))]
    class FloatPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(float newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            float fieldToDraw,
            string labelName,
            out VisualElement propertyFloatField,
            int indentLevel = 0)
        {
            var floatField = new FloatField {value = fieldToDraw};

            if (valueChangedCallback != null)
            {
                floatField.RegisterValueChangedCallback(evt => { valueChangedCallback((float) evt.newValue); });
            }

            propertyFloatField = floatField;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyFloatField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (float) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(Vector2))]
    class Vector2PropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Vector2 newValue);

        public Action preValueChangeCallback { get; set; }
        public Action postValueChangeCallback { get; set; }

        EventCallback<KeyDownEvent> m_KeyDownCallback;
        EventCallback<FocusOutEvent> m_FocusOutCallback;
        public int mUndoGroup { get; set; } = -1;

        public Vector2PropertyDrawer()
        {
            CreateCallbacks();
        }

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

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Vector2 fieldToDraw,
            string labelName,
            out VisualElement propertyVec2Field,
            int indentLevel = 0)
        {
            var vector2Field = new Vector2Field {value = fieldToDraw};

            vector2Field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector2Field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            vector2Field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector2Field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);

            // Bind value changed event to callback to handle dragger behavior before actually settings the value
            vector2Field.RegisterValueChangedCallback(evt =>
            {
                // Only true when setting value via FieldMouseDragger
                // Undo recorded once per dragger release
                if (mUndoGroup == -1)
                    preValueChangeCallback?.Invoke();

                valueChangedCallback(evt.newValue);
                postValueChangeCallback?.Invoke();
            });

            propertyVec2Field = vector2Field;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyVec2Field);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Vector2) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(Vector3))]
    class Vector3PropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Vector3 newValue);

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

        public Vector3PropertyDrawer()
        {
            CreateCallbacks();
        }

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Vector3 fieldToDraw,
            string labelName,
            out VisualElement propertyVec3Field,
            int indentLevel = 0)
        {
            var vector3Field = new Vector3Field {value = fieldToDraw};

            vector3Field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector3Field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            vector3Field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector3Field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            vector3Field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector3Field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);

            vector3Field.RegisterValueChangedCallback(evt =>
            {
                // Only true when setting value via FieldMouseDragger
                // Undo recorded once per dragger release
                if (mUndoGroup == -1)
                    preValueChangeCallback?.Invoke();

                valueChangedCallback(evt.newValue);
                postValueChangeCallback?.Invoke();
            });

            propertyVec3Field = vector3Field;

            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyVec3Field);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Vector3) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

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
            var vector4Field = new Vector4Field {value = fieldToDraw};

            vector4Field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector4Field.Q("unity-x-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            vector4Field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector4Field.Q("unity-y-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            vector4Field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector4Field.Q("unity-z-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);
            vector4Field.Q("unity-w-input").Q("unity-text-input").RegisterCallback<KeyDownEvent>(m_KeyDownCallback);
            vector4Field.Q("unity-w-input").Q("unity-text-input").RegisterCallback<FocusOutEvent>(m_FocusOutCallback);

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
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Vector4) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

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
                colorField.RegisterValueChangedCallback(evt => { valueChangedCallback((Color) evt.newValue); });
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
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Color) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(Texture))]
    class Texture2DPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Texture newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Texture fieldToDraw,
            string labelName,
            out VisualElement propertyColorField,
            int indentLevel = 0)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Texture)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Texture) evt.newValue); });
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
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Texture) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(Texture2DArray))]
    class Texture2DArrayPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Texture2DArray newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Texture2DArray fieldToDraw,
            string labelName,
            out VisualElement propertyColorField,
            int indentLevel = 0)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Texture2DArray)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Texture2DArray) evt.newValue); });
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
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Texture2DArray) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(Texture3D))]
    class Texture3DPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Texture3D newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Texture fieldToDraw,
            string labelName,
            out VisualElement propertyColorField,
            int indentLevel = 0)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Texture3D)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Texture3D) evt.newValue); });
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
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Texture3D) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

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
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Cubemap)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Cubemap) evt.newValue); });
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
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Cubemap) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(Matrix4x4))]
    class MatrixPropertyDrawer : IPropertyDrawer
    {
        public enum MatrixDimensions
        {
            Two,
            Three,
            Four
        }
        public MatrixDimensions dimension { get; set; }

        public delegate Vector4 GetMatrixRowDelegate(int rowNumber);

        internal Action PreValueChangeCallback;
        internal delegate void ValueChangedCallback(Matrix4x4 newValue);
        internal Action PostValueChangeCallback;
        // Matrix4x4, Matrix3x3, Matrix2x2 are all value types,
        // hence the local value doesn't stay up to date after modified
        // Need a callback to fetch the row data directly from the source
        internal GetMatrixRowDelegate MatrixRowFetchCallback;

        private void HandleMatrix2Property(
            ValueChangedCallback valueChangedCallback,
            PropertySheet propertySheet,
            Matrix4x4 matrix2Property,
            string labelName = "Default")
        {
            var vector2PropertyDrawer = new Vector2PropertyDrawer();
            vector2PropertyDrawer.preValueChangeCallback = PreValueChangeCallback;
            vector2PropertyDrawer.postValueChangeCallback = PostValueChangeCallback;

            propertySheet.Add(vector2PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector2 row1 = MatrixRowFetchCallback(1);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = newValue.x,
                        m01 = newValue.y,
                        m02 = 0,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = 0,
                        m13 = 0,
                        m20 = 0,
                        m21 = 0,
                        m22 = 0,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix2Property.GetRow(0),
                labelName,
                out var row0Field
                ));

            propertySheet.Add(vector2PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector2 row0 = MatrixRowFetchCallback(0);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = 0,
                        m03 = 0,
                        m10 = newValue.x,
                        m11 = newValue.y,
                        m12 = 0,
                        m13 = 0,
                        m20 = 0,
                        m21 = 0,
                        m22 = 0,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix2Property.GetRow(1),
                "",
                out var row1Field
            ));
        }

        private void HandleMatrix3Property(
            ValueChangedCallback valueChangedCallback,
            PropertySheet propertySheet,
            Matrix4x4 matrix3Property,
            string labelName = "Default")
        {
            var vector3PropertyDrawer = new Vector3PropertyDrawer();
            vector3PropertyDrawer.preValueChangeCallback = PreValueChangeCallback;
            vector3PropertyDrawer.postValueChangeCallback = PostValueChangeCallback;

            propertySheet.Add(vector3PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector3 row1 = MatrixRowFetchCallback(1);
                    Vector3 row2 = MatrixRowFetchCallback(2);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = newValue.x,
                        m01 = newValue.y,
                        m02 = newValue.z,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = 0,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix3Property.GetRow(0),
                labelName,
                out var row0Field
                ));

            propertySheet.Add(vector3PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector3 row0 = MatrixRowFetchCallback(0);
                    Vector3 row2 = MatrixRowFetchCallback(2);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = 0,
                        m10 = newValue.x,
                        m11 = newValue.y,
                        m12 = newValue.z,
                        m13 = 0,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix3Property.GetRow(1),
                "",
                out var row1Field
            ));

            propertySheet.Add(vector3PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector3 row0 = MatrixRowFetchCallback(0);
                    Vector3 row1 = MatrixRowFetchCallback(1);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = 0,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = 0,
                        m20 = newValue.x,
                        m21 = newValue.y,
                        m22 = newValue.z,
                        m23 = 0,
                        m30 = 0,
                        m31 = 0,
                        m32 = 0,
                        m33 = 0,
                    });
                },
                matrix3Property.GetRow(2),
                "",
                out var row2Field
            ));
        }

        private void HandleMatrix4Property(
            ValueChangedCallback valueChangedCallback,
            PropertySheet propertySheet,
            Matrix4x4 matrix4Property,
            string labelName = "Default")
        {
            var vector4PropertyDrawer = new Vector4PropertyDrawer();
            vector4PropertyDrawer.preValueChangeCallback = PreValueChangeCallback;
            vector4PropertyDrawer.postValueChangeCallback = PostValueChangeCallback;

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector4 row1 = MatrixRowFetchCallback(1);
                    Vector4 row2 = MatrixRowFetchCallback(2);
                    Vector4 row3 = MatrixRowFetchCallback(3);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = newValue.x,
                        m01 = newValue.y,
                        m02 = newValue.z,
                        m03 = newValue.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    });
                },
                matrix4Property.GetRow(0),
                labelName,
                out var row0Field
                ));

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector4 row0 = MatrixRowFetchCallback(0);
                    Vector4 row2 = MatrixRowFetchCallback(2);
                    Vector4 row3 = MatrixRowFetchCallback(3);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = newValue.x,
                        m11 = newValue.y,
                        m12 = newValue.z,
                        m13 = newValue.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    });
                },
                matrix4Property.GetRow(1),
                "",
                out var row1Field
            ));

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector4 row0 = MatrixRowFetchCallback(0);
                    Vector4 row1 = MatrixRowFetchCallback(1);
                    Vector4 row3 = MatrixRowFetchCallback(3);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = newValue.x,
                        m21 = newValue.y,
                        m22 = newValue.z,
                        m23 = newValue.w,
                        m30 = row3.x,
                        m31 = row3.y,
                        m32 = row3.z,
                        m33 = row3.w,
                    });
                },
                matrix4Property.GetRow(2),
                "",
                out var row2Field));

            propertySheet.Add(vector4PropertyDrawer.CreateGUI(
                newValue =>
                {
                    Vector4 row0 = MatrixRowFetchCallback(0);
                    Vector4 row1 = MatrixRowFetchCallback(1);
                    Vector4 row2 = MatrixRowFetchCallback(2);
                    valueChangedCallback(new Matrix4x4()
                    {
                        m00 = row0.x,
                        m01 = row0.y,
                        m02 = row0.z,
                        m03 = row0.w,
                        m10 = row1.x,
                        m11 = row1.y,
                        m12 = row1.z,
                        m13 = row1.w,
                        m20 = row2.x,
                        m21 = row2.y,
                        m22 = row2.z,
                        m23 = row2.w,
                        m30 = newValue.x,
                        m31 = newValue.y,
                        m32 = newValue.z,
                        m33 = newValue.w,
                    });
                },
                matrix4Property.GetRow(3),
                "",
                out var row3Field
            ));
        }

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Matrix4x4 fieldToDraw,
            string labelName,
            out VisualElement propertyMatrixField,
            int indentLevel = 0)
        {
            var propertySheet = new PropertySheet();

            switch (dimension)
            {
                case MatrixDimensions.Two:
                    HandleMatrix2Property(valueChangedCallback, propertySheet, fieldToDraw, labelName);
                    break;
                case MatrixDimensions.Three:
                    HandleMatrix3Property(valueChangedCallback, propertySheet, fieldToDraw, labelName);
                    break;
                case MatrixDimensions.Four:
                    HandleMatrix4Property(valueChangedCallback, propertySheet, fieldToDraw, labelName);
                    break;
            }

            propertyMatrixField = propertySheet;
            return propertyMatrixField;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Matrix4x4) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(Gradient))]
    class GradientPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Gradient newValue);

        internal VisualElement CreateGUI(
            ValueChangedCallback valueChangedCallback,
            Gradient fieldToDraw,
            string labelName,
            out VisualElement propertyGradientField,
            int indentLevel = 0)
        {
            var objectField = new GradientField { value = fieldToDraw};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Gradient) evt.newValue); });
            }

            propertyGradientField = objectField;

            // #TODO: Inspector - Repeated code for instantiating and styling the property row, should probably extract into a static helper class
            // Any core widgets used by the inspector over and over should come from some kind of factory
            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(labelName, indentLevel));
            defaultRow.Add(propertyGradientField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Gradient) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }
}
