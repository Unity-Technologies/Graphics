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

        bool m_OutputDeltaMovement;

        Vector2 m_PreviousMousePosition;

        public Draggable(Action<Vector2> handler, bool outputDeltaMovement = false)
        {
            m_Handler = handler;
            m_Active = false;
            m_OutputDeltaMovement = outputDeltaMovement;
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
            m_PreviousMousePosition = evt.localMousePosition;
            target.TakeMouseCapture();
            m_Active = true;
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_Active)
            {
                if (m_OutputDeltaMovement)
                {
                    Vector2 deltaMouse = evt.localMousePosition - m_PreviousMousePosition;
                    m_PreviousMousePosition = evt.localMousePosition;
                    m_Handler(deltaMouse);
                }
                else
                {
                    m_Handler(evt.localMousePosition);
                }
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
