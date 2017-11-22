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
                ValueToGUI();
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

        protected abstract void ValueToGUI();

        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }
    }


    class Vector2Field : VFXControl<Vector2>
    {
        LabeledField<FloatField, float> m_X;
        LabeledField<FloatField, float> m_Y;

        public bool dynamicUpdate
        {
            get
            {
                return m_X.control.dynamicUpdate;
            }
            set
            {
                m_X.control.dynamicUpdate = value;
                m_Y.control.dynamicUpdate = value;
            }
        }
        void CreateTextField()
        {
            m_X = new LabeledField<FloatField, float>("X");
            m_Y = new LabeledField<FloatField, float>("Y");

            m_X.control.AddToClassList("fieldContainer");
            m_Y.control.AddToClassList("fieldContainer");
            m_X.AddToClassList("fieldContainer");
            m_Y.AddToClassList("fieldContainer");

            m_X.RegisterCallback<ChangeEvent<float>>(OnXValueChanged);
            m_Y.RegisterCallback<ChangeEvent<float>>(OnYValueChanged);
        }

        void OnXValueChanged(ChangeEvent<float> e)
        {
            Vector2 newValue = value;
            newValue.x = (float)m_X.value;
            SetValueAndNotify(newValue);
        }

        void OnYValueChanged(ChangeEvent<float> e)
        {
            Vector2 newValue = value;
            newValue.y = (float)m_Y.value;
            SetValueAndNotify(newValue);
        }

        public Vector2Field()
        {
            CreateTextField();

            style.flexDirection = FlexDirection.Row;
            Add(m_X);
            Add(m_Y);
        }

        protected override void ValueToGUI()
        {
            m_X.value = value.x;
            m_Y.value = value.y;
        }
    }
}
