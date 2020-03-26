using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using FloatField = UnityEditor.ShaderGraph.Drawing.FloatField;
using ShaderKeyword = UnityEditor.ShaderGraph.ShaderKeyword;

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
        VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute);
    }

    [SGPropertyDrawer(typeof(Enum))]
    class EnumPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(Enum newValue);

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Enum fieldToDraw,
            string labelName,
            Enum defaultValue,
            out VisualElement propertyVisualElement)
        {
            var row = new PropertyRow(new Label(labelName));
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

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            Inspectable attribute)
        {
            return this.CreateGUIForField(newEnumValue =>
                    propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newEnumValue}),
                (Enum) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                (Enum) attribute.defaultValue,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(ToggleData))]
    class BoolPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(ToggleData newValue);

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            ToggleData fieldToDraw,
            string labelName,
            out VisualElement propertyToggle)
        {
            var row = new PropertyRow(new Label(labelName));
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

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            Inspectable attribute)
        {
            return this.CreateGUIForField(
                // Use the setter from the provided property as the callback
                newBoolValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newBoolValue}),
                (ToggleData) propertyInfo.GetValue(actualObject),
                 attribute.labelName,
                 out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(string))]
    class TextPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(string newValue);

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            string fieldToDraw,
            string labelName,
            out VisualElement propertyTextField)
        {
            var propertyRow = new PropertyRow(new Label(labelName));
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

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
                // Use the setter from the provided property as the callback
                newStringValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newStringValue}),
                (string) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(int))]
    class IntegerPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ValueChangedCallback(int newValue);

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            int fieldToDraw,
            string labelName,
            out VisualElement propertyFloatField)
        {
            var integerField = new IntegerField {value = fieldToDraw};

            if (valueChangedCallback != null)
            {
                integerField.RegisterValueChangedCallback(evt => { valueChangedCallback(evt.newValue); });
            }

            propertyFloatField = integerField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyFloatField);

            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            float fieldToDraw,
            string labelName,
            out VisualElement propertyFloatField)
        {
            var floatField = new FloatField {value = fieldToDraw};

            if (valueChangedCallback != null)
            {
                floatField.RegisterValueChangedCallback(evt => { valueChangedCallback((float) evt.newValue); });
            }

            propertyFloatField = floatField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyFloatField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Vector2 fieldToDraw,
            string labelName,
            out VisualElement propertyVec2Field)
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

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyVec2Field);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Vector3 fieldToDraw,
            string labelName,
            out VisualElement propertyVec3Field)
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

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyVec3Field);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Vector4 fieldToDraw,
            string labelName,
            out VisualElement propertyVec4Field)
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

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyVec4Field);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Color fieldToDraw,
            string labelName,
            out VisualElement propertyColorField)
        {
            var colorField = new ColorField { value = fieldToDraw, showEyeDropper = false, hdr = false };

            if (valueChangedCallback != null)
            {
                colorField.RegisterValueChangedCallback(evt => { valueChangedCallback((Color) evt.newValue); });
            }

            propertyColorField = colorField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyColorField);
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Texture fieldToDraw,
            string labelName,
            out VisualElement propertyColorField)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Texture)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Texture) evt.newValue); });
            }

            propertyColorField = objectField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyColorField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Texture2DArray fieldToDraw,
            string labelName,
            out VisualElement propertyColorField)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Texture2DArray)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Texture2DArray) evt.newValue); });
            }

            propertyColorField = objectField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyColorField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Texture fieldToDraw,
            string labelName,
            out VisualElement propertyColorField)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Texture3D)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Texture3D) evt.newValue); });
            }

            propertyColorField = objectField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyColorField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Cubemap fieldToDraw,
            string labelName,
            out VisualElement propertyCubemapField)
        {
            var objectField = new ObjectField { value = fieldToDraw, objectType = typeof(Cubemap)};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Cubemap) evt.newValue); });
            }

            propertyCubemapField = objectField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyCubemapField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal Action PreValueChangeCallback;
        internal delegate void ValueChangedCallback(Matrix4x4 newValue);
        internal Action PostValueChangeCallback;

        private void HandleMatrix2Property(
            ValueChangedCallback valueChangedCallback,
            PropertySheet propertySheet,
            Matrix4x4 matrix2Property,
            string labelName = "Default")
        {
            var vector2PropertyDrawer = new Vector2PropertyDrawer();
            vector2PropertyDrawer.preValueChangeCallback = PreValueChangeCallback;
            vector2PropertyDrawer.postValueChangeCallback = PostValueChangeCallback;

            propertySheet.Add(vector2PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector2 row1 = matrix2Property.GetRow(1);
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

            propertySheet.Add(vector2PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector2 row0 = matrix2Property.GetRow(0);
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

            propertySheet.Add(vector3PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector3 row1 = matrix3Property.GetRow(1);
                    Vector3 row2 = matrix3Property.GetRow(2);
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

            propertySheet.Add(vector3PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector3 row0 = matrix3Property.GetRow(0);
                    Vector3 row2 = matrix3Property.GetRow(2);
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

            propertySheet.Add(vector3PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector3 row0 = matrix3Property.GetRow(0);
                    Vector3 row1 = matrix3Property.GetRow(1);
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

            propertySheet.Add(vector4PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector4 row1 = matrix4Property.GetRow(1);
                    Vector4 row2 = matrix4Property.GetRow(2);
                    Vector4 row3 = matrix4Property.GetRow(3);
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

            propertySheet.Add(vector4PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector4 row0 = matrix4Property.GetRow(0);
                    Vector4 row2 = matrix4Property.GetRow(2);
                    Vector4 row3 = matrix4Property.GetRow(3);
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

            propertySheet.Add(vector4PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector4 row0 = matrix4Property.GetRow(0);
                    Vector4 row1 = matrix4Property.GetRow(1);
                    Vector4 row3 = matrix4Property.GetRow(3);
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

            propertySheet.Add(vector4PropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    Vector4 row0 = matrix4Property.GetRow(0);
                    Vector4 row1 = matrix4Property.GetRow(1);
                    Vector4 row2 = matrix4Property.GetRow(2);
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Matrix4x4 fieldToDraw,
            string labelName,
            out VisualElement propertyMatrixField)
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

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
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

        internal VisualElement CreateGUIForField(
            ValueChangedCallback valueChangedCallback,
            Gradient fieldToDraw,
            string labelName,
            out VisualElement propertyGradientField)
        {
            var objectField = new GradientField { value = fieldToDraw};

            if (valueChangedCallback != null)
            {
                objectField.RegisterValueChangedCallback(evt => { valueChangedCallback((Gradient) evt.newValue); });
            }

            propertyGradientField = objectField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyGradientField);
            defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            return defaultRow;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField(
                // Use the setter from the provided property as the callback
                newValue => propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {newValue}),
                (Gradient) propertyInfo.GetValue(actualObject),
                attribute.labelName,
                out var propertyVisualElement);
        }
    }

    [SGPropertyDrawer(typeof(ShaderInput))]
    class ShaderInputPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ChangeExposedFieldCallback(bool newValue);
        internal delegate void ChangeReferenceNameCallback(string newValue);
        internal delegate void ChangeValueCallback(object newValue);
        internal delegate void PreChangeValueCallback(string actionName);
        internal delegate void PostChangeValueCallback(bool bTriggerPropertyUpdate = false, ModificationScope modificationScope = ModificationScope.Node);

        // Keyword
        private ReorderableList m_ReorderableList;
        private int m_SelectedIndex;

        private ShaderInput shaderInput;
        private bool isSubGraph { get ; set;  }
        private ChangeExposedFieldCallback _exposedFieldChangedCallback;
        private ChangeReferenceNameCallback _referenceNameChangedCallback;
        private Action _keywordChangedCallback;
        private ChangeValueCallback _changeValueCallback;
        private PreChangeValueCallback _preChangeValueCallback;
        private PostChangeValueCallback _postChangeValueCallback;

        public void GetPropertyData(bool isSubGraph,
            ChangeExposedFieldCallback exposedFieldCallback,
            ChangeReferenceNameCallback referenceNameCallback,
            Action keywordChangedCallback,
            ChangeValueCallback changeValueCallback,
            PreChangeValueCallback preChangeValueCallback,
            PostChangeValueCallback postChangeValueCallback)
        {
            this.isSubGraph = isSubGraph;
            this._exposedFieldChangedCallback = exposedFieldCallback;
            this._referenceNameChangedCallback = referenceNameCallback;
            this._changeValueCallback = changeValueCallback;
            this._keywordChangedCallback = keywordChangedCallback;
            this._preChangeValueCallback = preChangeValueCallback;
            this._postChangeValueCallback = postChangeValueCallback;
        }

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            Inspectable attribute)
        {
            var propertySheet = new PropertySheet();
            // #TODO Handle child classes, needs extra work?
            shaderInput = actualObject as ShaderInput;
            BuildExposedField(propertySheet);
            BuildReferenceNameField(propertySheet);
            BuildPropertyFields(propertySheet);
            BuildKeywordFields(propertySheet, shaderInput);
            return propertySheet;
        }

        void BuildExposedField(PropertySheet propertySheet)
        {
            if(!isSubGraph)
            {
                var boolPropertyDrawer = new BoolPropertyDrawer();
                propertySheet.Add(boolPropertyDrawer.CreateGUIForField(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Exposed Toggle");
                        this._exposedFieldChangedCallback(evt.isOn);
                        this._postChangeValueCallback(false, ModificationScope.Graph);
                    },
                    new ToggleData(shaderInput.generatePropertyBlock),
                    "Exposed",
                    out var propertyToggle));
                propertyToggle.SetEnabled(shaderInput.isExposable);
            }
        }

        void BuildReferenceNameField(PropertySheet propertySheet)
        {
            if (!isSubGraph || shaderInput is ShaderKeyword)
            {
                var textPropertyDrawer = new TextPropertyDrawer();
                propertySheet.Add(textPropertyDrawer.CreateGUIForField(
                    null,
                    (string)shaderInput.referenceName,
                    "Reference",
                    out var propertyVisualElement));

                var propertyTextField = (TextField) propertyVisualElement;
                propertyTextField.RegisterValueChangedCallback(
                    evt =>
                    {
                        this._preChangeValueCallback("Change Reference Name");
                        this._referenceNameChangedCallback(evt.newValue);

                        if (string.IsNullOrEmpty(shaderInput.overrideReferenceName))
                            propertyTextField.RemoveFromClassList("modified");
                        else
                            propertyTextField.AddToClassList("modified");

                        this._postChangeValueCallback(false, ModificationScope.Graph);
                    });

                if(!string.IsNullOrEmpty(shaderInput.overrideReferenceName))
                    propertyVisualElement.AddToClassList("modified");
                propertyVisualElement.SetEnabled(shaderInput.isRenamable);
                propertyVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
            }
        }

        void BuildPropertyFields(PropertySheet propertySheet)
        {
            var property = shaderInput as AbstractShaderProperty;
            if(property == null)
                return;

            switch(property)
            {
            case Vector1ShaderProperty vector1Property:
                HandleVector1ShaderProperty(propertySheet, vector1Property);
                break;
            case Vector2ShaderProperty vector2Property:
                HandleVector2ShaderProperty(propertySheet, vector2Property);
                break;
            case Vector3ShaderProperty vector3Property:
                HandleVector3ShaderProperty(propertySheet, vector3Property);
                break;
            case Vector4ShaderProperty vector4Property:
                HandleVector4ShaderProperty(propertySheet, vector4Property);
                break;
            case ColorShaderProperty colorProperty:
                HandleColorProperty(propertySheet, colorProperty);
                break;
            case Texture2DShaderProperty texture2DProperty:
                HandleTexture2DProperty(propertySheet, texture2DProperty);
                break;
            case Texture2DArrayShaderProperty texture2DArrayProperty:
                HandleTexture2DArrayProperty(propertySheet, texture2DArrayProperty);
                break;
            case Texture3DShaderProperty texture3DProperty:
                HandleTexture3DProperty(propertySheet, texture3DProperty);
                break;
            case CubemapShaderProperty cubemapProperty:
                HandleCubemapProperty(propertySheet, cubemapProperty);
                break;
            case BooleanShaderProperty booleanProperty:
                HandleBooleanProperty(propertySheet, booleanProperty);
                break;
            case Matrix2ShaderProperty matrix2Property:
                HandleMatrix2PropertyField(propertySheet, matrix2Property);
                break;
            case Matrix3ShaderProperty matrix3Property:
                HandleMatrix3PropertyField(propertySheet, matrix3Property);
                break;
            case Matrix4ShaderProperty matrix4Property:
                HandleMatrix4PropertyField(propertySheet, matrix4Property);
                break;
            case SamplerStateShaderProperty samplerStateProperty:
                HandleSamplerStatePropertyField(propertySheet, samplerStateProperty);
                break;
            case GradientShaderProperty gradientProperty:
                HandleGradientPropertyField(propertySheet, gradientProperty);
                break;
            }

            BuildPrecisionField(propertySheet, property);
            BuildGpuInstancingField(propertySheet, property);
        }

        // #TODO: Current Blackboard calls ValidateGraph() after changing this property, is this actually needed?
        private void BuildPrecisionField(PropertySheet propertySheet, AbstractShaderProperty property)
        {
            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUIForField(newValue =>
                {
                    this._preChangeValueCallback("Change Precision");
                    if (property.precision == (Precision) newValue)
                        return;
                    property.precision = (Precision)newValue;
                    this._postChangeValueCallback();
                }, property.precision, "Precision", Precision.Inherit, out var precisionField));
        }

        private void BuildGpuInstancingField(PropertySheet propertySheet, AbstractShaderProperty property)
        {
            var boolPropertyDrawer = new BoolPropertyDrawer();
            propertySheet.Add(boolPropertyDrawer.CreateGUIForField( newValue =>
            {
                this._preChangeValueCallback("Change Hybrid Instanced Toggle");
                property.gpuInstanced = newValue.isOn;
                this._postChangeValueCallback(false, ModificationScope.Graph);
            }, new ToggleData(property.gpuInstanced), "Hybrid Instanced (experimental)", out var gpuInstancedToggle));

            gpuInstancedToggle.SetEnabled(property.isGpuInstanceable);
        }

        private void HandleVector1ShaderProperty(PropertySheet propertySheet, Vector1ShaderProperty vector1ShaderProperty)
        {
            // Handle vector 1 mode parameters
            switch (vector1ShaderProperty.floatType)
            {
                case FloatType.Slider:
                    var sliderFloatPropertyDrawer = new FloatPropertyDrawer();
                    // Default field
                    propertySheet.Add(sliderFloatPropertyDrawer.CreateGUIForField(
                        newValue => _changeValueCallback(newValue),
                        vector1ShaderProperty.value,
                        "Default",
                        out var propertyFloatField));
                    propertyFloatField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                        {
                            _preChangeValueCallback("Change Property Value");
                            float minValue = Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.x);
                            float maxValue = Mathf.Max(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y);
                            vector1ShaderProperty.rangeValues = new Vector2(minValue, maxValue);
                            _postChangeValueCallback();
                        });

                    // Min field
                    var minFieldChangedCallback = new ChangeValueCallback(newValue =>
                        {
                            _preChangeValueCallback("Change Range Property Minimum");
                            vector1ShaderProperty.rangeValues = new Vector2((float)newValue, vector1ShaderProperty.rangeValues.x);
                            _postChangeValueCallback();
                        });
                    propertySheet.Add(sliderFloatPropertyDrawer.CreateGUIForField(
                        newValue => minFieldChangedCallback(newValue),
                        vector1ShaderProperty.rangeValues.x,
                        "Min",
                        out var minFloatField));
                    minFloatField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                    {
                        vector1ShaderProperty.value = Mathf.Max(Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y), vector1ShaderProperty.rangeValues.x);
                        _postChangeValueCallback();
                    });

                    // Max field
                    var maxFieldChangedCallback = new ChangeValueCallback(newValue =>
                    {
                        this._preChangeValueCallback("Change Range Property Maximum");
                        vector1ShaderProperty.rangeValues = new Vector2(vector1ShaderProperty.rangeValues.x, (float)newValue);
                        this._postChangeValueCallback();
                    });
                    propertySheet.Add(sliderFloatPropertyDrawer.CreateGUIForField(
                        newValue => maxFieldChangedCallback(newValue),
                        vector1ShaderProperty.rangeValues.y,
                        "Max",
                        out var maxFloatField));
                    maxFloatField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
                    {
                        vector1ShaderProperty.value = Mathf.Max(Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y), vector1ShaderProperty.rangeValues.x);
                        this._postChangeValueCallback();
                    });
                    break;

                case FloatType.Integer:
                    var integerPropertyDrawer = new IntegerPropertyDrawer();
                    // Default field
                    propertySheet.Add(integerPropertyDrawer.CreateGUIForField(
                        newValue => this._changeValueCallback(newValue),
                        (int)vector1ShaderProperty.value,
                        "Default",
                        out var integerPropertyField));
                    break;

                default:
                    var defaultFloatPropertyDrawer = new FloatPropertyDrawer();
                    // Default field
                    propertySheet.Add(defaultFloatPropertyDrawer.CreateGUIForField(
                        newValue =>
                        {
                            this._preChangeValueCallback("Change property value");
                            this._changeValueCallback(newValue);
                            this._postChangeValueCallback();
                        },
                        vector1ShaderProperty.value,
                        "Default",
                        out var defaultFloatPropertyField));
                    break;
            }

            if (!isSubGraph)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUIForField(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Vector1 Mode");
                        vector1ShaderProperty.floatType = (FloatType)newValue;
                        this._postChangeValueCallback(true);
                    },
                    vector1ShaderProperty.floatType,
                    "Mode",
                     FloatType.Default,
                    out var modePropertyEnumField));
            }
        }

        private void HandleVector2ShaderProperty(PropertySheet propertySheet, Vector2ShaderProperty vector2ShaderProperty)
        {
            var vector2PropertyDrawer = new Vector2PropertyDrawer();
            vector2PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector2PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector2PropertyDrawer.CreateGUIForField(
                newValue=> _changeValueCallback(newValue),
                vector2ShaderProperty.value,
                "Default",
                out var propertyVec2Field));
        }

        private void HandleVector3ShaderProperty(PropertySheet propertySheet, Vector3ShaderProperty vector3ShaderProperty)
        {
            var vector3PropertyDrawer = new Vector3PropertyDrawer();
            vector3PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector3PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector3PropertyDrawer.CreateGUIForField(
                newValue => _changeValueCallback(newValue),
                vector3ShaderProperty.value,
                "Default",
                out var propertyVec3Field));
        }

        private void HandleVector4ShaderProperty(PropertySheet propertySheet, Vector4ShaderProperty vector4Property)
        {
            var vector4PropertyDrawer = new Vector4PropertyDrawer();
            vector4PropertyDrawer.preValueChangeCallback = () => this._preChangeValueCallback("Change property value");
            vector4PropertyDrawer.postValueChangeCallback = () => this._postChangeValueCallback();

            propertySheet.Add(vector4PropertyDrawer.CreateGUIForField(
                newValue => _changeValueCallback(newValue),
                vector4Property.value,
                "Default",
                out var propertyVec4Field));
        }

        private void HandleColorProperty(PropertySheet propertySheet, ColorShaderProperty colorProperty)
        {
            var colorPropertyDrawer = new ColorPropertyDrawer();

            propertySheet.Add(colorPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                colorProperty.value,
                "Default",
                out var propertyColorField));

            var colorField = (ColorField) propertyColorField;
            colorField.hdr = colorProperty.colorMode == ColorMode.HDR;

            if (!isSubGraph)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();

                propertySheet.Add(enumPropertyDrawer.CreateGUIForField(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Color Mode");
                        colorProperty.colorMode = (ColorMode)newValue;
                        this._postChangeValueCallback(true);
                    },
                    colorProperty.colorMode,
                    "Mode",
                    ColorMode.Default,
                    out var colorModeField));
            }
        }

        private void HandleTexture2DProperty(PropertySheet propertySheet, Texture2DShaderProperty texture2DProperty)
        {
            var texture2DPropertyDrawer = new Texture2DPropertyDrawer();
            propertySheet.Add(texture2DPropertyDrawer.CreateGUIForField(
                newValue =>
            {
                this._preChangeValueCallback("Change property value");
                this._changeValueCallback(newValue);
                this._postChangeValueCallback();
            },
                texture2DProperty.value.texture,
                "Default",
                out var texture2DField
            ));

            if (!isSubGraph)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUIForField(
                    newValue =>
                {
                    this._preChangeValueCallback("Change Texture mode");
                    if(texture2DProperty.defaultType == (Texture2DShaderProperty.DefaultType)newValue)
                        return;
                    texture2DProperty.defaultType = (Texture2DShaderProperty.DefaultType) newValue;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                },
                    texture2DProperty.defaultType,
                    "Mode",
                    Texture2DShaderProperty.DefaultType.White,
                    out var textureModeField));

                textureModeField.SetEnabled(texture2DProperty.generatePropertyBlock);
            }
        }

        private void HandleTexture2DArrayProperty(PropertySheet propertySheet, Texture2DArrayShaderProperty texture2DArrayProperty)
        {
            var texture2DArrayPropertyDrawer = new Texture2DArrayPropertyDrawer();
            propertySheet.Add(texture2DArrayPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                texture2DArrayProperty.value.textureArray,
                "Default",
                out var texture2DArrayField
            ));
        }

        private void HandleTexture3DProperty(PropertySheet propertySheet, Texture3DShaderProperty texture3DShaderProperty)
        {
            var texture3DPropertyDrawer = new Texture3DPropertyDrawer();
            propertySheet.Add(texture3DPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                texture3DShaderProperty.value.texture,
                "Default",
                out var texture3DField
            ));
        }

        private void HandleCubemapProperty(PropertySheet propertySheet, CubemapShaderProperty cubemapProperty)
        {
            var cubemapPropertyDrawer = new CubemapPropertyDrawer();
            propertySheet.Add(cubemapPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                cubemapProperty.value.cubemap,
                "Default",
                out var propertyCubemapField
                ));
        }

        private void HandleBooleanProperty(PropertySheet propertySheet, BooleanShaderProperty booleanProperty)
        {
            var booleanPropertyDrawer = new BoolPropertyDrawer();
            propertySheet.Add(booleanPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                new ToggleData(booleanProperty.value),
                "Default",
                out var propertyToggle));
        }

        private void HandleMatrix2PropertyField(PropertySheet propertySheet, Matrix2ShaderProperty matrix2Property)
        {
            var matrixPropertyDrawer = new MatrixPropertyDrawer
            {
                dimension = MatrixPropertyDrawer.MatrixDimensions.Two,
                PreValueChangeCallback = () => this._preChangeValueCallback("Change property value"),
                PostValueChangeCallback = () => this._postChangeValueCallback()
            };

            propertySheet.Add(matrixPropertyDrawer.CreateGUIForField(
                newValue => { this._changeValueCallback(newValue); },
                matrix2Property.value,
                "Default",
                out var propertyMatrixField));
        }

        private void HandleMatrix3PropertyField(PropertySheet propertySheet, Matrix3ShaderProperty matrix3Property)
        {
            var matrixPropertyDrawer = new MatrixPropertyDrawer
            {
                dimension = MatrixPropertyDrawer.MatrixDimensions.Three,
                PreValueChangeCallback = () => this._preChangeValueCallback("Change property value"),
                PostValueChangeCallback = () => this._postChangeValueCallback()
            };

            propertySheet.Add(matrixPropertyDrawer.CreateGUIForField(
                newValue => { this._changeValueCallback(newValue); },
                matrix3Property.value,
                "Default",
                out var propertyMatrixField));
        }

        private void HandleMatrix4PropertyField(PropertySheet propertySheet, Matrix4ShaderProperty matrix4Property)
        {
            var matrixPropertyDrawer = new MatrixPropertyDrawer
            {
                dimension = MatrixPropertyDrawer.MatrixDimensions.Four,
                PreValueChangeCallback = () => this._preChangeValueCallback("Change property value"),
                PostValueChangeCallback = () => this._postChangeValueCallback()
            };

            propertySheet.Add(matrixPropertyDrawer.CreateGUIForField(
                newValue => { this._changeValueCallback(newValue); },
                matrix4Property.value,
                "Default",
                out var propertyMatrixField));
        }

        private void HandleSamplerStatePropertyField(PropertySheet propertySheet, SamplerStateShaderProperty samplerStateShaderProperty)
        {
            var enumPropertyDrawer = new EnumPropertyDrawer();

            propertySheet.Add(enumPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    TextureSamplerState state = samplerStateShaderProperty.value;
                    state.filter = (TextureSamplerState.FilterMode) newValue;
                    samplerStateShaderProperty.value = state;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                },
                samplerStateShaderProperty.value.filter,
                "Filter",
                TextureSamplerState.FilterMode.Linear,
                out var filterVisualElement));

            propertySheet.Add(enumPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    TextureSamplerState state = samplerStateShaderProperty.value;
                    state.wrap = (TextureSamplerState.WrapMode) newValue;
                    samplerStateShaderProperty.value = state;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                },
                samplerStateShaderProperty.value.wrap,
                "Wrap",
                TextureSamplerState.WrapMode.Repeat,
                out var wrapVisualElement));
        }

        private void HandleGradientPropertyField(PropertySheet propertySheet, GradientShaderProperty gradientShaderProperty)
        {
            var gradientPropertyDrawer = new GradientPropertyDrawer();
            propertySheet.Add(gradientPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    this._changeValueCallback(newValue);
                    this._postChangeValueCallback();
                },
                gradientShaderProperty.value,
                "Default",
                out var propertyGradientField));
        }

        private void BuildKeywordFields(PropertySheet propertySheet, ShaderInput shaderInput)
        {
            var keyword = shaderInput as ShaderKeyword;
            if(keyword == null)
                return;

            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change Keyword type");
                    if (keyword.keywordDefinition == (KeywordDefinition) newValue)
                        return;
                    keyword.keywordDefinition = (KeywordDefinition) newValue;
                },
                keyword.keywordDefinition,
                "Definition",
                KeywordDefinition.ShaderFeature,
                out var typeField));

            typeField.SetEnabled(!keyword.isBuiltIn);

            if (keyword.keywordDefinition != KeywordDefinition.Predefined)
            {
                propertySheet.Add(enumPropertyDrawer.CreateGUIForField(
                    newValue =>
                    {
                        this._preChangeValueCallback("Change Keyword scope");
                        if (keyword.keywordScope == (KeywordScope) newValue)
                            return;
                        keyword.keywordScope = (KeywordScope) newValue;
                    },
                    keyword.keywordScope,
                    "Scope",
                    KeywordScope.Local,
                    out var scopeField));

                scopeField.SetEnabled(!keyword.isBuiltIn);
            }

            switch (keyword.keywordType)
            {
                case KeywordType.Boolean:
                    BuildBooleanKeywordField(propertySheet, keyword);
                    break;
                case KeywordType.Enum:
                    BuildEnumKeywordField(propertySheet, keyword);
                    break;
            }
        }

        private void BuildBooleanKeywordField(PropertySheet propertySheet, ShaderKeyword keyword)
        {
            var boolPropertyDrawer = new BoolPropertyDrawer();
            propertySheet.Add(boolPropertyDrawer.CreateGUIForField(
                newValue =>
                {
                    this._preChangeValueCallback("Change property value");
                    keyword.value = newValue.isOn ? 1 : 0;
                    this._postChangeValueCallback(false, ModificationScope.Graph);
                },
                new ToggleData(keyword.value == 1),
                "Default",
                out var boolKeywordField));
        }

        private void BuildEnumKeywordField(PropertySheet propertySheet, ShaderKeyword keyword)
        {
            // Clamp value between entry list
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);

            // Default field
            var field = new PopupField<string>(keyword.entries.Select(x => x.displayName).ToList(), value);
            field.RegisterValueChangedCallback(evt =>
            {
                this._preChangeValueCallback("Change Keyword Value");
                keyword.value = field.index;
                this._postChangeValueCallback(false, ModificationScope.Graph);
            });

            AddPropertyRowToSheet(propertySheet, field, "Default");

            // Entries
            var container = new IMGUIContainer(() => OnGUIHandler()) {name = "ListContainer"};
            AddPropertyRowToSheet(propertySheet, container, "Entries");
            container.SetEnabled(!keyword.isBuiltIn);
        }

        private static void AddPropertyRowToSheet(PropertySheet propertySheet, VisualElement control, string labelName)
        {
            propertySheet.Add(new PropertyRow(new Label(labelName)), (row) =>
            {
                row.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
                row.Add(control);
            });
        }

        private void OnGUIHandler()
        {
            if(m_ReorderableList == null)
            {
                RecreateList();
                AddCallbacks();
            }

            m_ReorderableList.index = m_SelectedIndex;
            m_ReorderableList.DoLayoutList();
        }

        internal void RecreateList()
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            // Create reorderable list from entries
            m_ReorderableList = new ReorderableList(keyword.entries, typeof(KeywordEntry), true, true, true, true);
        }

        private void AddCallbacks()
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            // Draw Header
            m_ReorderableList.drawHeaderCallback = (Rect rect) =>
            {
                int indent = 14;
                var displayRect = new Rect(rect.x + indent, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(displayRect, "Display Name");
                var referenceRect = new Rect((rect.x + indent) + (rect.width - indent) / 2, rect.y, (rect.width - indent) / 2, rect.height);
                EditorGUI.LabelField(referenceRect, "Reference Suffix");
            };

            // Draw Element
            m_ReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                KeywordEntry entry = ((KeywordEntry)m_ReorderableList.list[index]);
                EditorGUI.BeginChangeCheck();

                var displayName = EditorGUI.DelayedTextField( new Rect(rect.x, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.displayName, EditorStyles.label);
                var referenceName = EditorGUI.DelayedTextField( new Rect(rect.x + rect.width / 2, rect.y, rect.width / 2, EditorGUIUtility.singleLineHeight), entry.referenceName, EditorStyles.label);

                displayName = GetDuplicateSafeDisplayName(entry.id, displayName);
                referenceName = GetDuplicateSafeReferenceName(entry.id, referenceName.ToUpper());

                if(EditorGUI.EndChangeCheck())
                {
                    keyword.entries[index] = new KeywordEntry(index + 1, displayName, referenceName);

                    // Rebuild();
                    this._postChangeValueCallback(true);
                }
            };

            // Element height
            m_ReorderableList.elementHeightCallback = (int indexer) =>
            {
                return m_ReorderableList.elementHeight;
            };

            // Can add
            m_ReorderableList.onCanAddCallback = (ReorderableList list) =>
            {
                return list.count < 8;
            };

            // Can remove
            m_ReorderableList.onCanRemoveCallback = (ReorderableList list) =>
            {
                return list.count > 2;
            };

            // Add callback delegates
            m_ReorderableList.onSelectCallback += SelectEntry;
            m_ReorderableList.onAddCallback += AddEntry;
            m_ReorderableList.onRemoveCallback += RemoveEntry;
            m_ReorderableList.onReorderCallback += ReorderEntries;
        }

        private void SelectEntry(ReorderableList list)
        {
            m_SelectedIndex = list.index;
        }

        private void AddEntry(ReorderableList list)
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            this._preChangeValueCallback("Add Keyword Entry");

            var index = list.list.Count + 1;
            var displayName = GetDuplicateSafeDisplayName(index, "New");
            var referenceName = GetDuplicateSafeReferenceName(index, "NEW");

            // Add new entry
            keyword.entries.Add(new KeywordEntry(index, displayName, referenceName));

            // Update GUI
            // Rebuild();
            this._postChangeValueCallback(true);
            this._keywordChangedCallback();
            m_SelectedIndex = list.list.Count - 1;
        }

        private void RemoveEntry(ReorderableList list)
        {
            if(!(shaderInput is ShaderKeyword keyword))
                return;

            this._preChangeValueCallback("Remove Keyword Entry");

            // Remove entry
            m_SelectedIndex = list.index;
            var selectedEntry = (KeywordEntry)m_ReorderableList.list[list.index];
            keyword.entries.Remove(selectedEntry);

            // Clamp value within new entry range
            int value = Mathf.Clamp(keyword.value, 0, keyword.entries.Count - 1);
            keyword.value = value;

            // Rebuild();
            this._postChangeValueCallback(true);
            this._keywordChangedCallback();
        }

        private void ReorderEntries(ReorderableList list)
        {
            this._postChangeValueCallback(true);
        }

        public string GetDuplicateSafeDisplayName(int id, string name)
        {
            name = name.Trim();
            var entryList = m_ReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.displayName), "{0} ({1})", name);
        }

        public string GetDuplicateSafeReferenceName(int id, string name)
        {
            name = name.Trim();
            name = Regex.Replace(name, @"(?:[^A-Za-z_0-9])|(?:\s)", "_");
            var entryList = m_ReorderableList.list as List<KeywordEntry>;
            return GraphUtil.SanitizeName(entryList.Where(p => p.id != id).Select(p => p.referenceName), "{0}_{1}", name);
        }
    }
}
