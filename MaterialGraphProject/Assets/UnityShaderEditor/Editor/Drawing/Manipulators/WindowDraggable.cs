using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class WindowDraggable : MouseManipulator
    {
        bool m_Active;

        Vector2 m_LocalMosueOffset;

        public WindowDraggable()
        {
            m_Active = false;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), Capture.NoCapture);
            target.RegisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), Capture.NoCapture);
            target.RegisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), Capture.NoCapture);
            target.RegisterCallback<PostLayoutEvent>(OnPostLayout);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), Capture.NoCapture);
            target.UnregisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), Capture.NoCapture);
            target.UnregisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), Capture.NoCapture);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            m_Active = true;
            m_LocalMosueOffset = target.WorldToLocal(evt.mousePosition);

            target.TakeMouseCapture();
            evt.StopPropagation();
        }

        void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_Active)
            {
                Rect layout = target.layout;
                layout.position = target.parent.WorldToLocal(evt.mousePosition - m_LocalMosueOffset);
                target.layout = layout;
            }
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            m_Active = false;

            if (target.HasMouseCapture())
            {
                target.ReleaseMouseCapture();
            }

            evt.StopPropagation();
        }

        void OnPostLayout(PostLayoutEvent postLayoutEvent)
        {
            Rect inspectorViewRect = target.layout;

            float minimumXPosition = target.layout.width - inspectorViewRect.width;
            float maximumXPosition = target.parent.layout.width - target.layout.width;

            float minimumYPosition = target.layout.height - inspectorViewRect.height;
            float maximumYPosition = target.parent.layout.height - target.layout.height;

            inspectorViewRect.x = Mathf.Clamp(inspectorViewRect.x, minimumXPosition, maximumXPosition);
            inspectorViewRect.y = Mathf.Clamp(inspectorViewRect.y, minimumYPosition, maximumYPosition);

            inspectorViewRect.width = Mathf.Min(inspectorViewRect.width, target.layout.width);
            inspectorViewRect.height = Mathf.Min(inspectorViewRect.height, target.layout.height);

            target.layout = inspectorViewRect;
        }
    }
}

