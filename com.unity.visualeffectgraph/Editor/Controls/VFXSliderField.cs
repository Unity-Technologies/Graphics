using System;

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    interface ISliderScale
    {
        float ToLinear(float scaledValue);
        float ToScaled(float linearValue);
    }

    class LinearSliderScale : ISliderScale
    {
        public float ToLinear(float scaledValue) => scaledValue;

        public float ToScaled(float linearValue) => linearValue;
    }

    class LogarithmicSliderScale : ISliderScale
    {
        private readonly float m_Min;
        private readonly float m_Max;
        private readonly float m_MinValue;
        private readonly float m_MinLogValue;
        private readonly float m_MaxMinLogValue;
        private readonly float m_Factor;
        private readonly float m_InvLogBaseValue;
        private readonly float m_Base;
        private readonly bool m_SnapToPower;

        public LogarithmicSliderScale(Vector2 range, float logBase = 10, bool snapToPower = false)
        {
            if (range.x <= 0)
            {
                throw new ArgumentException("Logarithmic scale does not support a minimum range less or equal to zero", nameof(range));
            }

            // Check that maximum value can be snapped to
            if (snapToPower)
            {
                var expoMax = Math.Log10(range.y) / Math.Log10(logBase);
                var expoMin = Math.Log10(range.x) / Math.Log10(logBase);

                if (expoMax != Math.Round(expoMax) || expoMin != Math.Round(expoMin))
                {
                    throw new ArgumentException($"Logarithmic scale minimum and maximum values must be a power of the base when snapping is enabled ([{range.x}..{range.y}]");
                }
            }

            m_Min = range.x;
            m_Max = range.y;
            m_Base = logBase;
            m_InvLogBaseValue = 1f / (float)Math.Log10(logBase);
            m_MinValue = range.x;
            m_MinLogValue = (float)Math.Log10(m_MinValue) * m_InvLogBaseValue;
            m_MaxMinLogValue = (float)Math.Log10(range.y/range.x) * m_InvLogBaseValue;
            m_Factor = (range.y - range.x) / m_MaxMinLogValue;
            m_SnapToPower = snapToPower;
        }

        public float ToLinear(float scaledValue)
        {
            scaledValue = Mathf.Clamp(scaledValue, m_Min, m_Max);
            return m_MinValue + (float)Math.Log10(scaledValue/m_MinValue) * m_InvLogBaseValue * m_Factor;
        }

        public float ToScaled(float linearValue)
        {
            linearValue = Mathf.Clamp(linearValue, m_Min, m_Max);
            float power = m_MinLogValue + (linearValue - m_MinValue) / m_Factor;
            if (m_SnapToPower)
                power = Mathf.Round(power);
            return (float)Math.Pow(m_Base, power);
        }
    }

    abstract class VFXBaseSliderField<T> : VisualElement, INotifyValueChanged<T>, IVFXDraggedElement
    {
        protected readonly Slider m_Slider;
        protected readonly TextValueField<T> m_Field;

        private static readonly LinearSliderScale s_LinearSliderScale = new LinearSliderScale();

        private Action<IVFXDraggedElement> m_OnValueDragFinished;
        private Action<IVFXDraggedElement> m_OnValueDragStarted;
        private bool m_IgnoreNotification;
        private ISliderScale m_Scale;
        private T m_Value;
        private Vector2 m_Range;

        class StartFinishSliderManipulator : Manipulator
        {
            VisualElement m_DragContainer;
            VFXBaseSliderField<T> m_Slider;

            public StartFinishSliderManipulator(VFXBaseSliderField<T> slider)
            {
                m_Slider = slider;
            }

            protected override void RegisterCallbacksOnTarget()
            {
                target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.NoTrickleDown);
                m_DragContainer = target.Q("unity-drag-container"); // Weakness: if UIToolkit change the internal structure of a slider this code could break
                m_DragContainer.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                m_DragContainer.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            }

            void OnMouseDown(MouseDownEvent e)
            {
                m_Slider.ValueDragStarted();
            }

            void OnMouseUp(MouseUpEvent e)
            {
                m_Slider.ValueDragFinished();
            }
        }

        protected VFXBaseSliderField(TextValueField<T> field, ISliderScale customScale = null)
        {
            m_Slider = new Slider(0, 1, SliderDirection.Horizontal);
            m_Slider.AddToClassList("textfield");
            AddToClassList("sliderField");
            Add(m_Slider);

            m_Field = field;
            Add(m_Field);
            RegisterCallBack();

            scale = customScale ?? s_LinearSliderScale;
        }

        public ISliderScale scale
        {
            get => m_Scale;
            set => m_Scale = value ?? s_LinearSliderScale;
        }

        public void SetValueWithoutNotify(T newValue)
        {
            SetValueWithoutNotify(ValueToFloat(newValue), newValue);
        }

        public T value
        {
            get => m_Value;

            set => SetValueAndNotify(ValueToFloat(value), value);
        }

        public Vector2 range
        {
            get => m_Range;
            set
            {
                m_Range = value;
                m_IgnoreNotification = true;
                if (m_Slider.lowValue != m_Range.x || m_Slider.highValue != m_Range.y)
                {
                    m_Slider.lowValue = m_Range.x;
                    m_Slider.highValue = m_Range.y;

                    if (m_Slider.value < m_Slider.lowValue || m_Slider.value > m_Slider.highValue)
                    {
                        m_Slider.value = m_Slider.lowValue;
                    }
                }
                m_IgnoreNotification = false;
            }
        }

        public void SetOnValueDragStarted(Action<IVFXDraggedElement> callback) => m_OnValueDragStarted = callback;
        public void SetOnValueDragFinished(Action<IVFXDraggedElement> callback) => m_OnValueDragFinished = callback;

        private bool hasFocus => m_Field.HasFocus() || panel?.focusController.focusedElement == m_Field;

        private void ValueDragFinished()
        {
            m_OnValueDragFinished?.Invoke(this);
        }

        private void ValueDragStarted()
        {
            m_OnValueDragStarted?.Invoke(this);
        }

        private void RegisterCallBack()
        {
            m_Field.RegisterValueChangedCallback(ValueChanged);
            m_Field.RegisterCallback<BlurEvent>(OnFocusLost);
            m_Slider.RegisterValueChangedCallback(OnSliderValueChanged);
            m_Slider.AddManipulator(new StartFinishSliderManipulator(this));
        }

        private void OnFocusLost(BlurEvent e)
        {
            //forward the focus lost event
            using (BlurEvent newE = BlurEvent.GetPooled(this, e.relatedTarget, e.direction, panel.focusController))
            {
                SendEvent(newE);
            }

            e.StopPropagation();
        }

        private void OnSliderValueChanged(ChangeEvent<float> evt)
        {
            var scaledValue = m_Scale.ToScaled(evt.newValue);
            SetValueAndNotify(Mathf.Clamp(evt.newValue, range.x, range.y), FloatToValue(scaledValue));
        }

        private void SetValueAndNotify(float sliderValue, T typedNewValue)
        {
            if (!value.Equals(typedNewValue))
            {
                using (var evt = ChangeEvent<T>.GetPooled(value, typedNewValue))
                {
                    evt.target = this;
                    SetValueWithoutNotify(sliderValue, typedNewValue);
                    SendEvent(evt);
                }
            }
        }

        private void SetValueWithoutNotify(float sliderValue, T newTypedValue)
        {
            m_IgnoreNotification = true;
            m_Value = newTypedValue;
            tooltip = newTypedValue.ToString();
            if (!hasFocus)
                m_Field.value = newTypedValue;
            m_Slider.value = sliderValue;
            m_IgnoreNotification = false;
        }

        private void ValueChanged(ChangeEvent<T> e)
        {
            e.StopPropagation();
            if (!m_IgnoreNotification)
                SetValueAndNotify(ValueToFloat(e.newValue), e.newValue);
        }

        private float ValueToFloat(T v)
        {
            var scaledValue = (float)Convert.ChangeType(v, typeof(float));
            return scale.ToLinear(scaledValue);
        }

        private T FloatToValue(float v)
        {
            return (T)Convert.ChangeType(v, typeof(T));
        }
    }

    class VFXFloatSliderField : VFXBaseSliderField<float>
    {
        public VFXFloatSliderField() : base(CreateField()) { }

        private static FloatField CreateField() => new FloatField { name = "Field" };
    }

    class VFXIntSliderField : VFXBaseSliderField<int>
    {
        public VFXIntSliderField() : base(CreateField()) { }
        private static IntegerField CreateField() => new IntegerField { name = "Field" };
    }

    class VFXLongSliderField : VFXBaseSliderField<long>
    {
        public VFXLongSliderField() : base(CreateField()) { }

        private static LongField CreateField() => new LongField { name = "Field" };
    }
}
