using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor;
using UnityEditor.VFX.UI;

namespace UnityEditor.VFX.UIElements
{
    interface INumericPolicy<T>
    {
        T Zero();
        T Add(T a, T b);
        // add more functions here, such as multiplication etc.
        T FromFloat(float f);
    }

    class NumericPolicies :
        INumericPolicy<int>,
        INumericPolicy<long>,
        INumericPolicy<float>,
        INumericPolicy<double>
    {
        int INumericPolicy<int>.Zero() { return 0; }
        long INumericPolicy<long>.Zero() { return 0; }
        float INumericPolicy<float>.Zero() { return 0; }
        double INumericPolicy<double>.Zero() { return 0; }
        int INumericPolicy<int>.Add(int a, int b) { return a + b; }
        long INumericPolicy<long>.Add(long a, long b) { return a + b; }
        float INumericPolicy<float>.Add(float a, float b) { return a + b; }
        double INumericPolicy<double>.Add(double a, double b) { return a + b; }


        int INumericPolicy<int>.FromFloat(float f)
        {
            return (int)(f * 10f);
        }
        long INumericPolicy<long>.FromFloat(float f)
        {
            return (long)f;
        }
        float INumericPolicy<float>.FromFloat(float f)
        {
            return (float)f;
        }
        double INumericPolicy<double>.FromFloat(float f)
        {
            return (double)f;
        }
        // implement all functions from INumericPolicy<> interfaces.

        public static NumericPolicies Instance = new NumericPolicies();
    }

    interface IValueChangeListener<T>
    {
        T GetValue(object userData);

        void SetValue(T value, object userData);

        bool enabled
        {
            get;
        }
    }

    class DragValueManipulator<T> : Manipulator, IEventHandler
    {
        public DragValueManipulator(IValueChangeListener<T> listener, object userdata)
        {
            m_UserData = userdata;
            m_Listener = listener;
        }

        object m_UserData;
        IValueChangeListener<T> m_Listener;

        T m_OriginalValue;
        bool m_Dragging;


        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, Capture.Capture);
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public bool HasCaptureHandlers()
        {
            return true;
        }

        public bool HasBubbleHandlers()
        {
            return true;
        }

        void IEventHandler.HandleEvent(EventBase evt)
        {
            if (evt is MouseCaptureOutEvent)
            {
                OnLostCapture();
            }
        }

        void OnLostCapture()
        {
            m_Dragging = false;
            EditorGUIUtility.SetWantsMouseJumping(0);
        }

        void Release()
        {
            if (m_Dragging)
            {
                m_Dragging = false;

                //TODO reactivate live modification
                //VFXViewPresenter.viewPresenter.EndLiveModification();
                if (target.HasMouseCapture())
                {
                    target.ReleaseMouseCapture();
                }
                EditorGUIUtility.SetWantsMouseJumping(0);

                target.UnregisterCallback<MouseMoveEvent>(OnMouseDrag);
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 0 && m_Dragging)
            {
                Release();

                evt.StopPropagation();
            }
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                EditorGUIUtility.SetWantsMouseJumping(1);
                target.RegisterCallback<MouseMoveEvent>(OnMouseDrag, Capture.Capture);
                target.TakeMouseCapture();
                m_Dragging = true;
                //TODO reactivate live modification
                //VFXViewPresenter.viewPresenter.BeginLiveModification();
                m_OriginalValue = m_Listener.GetValue(m_UserData);
                evt.StopPropagation();
            }
        }

        void OnMouseDrag(MouseMoveEvent evt)
        {
            if (m_Dragging)
            {
                if (!target.HasMouseCapture())
                {
                    Release();
                    return;
                }
                if (evt.button == 0 && m_Dragging)
                {
                    ApplyDelta(HandleUtility.niceMouseDelta);
                }
                evt.StopPropagation();
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Dragging && evt.keyCode == KeyCode.Escape)
            {
                m_Listener.SetValue(m_OriginalValue, m_UserData);
                Release();
            }
        }

        void ApplyDelta(float delta)
        {
            T value = m_Listener.GetValue(m_UserData);
            INumericPolicy<T> p = (INumericPolicy<T>)NumericPolicies.Instance;

            value = p.Add(value, p.FromFloat(Mathf.Round(delta) * 0.1f));

            m_Listener.SetValue(value, m_UserData);
        }
    }

    class UIDragValueManipulator<T> : Manipulator, IEventHandler
    {
        public UIDragValueManipulator(INotifyValueChanged<T> listener)
        {
            m_Listener = listener;
        }

        INotifyValueChanged<T> m_Listener;

        T m_OriginalValue;
        bool m_Dragging;


        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, Capture.Capture);
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public bool HasCaptureHandlers()
        {
            return true;
        }

        public bool HasBubbleHandlers()
        {
            return true;
        }

        void IEventHandler.HandleEvent(EventBase evt)
        {
            if (evt.GetEventTypeId() == MouseCaptureOutEvent.TypeId())
            {
                OnLostCapture();
            }
        }

        void OnLostCapture()
        {
            m_Dragging = false;
            EditorGUIUtility.SetWantsMouseJumping(0);
        }

        void Release()
        {
            if (m_Dragging)
            {
                m_Dragging = false;

                //TODO reactivate live modification
                //VFXViewPresenter.viewPresenter.EndLiveModification();
                if (target.HasMouseCapture())
                {
                    target.ReleaseMouseCapture();
                }
                EditorGUIUtility.SetWantsMouseJumping(0);

                target.UnregisterCallback<MouseMoveEvent>(OnMouseDrag);
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            if (evt.button == 0 && m_Dragging)
            {
                Release();

                evt.StopPropagation();
            }
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == 0)
            {
                EditorGUIUtility.SetWantsMouseJumping(1);
                target.RegisterCallback<MouseMoveEvent>(OnMouseDrag, Capture.Capture);
                target.TakeMouseCapture();
                m_Dragging = true;
                //TODO reactivate live modification
                //VFXViewPresenter.viewPresenter.BeginLiveModification();
                m_OriginalValue = m_Listener.value;
                evt.StopPropagation();
            }
        }

        void OnMouseDrag(MouseMoveEvent evt)
        {
            if (m_Dragging)
            {
                if (!target.HasMouseCapture())
                {
                    Release();
                    return;
                }
                if (evt.button == 0 && m_Dragging)
                {
                    ApplyDelta(HandleUtility.niceMouseDelta);
                }
                evt.StopPropagation();
            }
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (m_Dragging && evt.keyCode == KeyCode.Escape)
            {
                m_Listener.value = m_OriginalValue;
                Release();
            }
        }

        void ApplyDelta(float delta)
        {
            T value = m_Listener.value;
            INumericPolicy<T> p = (INumericPolicy<T>)NumericPolicies.Instance;

            value = p.Add(value, p.FromFloat(Mathf.Round(delta) * 0.1f));

            m_Listener.value = value;
        }
    }
}
