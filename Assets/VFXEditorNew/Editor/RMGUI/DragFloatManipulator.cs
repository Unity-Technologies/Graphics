using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor;

namespace UnityEditor.VFX.UIElements
{
    interface INumericPolicy<T>
    {
        T Zero();
        T Add(T a, T b);
        // add more functions here, such as multiplication etc.
        T FromFloat(float f);
    }

    class NumericPolicies:
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
            return (int)f;
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

        void SetValue(T value,object userData);
    }

    class DragValueManipulator<T> : Manipulator
    {
        public DragValueManipulator(IValueChangeListener<T> listener,object userdata)
        {
            phaseInterest = EventPhase.Capture;
            m_UserData = userdata;
            m_Listener = listener;
        }
        object m_UserData;
        IValueChangeListener<T> m_Listener;

        T m_OriginalValue;
        bool m_Dragging;
        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)   
		{
            switch( evt.type)
            {
            case EventType.MouseDown:
                if ( evt.button == 0 )
                {
				    EditorGUIUtility.SetWantsMouseJumping(1);
                    this.TakeCapture();
                    m_Dragging = true;
                    m_OriginalValue = m_Listener.GetValue(m_UserData);
                    return EventPropagation.Stop;
		    	}
            break;
		    case EventType.MouseUp:
			    if ( evt.button == 0 && m_Dragging)
                {
                    m_Dragging = false;
                    this.ReleaseCapture();
                    EditorGUIUtility.SetWantsMouseJumping(0);
				    return EventPropagation.Stop;
                }
			break;
		    case EventType.MouseDrag:
                if ( evt.button == 0 && m_Dragging)
                {
                    ApplyDelta(HandleUtility.niceMouseDelta);
                }
			break;
		    case EventType.KeyDown:
                if (m_Dragging && evt.keyCode == KeyCode.Escape)
                {
                    m_Listener.SetValue(m_OriginalValue,m_UserData);
                    m_Dragging = false;
                    return EventPropagation.Stop;
                }
			break;
            }
			return EventPropagation.Continue;
		}
        void ApplyDelta(float delta)
        {
            T value = m_Listener.GetValue(m_UserData);
            INumericPolicy<T> p = (INumericPolicy<T>)NumericPolicies.Instance;

            value = p.Add(value,p.FromFloat(delta));

            m_Listener.SetValue(value,m_UserData);
        }
    }

}