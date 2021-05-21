using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Collections.Generic;
using System.Linq;

using Action = System.Action;

namespace UnityEditor.VFX.UI
{
    class VFXMinMaxSliderField : VisualElement, INotifyValueChanged<Vector2>
    {
        protected MinMaxSlider m_Slider;
        VisualElement m_IndeterminateLabel;
        Vector2Field m_Vector2Field;


        class StartFinishSliderManipulator : Manipulator
        {
            VFXMinMaxSliderField m_Slider;
            bool m_InDrag;
            protected override void RegisterCallbacksOnTarget()
            {
                m_Slider = target.GetFirstOfType<VFXMinMaxSliderField>();

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

        public VFXMinMaxSliderField()
        {
            AddToClassList("sliderField");
            m_Slider = new MinMaxSlider(0,1, Single.MinValue, 1);
            // m_Slider = new MinMaxSlider("Value");
            m_Slider.AddToClassList("textfield");
            m_Slider.RegisterValueChangedCallback(evt => ValueChanged(evt.newValue));

            m_Vector2Field = new Vector2Field();
            m_Vector2Field.RegisterValueChangedCallback(ValueChanged);
            m_Vector2Field.name = "Field";


            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = VFXControlConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);

            Add(m_Slider);
            Add(m_Vector2Field);
            RegisterCallBack();
        }
        protected void RegisterCallBack()
        {
            (m_Vector2Field as VisualElement).RegisterCallback<BlurEvent>(OnFocusLost);

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

        public Vector2 m_Value;

        public Vector2 value
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
                if (m_Slider.minValue != m_Range.x || m_Slider.maxValue != m_Range.y)
                {
                    m_Slider.minValue = m_Range.x;
                    m_Slider.maxValue = m_Range.y;

                    m_Slider.value = m_Range;
                }
                m_IgnoreNotification = false;
            }
        }

        protected bool m_IgnoreNotification;

        public void SetValueAndNotify(Vector2 newValue)
        {
            if (!EqualityComparer<Vector2>.Default.Equals(value, newValue))
            {
                using (ChangeEvent<Vector2> evt = ChangeEvent<Vector2>.GetPooled(value, newValue))
                {
                    evt.target = this;
                    SetValueWithoutNotify(newValue);
                    SendEvent(evt);
                }
            }
        }

        public void SetValueWithoutNotify(Vector2 newValue)
        {
            m_IgnoreNotification = true;
            m_Value = newValue;
            tooltip = newValue.ToString();
            if (!hasFocus)
                m_Vector2Field.value = newValue;
            m_Slider.value = value;
            m_IgnoreNotification = false;
        }

        protected void ValueChanged(ChangeEvent<Vector2> e)
        {
            e.StopPropagation();
            if (!m_IgnoreNotification)
                SetValueAndNotify(e.newValue);
        }
        void ValueChanged(Vector2 newValue)
        {
            SetValueAndNotify(newValue);
        }

        public bool indeterminate
        {
            get { return m_Slider.parent == null; }

            set
            {
                if (indeterminate != value)
                {
                    if (value)
                    {
                        m_Slider.RemoveFromHierarchy();
                        Add(m_IndeterminateLabel);
                    }
                    else
                    {
                        m_IndeterminateLabel.RemoveFromHierarchy();
                        Add(m_Slider);
                    }

                    m_Slider.SetEnabled(!value);
                }
            }
        }
        public bool hasFocus
        {
            get
            {
                return m_Slider.HasFocus() || m_Vector2Field.HasFocus() || (panel != null && panel.focusController.focusedElement == m_Vector2Field as VisualElement);
            }
        }
    }
}
