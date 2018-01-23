using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using System.Collections.Generic;
using FloatField = UnityEditor.VFX.UIElements.VFXFloatField;

namespace UnityEditor.VFX.UIElements
{
    abstract class VFXBaseSliderField<T> : VisualElement, INotifyValueChanged<T>
    {
        protected Slider m_Slider;
        protected INotifyValueChanged<T> m_Field;

        public VFXBaseSliderField()
        {
            AddToClassList("sliderField");
        }

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

        public abstract bool hasFocus {get; }
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
    class VFXDoubleSliderField : VFXBaseSliderField<float>
    {
        public VFXDoubleSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, Slider.Direction.Horizontal, (range.y - range.x) * 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            var doubleField = new FloatField();
            doubleField.RegisterCallback<ChangeEvent<float>>(ValueChanged);
            doubleField.name = "Field";
            m_Field = doubleField;

            Add(m_Slider);
            Add(doubleField);
        }

        public override bool hasFocus
        {
            get
            {
                return (m_Field as FloatField).hasFocus;
            }
        }

        protected override float ValueToFloat(float value)
        {
            return value;
        }

        void ValueChanged(float newValue)
        {
            SetValueAndNotify(newValue);
        }
    }
    class VFXIntSliderField : VFXBaseSliderField<long>
    {
        public VFXIntSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, Slider.Direction.Horizontal, 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            var integerField = new IntegerField();
            integerField.RegisterCallback<ChangeEvent<long>>(ValueChanged);
            integerField.name = "Field";
            m_Field = integerField;

            Add(m_Slider);
            Add(integerField);
        }

        public override bool hasFocus
        {
            get
            {
                return (m_Field as IntegerField).hasFocus;
            }
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
