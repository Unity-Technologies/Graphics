using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using System.Collections.Generic;

namespace UnityEditor.VFX.UIElements
{
    abstract class BaseSliderField<T> : VisualElement, INotifyValueChanged<T>
    {
        protected Slider m_Slider;
        protected INotifyValueChanged<T> m_Field;

        public T m_Value;

        public T value
        {
            get
            {
                return m_Value;
            }

            set
            {
                m_Value = value;
                m_Field.value = value;
                m_Slider.value = ValueToFloat(value);
            }
        }

        protected abstract float ValueToFloat(T value);


        private Vector2 m_Range;

        public Vector2 range
        {
            get
            {
                return m_Range;
            }
            set
            {
                m_Range = value;
                m_Slider.lowValue = m_Range.x;
                m_Slider.highValue = m_Range.y;
                m_Slider.pageSize = (m_Slider.highValue - m_Slider.lowValue) * 0.1f;
            }
        }
        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
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

        protected void ValueChanged(ChangeEvent<T> e)
        {
            e.StopPropagation();
            SetValueAndNotify(e.newValue);
        }
    }
    class DoubleSliderField : BaseSliderField<double>
    {
        public DoubleSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, Slider.Direction.Horizontal, (range.y - range.x) * 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            var doubleField = new DoubleField();
            doubleField.RegisterCallback<ChangeEvent<double>>(ValueChanged);
            doubleField.dynamicUpdate = true;
            m_Field = doubleField;

            Add(m_Slider);
            Add(doubleField);
        }

        protected override float ValueToFloat(double value)
        {
            return (float)value;
        }

        void ValueChanged(float newValue)
        {
            SetValueAndNotify((double)newValue);
        }
    }
    class IntSliderField : BaseSliderField<long>
    {
        public IntSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, Slider.Direction.Horizontal, 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            var integerField = new IntegerField();
            integerField.RegisterCallback<ChangeEvent<long>>(ValueChanged);
            integerField.dynamicUpdate = true;
            m_Field = integerField;

            Add(m_Slider);
            Add(integerField);
        }

        protected override float ValueToFloat(long value)
        {
            return (float)value;
        }

        void ValueChanged(float newValue)
        {
            SetValueAndNotify((long)newValue);
        }
    }
}
