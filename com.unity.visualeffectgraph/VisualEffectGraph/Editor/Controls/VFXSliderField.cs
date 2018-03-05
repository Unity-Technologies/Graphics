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

        protected void RegisterCallBack()
        {
            (m_Field as VisualElement).RegisterCallback<BlurEvent>(OnFocusLost);
        }

        void OnFocusLost(BlurEvent e)
        {
            //forward the focus lost event
            using (BlurEvent newE = BlurEvent.GetPooled(this, e.relatedTarget, e.direction))
            {
                UIElementsUtility.eventDispatcher.DispatchEvent(newE, null);
            }

            e.StopPropagation();
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
                m_IgnoreNotification = true;
                m_Value = value;
                m_Field.value = value;
                m_Slider.value = ValueToFloat(value);
                m_IgnoreNotification = false;
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
                m_IgnoreNotification = true;
                if (m_Slider.lowValue != m_Range.x || m_Slider.highValue != m_Range.y)
                {
                    m_Slider.lowValue = m_Range.x;
                    m_Slider.highValue = m_Range.y;
                    m_Slider.pageSize = (m_Slider.highValue - m_Slider.lowValue) * 0.1f;

                    //TODO ask fix in Slider

                    m_Slider.value = m_Range.x;
                    m_Slider.value = m_Range.y;
                    m_Slider.value = ValueToFloat(this.value);
                }
                m_IgnoreNotification = false;
            }
        }

        protected bool m_IgnoreNotification;

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
            if (!m_IgnoreNotification)
                SetValueAndNotify(e.newValue);
        }
    }
    class VFXFloatSliderField : VFXBaseSliderField<float>
    {
        public VFXFloatSliderField()
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
            RegisterCallBack();
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
            if (!m_IgnoreNotification)
                SetValueAndNotify(newValue);
        }
    }
    class VFXIntSliderField : VFXBaseSliderField<int>
    {
        public VFXIntSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, Slider.Direction.Horizontal, 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            var integerField = new IntegerField();
            integerField.RegisterCallback<ChangeEvent<int>>(ValueChanged);
            integerField.name = "Field";
            m_Field = integerField;

            Add(m_Slider);
            Add(integerField);
            RegisterCallBack();
        }

        public override bool hasFocus
        {
            get
            {
                return (m_Field as IntegerField).hasFocus;
            }
        }

        protected override float ValueToFloat(int value)
        {
            return (float)value;
        }

        void ValueChanged(float newValue)
        {
            SetValueAndNotify((int)newValue);
        }
    }
    class VFXLongSliderField : VFXBaseSliderField<long>
    {
        public VFXLongSliderField()
        {
            m_Slider = new Slider(0, 1, ValueChanged, Slider.Direction.Horizontal, 0.1f);
            m_Slider.AddToClassList("textfield");
            m_Slider.valueChanged += ValueChanged;

            var integerField = new LongField();
            integerField.RegisterCallback<ChangeEvent<long>>(ValueChanged);
            integerField.name = "Field";
            m_Field = integerField;

            Add(m_Slider);
            Add(integerField);
            RegisterCallBack();
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
