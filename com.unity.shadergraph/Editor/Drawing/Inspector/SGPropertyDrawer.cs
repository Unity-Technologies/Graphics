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

        internal VisualElement CreateGUIForField(ValueChangedCallback valueChangedCallback,
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

        internal VisualElement CreateGUIForField(ValueChangedCallback valueChangedCallback,
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

    [SGPropertyDrawer(typeof(float))]
    class SliderPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ChangeValueCallback(object newValue);
        internal delegate void CommitValueCallback(FocusOutEvent evt);

        private Vector2 _range;
        private float _value;
        private ChangeValueCallback _valueChangedCallback;
        private CommitValueCallback _valueCommittedCallback;
        private ChangeValueCallback _minimumChangedCallback;
        private CommitValueCallback _minimumCommittedCallback;
        private ChangeValueCallback _maximumChangedCallback;
        private CommitValueCallback _maximumCommittedCallback;

        public void GetPropertyData(Vector2 range,
                                    ChangeValueCallback valueChangedCallback,
                                    CommitValueCallback valueCommittedCallback,
                                    ChangeValueCallback minimumChangedCallback,
                                    CommitValueCallback minimumCommittedCallback,
                                    ChangeValueCallback maximumChangedCallback,
                                    CommitValueCallback maximumCommittedCallback)
        {
            this._range = range;
            this._valueChangedCallback = valueChangedCallback;
            this._valueCommittedCallback = valueCommittedCallback;
            this._minimumChangedCallback = minimumChangedCallback;
            this._minimumCommittedCallback = minimumCommittedCallback;
            this._maximumChangedCallback = maximumChangedCallback;
            this._maximumCommittedCallback = maximumCommittedCallback;
        }

        internal VisualElement CreateGUIForField(
            float fieldToDraw,
            out VisualElement propertyFloatField,
            out VisualElement minFloatField,
            out VisualElement maxFloatField)
        {
            this._value = fieldToDraw;
            float min = Mathf.Min(this._value, _range.x);
            float max = Mathf.Max(this._value, _range.y);
            _range = new Vector2(min, max);

            var defaultField = new FloatField {value = this._value};
            var minField = new FloatField {value = _range.x};
            var maxField = new FloatField {value = _range.y};

            defaultField.RegisterValueChangedCallback(evt =>
            {
                this._value = (float)evt.newValue;
                this._valueChangedCallback((float) evt.newValue);
            });
            defaultField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                float minValue = Mathf.Min(this._value, _range.x);
                float maxValue = Mathf.Max(this._value, _range.y);
                _range = new Vector2(minValue, maxValue);
                minField.value = minValue;
                maxField.value = maxValue;
                this._valueCommittedCallback(evt);
            });
            minField.RegisterValueChangedCallback(evt =>
            {
                _range = new Vector2((float) evt.newValue, _range.y);
                this._minimumChangedCallback((float)evt.newValue);
            });
            minField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                this._value = Mathf.Max(Mathf.Min(this._value, _range.y), _range.x);
                defaultField.value = this._value;
                this._minimumCommittedCallback(evt);
            });
            maxField.RegisterValueChangedCallback(evt =>
            {
                _range = new Vector2(_range.x, (float) evt.newValue);
                this._maximumChangedCallback((float) evt.newValue);
            });
            maxField.Q("unity-text-input").RegisterCallback<FocusOutEvent>(evt =>
            {
                this._value = Mathf.Max(Mathf.Min(this._value, _range.y), _range.x);
                defaultField.value = this._value;
                this._maximumCommittedCallback(evt);
            });

            propertyFloatField = defaultField;
            minFloatField = minField;
            maxFloatField = maxField;

            var propertySheet = new PropertySheet();
            var defaultRow = new PropertyRow(new Label("Default"));
            defaultRow.Add(defaultField);
            var minRow = new PropertyRow(new Label("Min"));
            minRow.Add(minField);
            var maxRow = new PropertyRow(new Label("Max"));
            maxRow.Add(maxField);

            propertySheet.Add(defaultRow);
            propertySheet.Add(minRow);
            propertySheet.Add(maxRow);

            return propertySheet;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            // How to draw a property with the 7 other callbacks?!

            throw new NotImplementedException();
        }
    }

    [SGPropertyDrawer(typeof(float))]
    class FloatPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ChangeValueCallback(object newValue);

        internal VisualElement CreateGUIForField(
            ChangeValueCallback valueChangedCallback,
            float fieldToDraw,
            out VisualElement propertyFloatField)
        {
            var defaultField = new FloatField {value = fieldToDraw};

            defaultField.RegisterValueChangedCallback(evt =>
            {
                valueChangedCallback((float) evt.newValue);
            });

            var propertySheet = new PropertySheet();
            var defaultRow = new PropertyRow(new Label("Default"));
            defaultRow.Add(defaultField);

            propertySheet.Add(defaultRow);

            propertyFloatField = defaultField;

            return propertySheet;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            throw new NotImplementedException();
        }
    }

    [SGPropertyDrawer(typeof(ShaderInput))]
    class ShaderInputPropertyDrawer : IPropertyDrawer
    {
        internal delegate void ChangeExposedFieldCallback(bool newValue);
        internal delegate void ChangeReferenceNameCallback(string newValue);
        internal delegate void ChangeValueCallback(object newValue);
        internal delegate void CommitValueCallback();

        public void GetPropertyData(bool isSubGraph,
            ChangeExposedFieldCallback exposedFieldCallback,
            ChangeReferenceNameCallback referenceNameCallback,
            ChangeValueCallback propertyValueChangedCallback,
            CommitValueCallback propertyValueCommittedCallback,
            ChangeValueCallback changeMinimumCallback,
            CommitValueCallback commitMinimumCallback,
            ChangeValueCallback changeMaximumCallback,
            CommitValueCallback commitMaximumCallback)
        {
            this.isSubGraph = isSubGraph;
            this.ExposedFieldCallback = exposedFieldCallback;
            this.ReferenceNameChangedCallback = referenceNameCallback;
            this.PropertyChangeCallback = propertyValueChangedCallback;
            this.PropertyCommitCallback = propertyValueCommittedCallback;
            this.MinimumChangeCallback = changeMinimumCallback;
            this.MinimumCommitCallback = commitMinimumCallback;
            this.MaximumChangeCallback = changeMaximumCallback;
            this.MaximumCommitCallback = commitMaximumCallback;
        }

        private bool isSubGraph { get ; set;  }
        private ChangeExposedFieldCallback ExposedFieldCallback;
        private ChangeReferenceNameCallback ReferenceNameChangedCallback;
        private ChangeValueCallback PropertyChangeCallback;
        private CommitValueCallback PropertyCommitCallback;
        private ChangeValueCallback MinimumChangeCallback;
        private CommitValueCallback MinimumCommitCallback;
        private ChangeValueCallback MaximumChangeCallback;
        private CommitValueCallback MaximumCommitCallback;

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
                    evt => ExposedFieldCallback(evt.isOn),
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
                    evt => ReferenceNameChangedCallback(evt),
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
            switch (vector1ShaderProperty.floatType)
            {
                case FloatType.Slider:
                    // Handle slider type
                    var sliderPropertyDrawer = new SliderPropertyDrawer();
                    sliderPropertyDrawer.GetPropertyData(vector1ShaderProperty.rangeValues,
                                    newValue => this.PropertyChangeCallback(newValue),
                                                        evt => this.PropertyCommitCallback(),
                                                        newValue => this.MinimumChangeCallback(newValue),
                                                        evt => this.MinimumCommitCallback(),
                                                            newValue => this.MaximumChangeCallback(newValue),
                                                            evt => this.MaximumCommitCallback());

                    propertySheet.Add(sliderPropertyDrawer.CreateGUIForField(vector1ShaderProperty.value,
                        out var propertyFloatField,
                        out var minFloatField,
                        out var maxFloatField));

                    break;
                case FloatType.Integer:
                    // Handle integer
                    break;
                default:

                    // Handle Enum
                    break;
            }
        }
    }
}
