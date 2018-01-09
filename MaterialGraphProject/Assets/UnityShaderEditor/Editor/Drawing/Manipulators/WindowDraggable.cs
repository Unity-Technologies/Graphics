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

        bool m_DockLeft;
        bool m_DockTop;

        Vector2 m_LocalMosueOffset;
        Rect m_PreviousParentRect;

        public WindowDraggable()
        {
            m_Active = false;
            m_PreviousParentRect = new Rect(0f, 0f, 0f, 0f);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), Capture.NoCapture);
            target.RegisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), Capture.NoCapture);
            target.RegisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), Capture.NoCapture);
            target.RegisterCallback<PostLayoutEvent>(InitialLayoutSetup);
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

            RefreshDocking();
        }

        void RefreshDocking()
        {
            Vector2 windowCenter = new Vector2(target.layout.x + target.layout.width * .5f, target.layout.y + target.layout.height * .5f);
            windowCenter /= target.parent.layout.size;

            m_DockLeft = windowCenter.x < .5f;
            m_DockTop = windowCenter.y < .5f;
        }

        void InitialLayoutSetup(PostLayoutEvent postLayoutEvent)
        {
            m_PreviousParentRect = target.parent.layout;
            target.UnregisterCallback<PostLayoutEvent>(InitialLayoutSetup);
            target.RegisterCallback<PostLayoutEvent>(OnPostLayout);

            RefreshDocking();
        }

        void OnPostLayout(PostLayoutEvent postLayoutEvent)
        {
            Rect windowRect = target.layout;

            Vector2 scaling = target.parent.layout.size / m_PreviousParentRect.size;

            Vector2 distanceFromEdge = Vector2.zero;
            distanceFromEdge.x = m_DockLeft ? target.layout.x : (m_PreviousParentRect.width - target.layout.x - target.layout.width);
            distanceFromEdge.y = m_DockTop ? target.layout.y: (m_PreviousParentRect.height - target.layout.y - target.layout.height);

            Vector2 normalizedDistanceFromEdge = distanceFromEdge / m_PreviousParentRect.size;

            windowRect.size *= scaling;

            if (m_DockLeft)
            {
                windowRect.x = normalizedDistanceFromEdge.x * target.parent.layout.width;
            }
            else
            {
                windowRect.x = (1f - normalizedDistanceFromEdge.x) * target.parent.layout.width - windowRect.width;
            }

            if (m_DockTop)
            {
                windowRect.y = normalizedDistanceFromEdge.y * target.parent.layout.height;
            }
            else
            {
                windowRect.y = (1f - normalizedDistanceFromEdge.y) * target.parent.layout.height- windowRect.height;
            }

            float maximumXPosition = target.parent.layout.width - windowRect.width;
            float maximumYPosition = target.parent.layout.height - windowRect.height;

            windowRect.x = Mathf.Clamp(windowRect.x, 0f, maximumXPosition);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, maximumYPosition);

            windowRect.width = Mathf.Min(windowRect.width, target.parent.layout.width);
            windowRect.height = Mathf.Min(windowRect.height, target.parent.layout.height);

            m_PreviousParentRect = target.parent.layout;

            target.layout = windowRect;
        }
    }
}

