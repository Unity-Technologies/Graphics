using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.Rendering.HighDefinition.ShaderGraph
{
    [Serializable]
    internal class ExposableProperty//: IEquatable<ExposableProperty<T>>
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

        public void AddProperty(TargetPropertyGUIContext context, string label, Action onChange, Action<string> registerUndo, int indentLevel = 0)
            => AddProperty(context, label, null, onChange, registerUndo, indentLevel);

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
                context.AddProperty(label, indentLevel, elem, (evt) => {
                    if (Equals(value, evt.newValue))
                        return;

                    registerUndo(label);
                    value = evt.newValue;
                    onChange();
                }, exposedField);
            }
            else
            {
                context.AddProperty(label, indentLevel, elemEnum, (evt) => {
                    if (Equals(value, evt.newValue))
                        return;

                    registerUndo(label);
                    value = (T)(object)evt.newValue;
                    onChange();
                }, exposedField);
            }
        }

        /*
        public static bool operator ==(ExposableProperty<T> lhs, T rhs) => !ReferenceEquals(lhs.value, null) && lhs.value.Equals(rhs);
        public static bool operator !=(ExposableProperty<T> lhs, T rhs) => !(lhs == rhs);
        public bool Equals(ExposableProperty<T> other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return System.Collections.Generic.EqualityComparer<T>.Default.Equals(value, other.value);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((ExposableProperty<T>)obj);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + value.GetHashCode();
                hash = hash * 23 + exposed.GetHashCode();
                return hash;
            }
        }

        public static explicit operator T(ExposableProperty<T> prop) => prop.value;
        */
    }
}
