using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    static class VFXControlConstants
    {
        public const string indeterminateText = "\u2014";
        public static readonly Color indeterminateTextColor = new Color(0.82f, 0.82f, 0.82f);
    }

    abstract class VFXControl<T> : VisualElement, INotifyValueChanged<T>
    {
        T m_Value;
        public T value
        {
            get { return m_Value; }
            set
            {
                SetValueAndNotify(value);
            }
        }
        public void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    SetValueWithoutNotify(newValue);
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(T newValue)
        {
            m_Value = newValue;
            ValueToGUI(false);
        }

        public new virtual void SetEnabled(bool value)
        {
        }

        public void ForceUpdate()
        {
            ValueToGUI(true);
        }

        public abstract bool indeterminate {get; set; }

        protected abstract void ValueToGUI(bool force);

        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        public void RemoveOnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            UnregisterCallback(callback);
        }
    }
}
