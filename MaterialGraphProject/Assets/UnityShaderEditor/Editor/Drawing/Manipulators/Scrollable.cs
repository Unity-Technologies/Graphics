using System;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class Scrollable : MouseManipulator
    {
        Action<float> m_Handler;

        public Scrollable(Action<float> handler)
        {
            m_Handler = handler;
            activators.Add(new ManipulatorActivationFilter()
            {
                button = MouseButton.LeftMouse
            });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback(new EventCallback<WheelEvent>(OnMouseWheel), Capture.NoCapture);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback(new EventCallback<WheelEvent>(OnMouseWheel), Capture.NoCapture);
        }

        void OnMouseWheel(WheelEvent evt)
        {
            m_Handler(evt.delta.y);
        }
    }
}
