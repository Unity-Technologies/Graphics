using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using UnityEditor.Experimental.UIElements;
using System.Collections.Generic;

namespace UnityEditor.VFX.UIElements
{
    class DoubleSliderField : VisualElement, INotifyValueChanged<double>
    {
        Slider m_Slider;
        DoubleField m_DoubleField;


        public double m_Value;

        public double value
        {
            get
            {
                return m_Value;
            }

            set
            {
                m_Value = value;
                m_DoubleField.value = value;
                m_Slider.value = (float)value;
            }
        }

        public void OnValueChanged(EventCallback<ChangeEvent<double>> callback)
        {
            RegisterCallback(callback);
        }

        public void SetValueAndNotify(double newValue)
        {
            if (!EqualityComparer<double>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<double> evt = ChangeEvent<double>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    value = newValue;
                    UIElementsUtility.eventDispatcher.DispatchEvent(evt, panel);
                }
            }
        }

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
            }
        }

        public DoubleSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, Slider.Direction.Horizontal, (range.y - range.x) * 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            m_DoubleField = new DoubleField();
            m_DoubleField.RegisterCallback<ChangeEvent<double>>(ValueChanged);
            m_DoubleField.dynamicUpdate = true;

            Add(m_Slider);
            Add(m_DoubleField);
        }

        void ValueChanged(ChangeEvent<double> e)
        {
            e.StopPropagation();
            SetValueAndNotify(e.newValue);
        }

        void ValueChanged(float newValue)
        {
            SetValueAndNotify(newValue);
        }
    }
    class IntSliderField : IntField
    {
        Slider m_Slider;

        void CreateSlider(Vector2 range)
        {
            m_Slider = new Slider(range.x, range.y, ValueChanged, Slider.Direction.Horizontal, (range.y - range.x) * 0.1f);
            m_Slider.AddToClassList("textfield");
        }

        public IntSliderField(string label, Vector2 range) : base(label)
        {
            CreateSlider(range);
            Add(m_Slider);
        }

        public IntSliderField(VisualElement existingLabel, Vector2 range) : base(existingLabel)
        {
            CreateSlider(range);
            Add(m_Slider);
        }

        void ValueChanged(float newValue)
        {
            SetValue((int)newValue);
        }

        protected override void ValueToGUI()
        {
            base.ValueToGUI();
            m_Slider.value = (float)GetValue();
        }
    }
}
