using UnityEngine;
using UnityEngine.UIElements;
using System;

namespace UnityEditor.VFX.UI
{
    //Copied from mousefield dragger but add notifications needed for delayed fields
    class VFXFieldMouseDragger<T>
    {
        Action m_OnDragFinished;
        Action m_OnDragStarted;
        public VFXFieldMouseDragger(IValueField<T> drivenField, Action onDragFinished = null, Action onDragStarted = null)
        {
            m_DrivenField = drivenField;
            m_DragElement = null;
            m_DragHotZone = new Rect(0, 0, -1, -1);
            m_OnDragFinished = onDragFinished;
            m_OnDragStarted = onDragStarted;
            dragging = false;
        }

        IValueField<T> m_DrivenField;
        VisualElement m_DragElement;
        Rect m_DragHotZone;

        public bool dragging;
        public T startValue;

        public void SetDragZone(VisualElement dragElement)
        {
            SetDragZone(dragElement, new Rect(0, 0, -1, -1));
        }

        public void SetDragZone(VisualElement dragElement, Rect hotZone)
        {
            if (m_DragElement != null)
            {
                m_DragElement.UnregisterCallback<MouseDownEvent>(UpdateValueOnMouseDown);
                m_DragElement.UnregisterCallback<MouseMoveEvent>(UpdateValueOnMouseMove);
                m_DragElement.UnregisterCallback<MouseUpEvent>(UpdateValueOnMouseUp);
                m_DragElement.UnregisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }

            m_DragElement = dragElement;
            m_DragHotZone = hotZone;

            if (m_DragElement != null)
            {
                dragging = false;
                m_DragElement.RegisterCallback<MouseDownEvent>(UpdateValueOnMouseDown);
                m_DragElement.RegisterCallback<MouseMoveEvent>(UpdateValueOnMouseMove);
                m_DragElement.RegisterCallback<MouseUpEvent>(UpdateValueOnMouseUp);
                m_DragElement.RegisterCallback<KeyDownEvent>(UpdateValueOnKeyDown);
            }
        }

        void UpdateValueOnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0 && (m_DragHotZone.width < 0 || m_DragHotZone.height < 0 || m_DragHotZone.Contains(m_DragElement.WorldToLocal(evt.mousePosition))))
            {
                m_DragElement.CaptureMouse();

                // Make sure no other elements can capture the mouse!
                evt.StopPropagation();

                dragging = true;
                startValue = m_DrivenField.value;
                if (m_OnDragStarted != null)
                    m_OnDragStarted();

                EditorGUIUtility.SetWantsMouseJumping(1);
            }
        }

        void UpdateValueOnMouseMove(MouseMoveEvent evt)
        {
            if (dragging)
            {
                DeltaSpeed s = evt.shiftKey ? DeltaSpeed.Fast : (evt.altKey ? DeltaSpeed.Slow : DeltaSpeed.Normal);
                m_DrivenField.ApplyInputDeviceDelta(evt.mouseDelta, s, startValue);
            }
        }

        void UpdateValueOnMouseUp(MouseUpEvent evt)
        {
            if (dragging)
            {
                dragging = false;
                MouseCaptureController.ReleaseMouse();
                EditorGUIUtility.SetWantsMouseJumping(0);
                if (m_OnDragFinished != null)
                    m_OnDragFinished();
            }
        }

        void UpdateValueOnKeyDown(KeyDownEvent evt)
        {
            if (dragging && evt.keyCode == KeyCode.Escape)
            {
                dragging = false;
                m_DrivenField.value = startValue;
                MouseCaptureController.ReleaseMouse();
                EditorGUIUtility.SetWantsMouseJumping(0);
            }
        }
    }

    interface IVFXDraggedElement
    {
        void SetOnValueDragStarted(Action<IVFXDraggedElement> callback);
        void SetOnValueDragFinished(Action<IVFXDraggedElement> callback);
    }

    class VFXLabeledField<T, U> : VisualElement, INotifyValueChanged<U>, IVFXDraggedElement where T : VisualElement, INotifyValueChanged<U>, new()
    {
        private Action<IVFXDraggedElement> m_OnValueDragFinished;
        private Action<IVFXDraggedElement> m_OnValueDragStarted;

        private bool m_Indeterminate;

        protected Label m_Label;
        protected T m_Control;

        public VisualElement m_IndeterminateLabel;

        public VFXLabeledField(Label existingLabel, string controlName = null)
        {
            m_Label = existingLabel;

            CreateControl(controlName);
            SetupLabel();
        }


        public bool indeterminate
        {
            get { return m_Control.parent == null; }

            set
            {
                if (m_Indeterminate != value)
                {
                    m_Indeterminate = value;
                    if (value)
                    {
                        m_Control.RemoveFromHierarchy();
                        Add(m_IndeterminateLabel);
                    }
                    else
                    {
                        m_IndeterminateLabel.RemoveFromHierarchy();
                        Add(m_Control);
                    }
                }
            }
        }

        public VFXLabeledField(string label, string controlName = null)
        {
            if (!string.IsNullOrEmpty(label))
            {
                m_Label = new Label() { text = label };
                m_Label.AddToClassList("label");

                Add(m_Label);
            }
            style.flexDirection = FlexDirection.Row;

            CreateControl(controlName);
            SetupLabel();
        }

        public void SetOnValueDragStarted(Action<IVFXDraggedElement> callback) => m_OnValueDragStarted = callback;
        public void SetOnValueDragFinished(Action<IVFXDraggedElement> callback) => m_OnValueDragFinished = callback;

        void SetupLabel()
        {
            if (typeof(IValueField<U>).IsAssignableFrom(typeof(T)))
            {
                m_Label.styleSheets.Add(VFXView.LoadStyleSheet("VFXLabeledField"));

                var dragger = new VFXFieldMouseDragger<U>((IValueField<U>)m_Control, DragValueFinished, DragValueStarted);
                dragger.SetDragZone(m_Label);
                m_Label.AddToClassList("cursor-slide-arrow");
            }

            m_IndeterminateLabel = new Label()
            {
                name = "indeterminate",
                text = VFXControlConstants.indeterminateText
            };
            m_IndeterminateLabel.SetEnabled(false);
        }

        void DragValueFinished()
        {
            m_OnValueDragFinished?.Invoke(this);
        }

        void DragValueStarted()
        {
            m_OnValueDragStarted?.Invoke(this);
        }

        void CreateControl(string controlName)
        {
            m_Control = new T { name = controlName };
            Add(m_Control);

            m_Control.RegisterValueChangedCallback(OnControlChange);
        }

        void OnControlChange(ChangeEvent<U> e)
        {
            e.StopPropagation();
            using (ChangeEvent<U> evt = ChangeEvent<U>.GetPooled(e.previousValue, e.newValue))
            {
                evt.target = this;
                SendEvent(evt);
            }
        }

        public T control
        {
            get { return m_Control; }
        }

        public Label label
        {
            get { return m_Label; }
        }

        public void SetValueAndNotify(U newValue)
        {
            m_Control.value = newValue;
        }

        public void SetValueWithoutNotify(U newValue)
        {
            m_Control.SetValueWithoutNotify(newValue);
        }

        public U value
        {
            get { return m_Control.value; }
            set { m_Control.value = value; }
        }
    }

    abstract class ValueControl<T> : VisualElement
    {
        protected Label m_Label;

        protected ValueControl(Label existingLabel)
        {
            m_Label = existingLabel;
        }

        protected ValueControl(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                m_Label = new Label() { text = label };
                m_Label.AddToClassList("label");

                Add(m_Label);
            }
            style.flexDirection = FlexDirection.Row;
        }

        public T GetValue()
        {
            return m_Value;
        }

        public void SetValue(T value)
        {
            m_Value = value;
            ValueToGUI(false);
        }

        public T value
        {
            get { return GetValue(); }
            set { SetValue(value); }
        }

        public void SetMultiplier(T multiplier)
        {
            m_Multiplier = multiplier;
        }

        protected T m_Value;
        protected T m_Multiplier;

        public System.Action OnValueChanged;

        protected abstract void ValueToGUI(bool force);
    }
}
