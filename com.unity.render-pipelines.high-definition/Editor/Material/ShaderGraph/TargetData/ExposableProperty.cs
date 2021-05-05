using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    [Serializable]
    internal class ExposableProperty
    {
        [SerializeField]
        private bool exposed;

        [NonSerialized]
        public ExposableProperty parent;

        public bool IsExposed
        {
            get { return parent != null ? parent.IsExposed && exposed : exposed; }
            set { exposed = value; }
        }

        public BaseField<bool> GetExposeField(Action onChange, Action<string> registerUndo)
        {
            BaseField<bool> exposedField = new Toggle { value = exposed };
            exposedField.SetEnabled(parent != null ? parent.IsExposed : true);
            exposedField.RegisterValueChangedCallback((evt) => {
                if (exposed == evt.newValue)
                    return;

                registerUndo(exposedField.tooltip);
                exposed = evt.newValue;
                onChange();
            });

            return exposedField;
        }
    }

    /// <summary>
    /// Exposable Property helper class for a SubTarget property
    /// </summary>
    [Serializable]
    internal class ExposableProperty<T>: ExposableProperty
    {
        [SerializeField]
        public T value;

        public ExposableProperty(T initial = default, bool exposed = false)
        {
            value = initial;
            IsExposed = exposed;
        }

        public static implicit operator T(ExposableProperty<T> prop) => prop.value;

        public void AddProperty(TargetPropertyGUIContext context, GUIContent label, Action onChange, Action<string> registerUndo, int indentLevel = 0)
            => AddProperty(context, label.text, label.tooltip, onChange, registerUndo, indentLevel);

        public void AddProperty(TargetPropertyGUIContext context, string label, string tooltip, Action onChange, Action<string> registerUndo, int indentLevel = 0)
        {
            BaseField<bool> exposedField = GetExposeField(onChange, registerUndo);
            BaseField<T> elem = null;
            BaseField<Enum> elemEnum = null;

            switch (value)
            {
                case bool b: elem = new Toggle { value = b, tooltip = tooltip } as BaseField<T>; break;
                case int i: elem = new IntegerField { value = i, tooltip = tooltip } as BaseField<T>; break;
                case float f: elem = new FloatField { value = f, tooltip = tooltip } as BaseField<T>; break;
                case Enum e: elemEnum = new EnumField(e) { value = e, tooltip = tooltip }; break;
                default: throw new Exception($"Can't create UI field for type {typeof(T)}. Consider using TargetPropertyGUIContext.AddProperty instead.");
            }

            if (elem != null)
            {
                context.AddProperty(label, tooltip, indentLevel, elem, (evt) => {
                    if (Equals(value, evt.newValue))
                        return;

                    registerUndo(label);
                    value = evt.newValue;
                    onChange();
                }, exposedField);
            }
            else
            {
                context.AddProperty(label, tooltip, indentLevel, elemEnum, (evt) => {
                    if (Equals(value, evt.newValue))
                        return;

                    registerUndo(label);
                    value = (T)(object)evt.newValue;
                    onChange();
                }, exposedField);
            }
        }
    }
}
