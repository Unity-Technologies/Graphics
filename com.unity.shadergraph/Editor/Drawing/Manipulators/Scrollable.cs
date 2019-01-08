using UnityEngine;
using System;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    class Scrollable : MouseManipulator
    {
        Action<float> m_Handler;

        public Scrollable(Action<float> handler)
        {
            m_Handler = handler;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<WheelEvent>(HandleMouseWheelEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<WheelEvent>(HandleMouseWheelEvent);
        }

        void HandleMouseWheelEvent(WheelEvent evt)
        {
            m_Handler(evt.delta.y);
            evt.StopPropagation();
        }
    }
}
