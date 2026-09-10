using System;
using UnityEngine;
using UnityEngine.UIElements;

using System.Collections.Generic;

namespace UnityEditor.VFX.UI
{
    static class VFXControlConstants
    {
        public const string indeterminateText = "\u2014";
        public static readonly Color indeterminateTextColor = new Color(0.82f, 0.82f, 0.82f);
    }

    interface IVFXControl
    {
        bool indeterminate { get; set; }
        // The boolean argument denotes whether the value underwent a net change during drag
        event Action<bool> onValueDragFinished;
        event Action onValueDragStarted;
        void ForceUpdate();
        void SetEnabled(bool isEnabled);
    }

    abstract class VFXControl<T> : VisualElement, INotifyValueChanged<T>, IVFXControl
    {
        T m_Value;
        public T value
        {
            get => m_Value;
            set => SetValueAndNotify(value);
        }

        T m_ValueAtDragStart;

        public event Action<bool> onValueDragFinished;
        public event Action onValueDragStarted;

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

        public abstract bool indeterminate { get; set; }

        protected abstract void ValueToGUI(bool force);

        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        public void RemoveOnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            UnregisterCallback(callback);
        }

        protected void ValueDragFinished(PointerCaptureOutEvent evt)
        {
            onValueDragFinished?.Invoke(!EqualityComparer<T>.Default.Equals(m_ValueAtDragStart, value));
        }

        protected void ValueDragStarted(PointerCaptureEvent evt)
        {
            m_ValueAtDragStart = value;
            onValueDragStarted?.Invoke();
        }
    }
}
