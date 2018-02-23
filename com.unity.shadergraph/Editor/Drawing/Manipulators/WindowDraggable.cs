﻿using System;
using UnityEngine;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
#if UNITY_2018_1
using GeometryChangedEvent = UnityEngine.Experimental.UIElements.PostLayoutEvent;
#endif

namespace UnityEditor.ShaderGraph.Drawing
{
    public class WindowDraggable : MouseManipulator
    {
        bool m_Active;

        WindowDockingLayout m_WindowDockingLayout;

        Vector2 m_LocalMosueOffset;
        Rect m_PreviousParentRect;

        VisualElement m_Handle;
        GraphView m_GraphView;

        public Action OnDragFinished;

        public WindowDraggable(VisualElement handle = null)
        {
            m_Handle = handle;
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
            target.RegisterCallback<GeometryChangedEvent>(InitialLayoutSetup);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            m_Handle.UnregisterCallback(new EventCallback<MouseDownEvent>(OnMouseDown), Capture.NoCapture);
            m_Handle.UnregisterCallback(new EventCallback<MouseMoveEvent>(OnMouseMove), Capture.NoCapture);
            m_Handle.UnregisterCallback(new EventCallback<MouseUpEvent>(OnMouseUp), Capture.NoCapture);
            target.UnregisterCallback<GeometryChangedEvent>(InitialLayoutSetup);
            target.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            if (m_GraphView != null)
                m_GraphView.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
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

        void InitialLayoutSetup(GeometryChangedEvent GeometryChangedEvent)
        {
            m_PreviousParentRect = target.parent.layout;
            target.UnregisterCallback<GeometryChangedEvent>(InitialLayoutSetup);
            target.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            VisualElement parent = target.parent;
            while (parent != null && !(parent is GraphView))
                parent = parent.parent;
            m_GraphView = parent as GraphView;
            if (m_GraphView != null)
                m_GraphView.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            m_WindowDockingLayout.CalculateDockingCornerAndOffset(target.layout, target.parent.layout);
        }

        void OnGeometryChanged(GeometryChangedEvent GeometryChangedEvent)
        {
            Rect windowRect = target.layout;

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
            distanceFromParentEdge.y = m_WindowDockingLayout.dockingTop ? target.layout.y : (m_PreviousParentRect.height - target.layout.y - target.layout.height);

            Vector2 normalizedDistanceFromEdge = distanceFromParentEdge / target.parent.layout.size;

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
