using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;

using Action = System.Action;

namespace UnityEditor.VFX.UI
{
    abstract class VFXBaseSliderField<T> : VisualElement, INotifyValueChanged<T>
    {
        protected Slider m_Slider;
        protected INotifyValueChanged<T> m_Field;


        class StartFinishSliderManipulator : Manipulator
        {
            VFXBaseSliderField<T> m_Slider;
            bool m_InDrag;
            protected override void RegisterCallbacksOnTarget()
            {
                m_Slider = target.GetFirstOfType<VFXBaseSliderField<T>>();

                target.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                target.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            }

            protected override void UnregisterCallbacksFromTarget()
            {
                target.UnregisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
                target.UnregisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
            }

            void OnMouseDown(MouseDownEvent e)
            {
                target.RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            }

            void OnMouseMove(MouseMoveEvent e)
            {
                if (!m_InDrag)
                {
                    m_InDrag = true;
                    m_Slider.ValueDragStarted();
                    target.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown); //we care only about the first drag event
                }
            }

            void OnMouseUp(MouseUpEvent e)
            {
                if (m_InDrag)
                    m_Slider.ValueDragFinished();
                else
                    target.UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
                m_InDrag = false;
            }
        }

        protected void ValueDragFinished()
        {
            if (onValueDragFinished != null)
                onValueDragFinished();
        }

        protected void ValueDragStarted()
        {
            if (onValueDragStarted != null)
                onValueDragStarted();
        }

        public Action onValueDragFinished;
        public Action onValueDragStarted;

        public VFXBaseSliderField()
        {
            AddToClassList("sliderField");
        }

        protected void RegisterCallBack()
        {
            (m_Field as VisualElement).RegisterCallback<BlurEvent>(OnFocusLost);

            m_Slider.Children().First().AddManipulator(new StartFinishSliderManipulator());
        }

        void OnFocusLost(BlurEvent e)
        {
            //forward the focus lost event
            using (BlurEvent newE = BlurEvent.GetPooled(this, e.relatedTarget, e.direction, panel.focusController))
            {
                SendEvent(newE);
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
                SetValueAndNotify(value);
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

                    if (m_Slider.value < m_Slider.lowValue || m_Slider.value > m_Slider.highValue)
                    {
                        m_Slider.value = m_Slider.lowValue;
                    }
                }
                m_IgnoreNotification = false;
            }
        }

        protected bool m_IgnoreNotification;

        public abstract bool hasFocus { get; }
        public void OnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        public void RemoveOnValueChanged(EventCallback<ChangeEvent<T>> callback)
        {
            UnregisterCallback(callback);
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
            m_IgnoreNotification = true;
            m_Value = newValue;
            tooltip = newValue.ToString();
            if (!hasFocus)
                m_Field.value = newValue;
            m_Slider.value = ValueToFloat(value);
            m_IgnoreNotification = false;
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
            m_Slider = new Slider(0, 1, SliderDirection.Horizontal);
            m_Slider.AddToClassList("textfield");
            m_Slider.RegisterValueChangedCallback(evt => ValueChanged(evt.newValue));

            m_FloatField = new FloatField();
            m_FloatField.RegisterValueChangedCallback(ValueChanged);
            m_FloatField.name = "Field";
            m_Field = m_FloatField;

            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = VFXControlConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);

            Add(m_Slider);
            Add(m_FloatField);
            RegisterCallBack();
        }

        VisualElement m_IndeterminateLabel;
        FloatField m_FloatField;

        public override bool hasFocus
        {
            get { return ((FloatField)m_Field).HasFocus() || (panel != null && panel.focusController.focusedElement == m_Field as VisualElement); }
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

        public bool indeterminate
        {
            get { return m_FloatField.parent == null; }

            set
            {
                if (indeterminate != value)
                {
                    if (value)
                    {
                        m_FloatField.RemoveFromHierarchy();
                        Add(m_IndeterminateLabel);
                    }
                    else
                    {
                        m_IndeterminateLabel.RemoveFromHierarchy();
                        Add(m_FloatField);
                    }
                    m_Slider.SetEnabled(!value);
                }
            }
        }
    }
    class VFXIntSliderField : VFXBaseSliderField<int>
    {
        public VFXIntSliderField()
        {
            m_Slider = new Slider(0, 1, SliderDirection.Horizontal);
            m_Slider.AddToClassList("textfield");
            m_Slider.RegisterValueChangedCallback(evt => ValueChanged(evt.newValue));

            var integerField = new IntegerField();
            integerField.RegisterValueChangedCallback(ValueChanged);
            integerField.name = "Field";
            m_Field = integerField;

            Add(m_Slider);
            Add(integerField);
            RegisterCallBack();
        }

        public override bool hasFocus
        {
            get { return ((IntegerField)m_Field).HasFocus() || (panel != null && panel.focusController.focusedElement == m_Field as VisualElement); }
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
            m_Slider = new Slider(0, 1, SliderDirection.Horizontal);
            m_Slider.AddToClassList("textfield");
            m_Slider.RegisterValueChangedCallback(evt => ValueChanged(evt.newValue));

            var integerField = new LongField();
            integerField.RegisterValueChangedCallback(ValueChanged);
            integerField.name = "Field";
            m_Field = integerField;

            Add(m_Slider);
            Add(integerField);
            RegisterCallBack();
        }

        public override bool hasFocus
        {
            get { return ((LongField)m_Field).HasFocus() || (panel != null && panel.focusController.focusedElement == m_Field as VisualElement); }
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
