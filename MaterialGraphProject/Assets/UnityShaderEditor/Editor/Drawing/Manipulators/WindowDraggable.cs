﻿using System;
 using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class WindowDraggable : MouseManipulator
    {
        bool m_ResizeWithParentWindow;

        bool m_Active;

        WindowDockingLayout m_WindowDockingLayout;

        Vector2 m_LocalMosueOffset;
        Rect m_PreviousParentRect;

        VisualElement m_Handle;

        public Action OnDragFinished;

        public WindowDraggable(VisualElement handle = null, bool resizeWithParentwindow = false)
        {
            m_Handle = handle;
            m_ResizeWithParentWindow = resizeWithParentwindow;
            m_Active = false;
            m_PreviousParentRect = new Rect(0f, 0f, 0f, 0f);
            m_WindowDockingLayout = new WindowDockingLayout();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            if (m_Handle == null)
                m_Handle = target;
            m_Handle.RegisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), Capture.NoCapture);
            m_Handle.RegisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), Capture.NoCapture);
            m_Handle.RegisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), Capture.NoCapture);
            target.RegisterCallback<PostLayoutEvent>(InitialLayoutSetup);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            m_Handle.UnregisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), Capture.NoCapture);
            m_Handle.UnregisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), Capture.NoCapture);
            m_Handle.UnregisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), Capture.NoCapture);
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            m_Active = true;
            m_LocalMosueOffset = m_Handle.WorldToLocal(evt.mousePosition);

            m_Handle.TakeMouseCapture();
            evt.StopImmediatePropagation();
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
            bool emitDragFinishedEvent = m_Active;

            m_Active = false;

            if (m_Handle.HasMouseCapture())
            {
                m_Handle.ReleaseMouseCapture();
            }

            evt.StopImmediatePropagation();

            m_WindowDockingLayout.CalculateDockingCornerAndOffset(target.layout, target.parent.layout);

            if (emitDragFinishedEvent && OnDragFinished != null)
            {
                OnDragFinished();
            }
        }

        void InitialLayoutSetup(PostLayoutEvent postLayoutEvent)
        {
            m_PreviousParentRect = target.parent.layout;
            target.UnregisterCallback<PostLayoutEvent>(InitialLayoutSetup);
            target.RegisterCallback<PostLayoutEvent>(OnPostLayout);

            m_WindowDockingLayout.CalculateDockingCornerAndOffset(target.layout, target.parent.layout);
        }

        void OnPostLayout(PostLayoutEvent postLayoutEvent)
        {
            Rect windowRect = target.layout;

            Vector2 scaling = target.parent.layout.size / m_PreviousParentRect.size;

            Vector2 minSize = new Vector2(60f, 60f);

            if (!Mathf.Approximately(target.style.minWidth, 0f))
            {
                minSize.x = target.style.minWidth;
            }

            if (!Mathf.Approximately(target.style.minHeight, 0f))
            {
                minSize.y = target.style.minHeight;
            }

            Vector2 distanceFromParentEdge = Vector2.zero;
            distanceFromParentEdge.x = m_WindowDockingLayout.dockingLeft ? target.layout.x : (m_PreviousParentRect.width - target.layout.x - target.layout.width);
            distanceFromParentEdge.y = m_WindowDockingLayout.dockingTop ? target.layout.y: (m_PreviousParentRect.height - target.layout.y - target.layout.height);

            Vector2 normalizedDistanceFromEdge = distanceFromParentEdge / m_PreviousParentRect.size;

            if (m_ResizeWithParentWindow)
            {
                if (scaling.x > 1f)
                {
                    scaling.x = target.parent.layout.width * .33f < minSize.x ? 1f : scaling.x;
                }

                if (scaling.y > 1f)
                {
                    scaling.y = target.parent.layout.height * .33f < minSize.y ? 1f : scaling.y;
                }

                windowRect.size *= scaling;
            }
            else
            {
                normalizedDistanceFromEdge = distanceFromParentEdge / target.parent.layout.size;
            }

            if (m_WindowDockingLayout.dockingLeft)
            {
                windowRect.x = normalizedDistanceFromEdge.x * target.parent.layout.width;
            }
            else
            {
                windowRect.x = (1f - normalizedDistanceFromEdge.x) * target.parent.layout.width - windowRect.width;
            }

            if (m_WindowDockingLayout.dockingTop)
            {
                windowRect.y = normalizedDistanceFromEdge.y * target.parent.layout.height;
            }
            else
            {
                windowRect.y = (1f - normalizedDistanceFromEdge.y) * target.parent.layout.height - windowRect.height;
            }

            windowRect.width = Mathf.Max(windowRect.width, minSize.x);
            windowRect.height = Mathf.Max(windowRect.height, minSize.y);

            float maximumXPosition = Mathf.Max(target.parent.layout.width - windowRect.width, 0f);
            float maximumYPosition = Mathf.Max(target.parent.layout.height - windowRect.height, 0f);

            windowRect.x = Mathf.Clamp(windowRect.x, 0f, maximumXPosition);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, maximumYPosition);

            m_PreviousParentRect = target.parent.layout;

            target.layout = windowRect;
        }
    }
}
