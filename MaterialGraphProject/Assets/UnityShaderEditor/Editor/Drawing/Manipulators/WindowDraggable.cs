using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    public class WindowDraggable : MouseManipulator
    {
        bool m_ResizeWithParentWindow;

        bool m_Active;

        bool m_DockLeft;
        bool m_DockTop;

        Vector2 m_LocalMosueOffset;
        Rect m_PreviousParentRect;

        public Action OnDragFinished;

        public WindowDraggable(bool resizeWithParentwindow = false)
        {
            m_ResizeWithParentWindow = resizeWithParentwindow;
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
            bool emitDragFinishedEvent = m_Active;

            m_Active = false;

            if (target.HasMouseCapture())
            {
                target.ReleaseMouseCapture();
            }

            evt.StopPropagation();

            RefreshDocking();

            if (emitDragFinishedEvent && OnDragFinished != null)
            {
                OnDragFinished();
            }
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
            distanceFromParentEdge.x = m_DockLeft ? target.layout.x : (m_PreviousParentRect.width - target.layout.x - target.layout.width);
            distanceFromParentEdge.y = m_DockTop ? target.layout.y: (m_PreviousParentRect.height - target.layout.y - target.layout.height);

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

            windowRect.width = Mathf.Max(Mathf.Min(windowRect.width, target.parent.layout.width), minSize.x);
            windowRect.height = Mathf.Max(Mathf.Min(windowRect.height, target.parent.layout.height), minSize.y);

            float maximumXPosition = Mathf.Max(target.parent.layout.width - windowRect.width, 0f);
            float maximumYPosition = Mathf.Max(target.parent.layout.height - windowRect.height, 0f);

            windowRect.x = Mathf.Clamp(windowRect.x, 0f, maximumXPosition);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, maximumYPosition);

            m_PreviousParentRect = target.parent.layout;

            target.layout = windowRect;
        }
    }
}
