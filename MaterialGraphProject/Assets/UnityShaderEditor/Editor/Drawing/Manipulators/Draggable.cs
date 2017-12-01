using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class Draggable : MouseManipulator
    {
        Action<Vector2> m_Handler;

        bool m_Active;

        public Draggable(Action<Vector2> handler)
        {
            m_Handler = handler;
            m_Active = false;
            activators.Add(new ManipulatorActivationFilter()
            {
                button = MouseButton.LeftMouse
            });
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), Capture.NoCapture);
            target.RegisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), Capture.NoCapture);
            target.RegisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), Capture.NoCapture);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), Capture.NoCapture);
            target.UnregisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), Capture.NoCapture);
            target.UnregisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), Capture.NoCapture);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            target.TakeMouseCapture();
            m_Active = true;
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_Active)
            {
                m_Handler(evt.localMousePosition);
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            m_Active = false;
            target.ReleaseMouseCapture();
            evt.StopPropagation();
        }
    }
}
