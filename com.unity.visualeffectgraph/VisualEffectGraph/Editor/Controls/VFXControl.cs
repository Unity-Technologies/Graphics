using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;

using System.Collections.Generic;

namespace UnityEditor.VFX.UIElements
{
    public abstract class VFXControl<T> : VisualElement, INotifyValueChanged<T>
    {
        T m_Value;
        public T value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                ValueToGUI(false);
            }
        }
        public void SetValueAndNotify(T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<T> evt = ChangeEvent<T>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

        public void ForceUpdate()
        {
            ValueToGUI(true);
        }

        protected abstract void ValueToGUI(bool force);

        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }
    }
}
