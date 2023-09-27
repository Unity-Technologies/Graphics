using System;
using UnityEditor.ShaderGraph.Drawing;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class Draggable : MouseManipulator
    {
        Action<Vector2> m_Handler;

        bool m_Active;

        bool m_OutputDeltaMovement;

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
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        }

        void OnCaptureLost(MouseCaptureOutEvent e)
        {
            m_Active = false;
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            target.CaptureMouse();
            m_Active = true;
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_Active)
            {
                if (m_OutputDeltaMovement)
                {
                    m_Handler(evt.mouseDelta);
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
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);

            if (target.HasMouseCapture())
            {
                target.ReleaseMouse();
            }

            evt.StopPropagation();
        }
    }
}
