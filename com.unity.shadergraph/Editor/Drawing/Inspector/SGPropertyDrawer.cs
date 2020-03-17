using System;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
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
                field.RegisterValueChangedCallback(evt => valueChangedCallback(evt.newValue));
            });

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

        internal VisualElement CreateGUIForField(ValueChangedCallback valueChangedCallback,
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
                toggle.OnToggleChanged(evt => valueChangedCallback(new ToggleData(evt.newValue)));
            });

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
                textField.RegisterValueChangedCallback(evt => valueChangedCallback(evt.newValue));
            });
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
        internal delegate void ChangeValueCallback(int newValue);

        internal VisualElement CreateGUIForField(
            ChangeValueCallback changeValueCallback,
            int fieldToDraw,
            string labelName,
            out VisualElement propertyFloatField)
        {
            var integerField = new IntegerField {value = fieldToDraw};

            integerField.RegisterValueChangedCallback(evt =>
            {
                changeValueCallback((int) evt.newValue);
            });

            propertyFloatField = integerField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyFloatField);
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
        internal delegate void ChangeValueCallback(float newValue);

        internal VisualElement CreateGUIForField(
            ChangeValueCallback changeValueCallback,
            float fieldToDraw,
            string labelName,
            out VisualElement propertyFloatField)
        {
            var floatField = new FloatField {value = fieldToDraw};

            floatField.RegisterValueChangedCallback(evt =>
            {
                changeValueCallback((float) evt.newValue);
            });

            propertyFloatField = floatField;

            var defaultRow = new PropertyRow(new Label(labelName));
            defaultRow.Add(propertyFloatField);
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

    [SGPropertyDrawer(typeof(ShaderInput))]
    class ShaderInputPropertyDrawer : IPropertyDrawer
    {
        private PropertySheet _shaderInputVisualElement;
        internal delegate void ChangeExposedFieldCallback(bool newValue);
        internal delegate void ChangeReferenceNameCallback(string newValue);
        internal delegate void ChangeValueCallback(object newValue);
        internal delegate void PreChangeValueCallback(string actionName);
        internal delegate void PostChangeValueCallback();

        public void GetPropertyData(bool isSubGraph,
            ChangeExposedFieldCallback exposedFieldCallback,
            ChangeReferenceNameCallback referenceNameCallback,
            ChangeValueCallback changeValueCallback,
            PreChangeValueCallback preChangeValueCallback,
            PostChangeValueCallback postChangeValueCallback)
        {
            this.isSubGraph = isSubGraph;
            this._exposedFieldCallback = exposedFieldCallback;
            this._referenceNameChangedCallback = referenceNameCallback;
            this._propertyChangeCallback = changeValueCallback;
            this._preChangeValueCallback = preChangeValueCallback;
            this._postChangeValueCallback = postChangeValueCallback;
        }

        private bool isSubGraph { get ; set;  }
        private ChangeExposedFieldCallback _exposedFieldCallback;
        private ChangeReferenceNameCallback _referenceNameChangedCallback;
        private ChangeValueCallback _propertyChangeCallback;
        private PreChangeValueCallback _preChangeValueCallback;
        private PostChangeValueCallback _postChangeValueCallback;

        public VisualElement DrawProperty(
            PropertyInfo propertyInfo,
            object actualObject,
            Inspectable attribute)
        {
            var propertySheet = new PropertySheet();
            // #TODO Handle child classes, needs extra work?
            var shaderInput = actualObject as ShaderInput;
            BuildExposedField(propertySheet, shaderInput);
            BuildReferenceNameField(propertySheet, shaderInput);
            BuildPropertyFields(propertySheet, shaderInput);
            return propertySheet;
        }

        void BuildExposedField(PropertySheet propertySheet, ShaderInput shaderInput)
        {
            if(!isSubGraph)
            {
                var boolPropertyDrawer = new BoolPropertyDrawer();
                propertySheet.Add(boolPropertyDrawer.CreateGUIForField(
                    evt => _exposedFieldCallback(evt.isOn),
                    new ToggleData(shaderInput.generatePropertyBlock),
                    "Exposed",
                    out var propertyToggle));
                propertyToggle.SetEnabled(shaderInput.isExposable);
            }
        }

        void BuildReferenceNameField(PropertySheet propertySheet, ShaderInput shaderInput)
        {
            if (!isSubGraph || shaderInput is ShaderKeyword)
            {
                var textPropertyDrawer = new TextPropertyDrawer();
                propertySheet.Add(textPropertyDrawer.CreateGUIForField(
                    evt => _referenceNameChangedCallback(evt),
                    (string)shaderInput.referenceName,
                    "Reference",
                    out var propertyVisualElement));

                if(!string.IsNullOrEmpty(shaderInput.overrideReferenceName))
                    propertyVisualElement.AddToClassList("modified");
                propertyVisualElement.SetEnabled(shaderInput.isRenamable);
                propertyVisualElement.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyNameReferenceField"));
            }
        }

        void BuildPropertyFields(PropertySheet propertySheet, ShaderInput shaderInput)
        {
            var property = shaderInput as AbstractShaderProperty;
            if(property == null)
                return;

            switch(property)
            {
            case Vector1ShaderProperty vector1ShaderProperty:
                HandleVector1ShaderProperty(propertySheet, vector1ShaderProperty);
                break;
            default:
                break;
                // Do default handling with types that aren't bad
            }
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
                        newValue => _propertyChangeCallback(newValue),
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
                        newValue => this._propertyChangeCallback(newValue),
                        (int)vector1ShaderProperty.value,
                        "Default",
                        out var integerPropertyField));
                    break;
                default:
                    var defaultFloatPropertyDrawer = new FloatPropertyDrawer();
                    // Default field
                    propertySheet.Add(defaultFloatPropertyDrawer.CreateGUIForField(
                        newValue => this._propertyChangeCallback(newValue),
                        vector1ShaderProperty.value,
                        "Default",
                        out var defaultFloatPropertyField));
                    defaultFloatPropertyField.RegisterCallback<FocusOutEvent>(evt =>
                    {
                        this._preChangeValueCallback("Change Property Value");
                        float minValue = Mathf.Min(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.x);
                        float maxValue = Mathf.Max(vector1ShaderProperty.value, vector1ShaderProperty.rangeValues.y);
                        vector1ShaderProperty.rangeValues = new Vector2(minValue, maxValue);
                        this._postChangeValueCallback();
                    });
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
                        this._postChangeValueCallback();
                    },
                    vector1ShaderProperty.floatType,
                    "Mode",
                     FloatType.Default,
                    out var modePropertyEnumField));
            }
        }
    }
}
