using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.Rendering.LWRP.Path2D.GUIFramework;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal abstract class RectSelector<T> : ISelector<T>
    {
        public Action<ISelector<T>, bool> onSelectionBegin;
        public Action<ISelector<T>> onSelectionChanged;
        public Action<ISelector<T>> onSelectionEnd = null;

        private GUISystem m_GUISystem;
        private Control m_RectSelectorControl;
        private GUIAction m_RectSelectAction;
        private Vector3 m_RectStart;
        private Vector3 m_RectEnd;
        private Rect m_GUIRect;
        private IDrawer m_Drawer = new Drawer();

        public Rect guiRect
        {
            get { return m_GUIRect; }
        }

        public RectSelector() : this(new GUISystem(new GUIState())) { }
        
        public RectSelector(GUISystem guiSystem)
        {
            m_GUISystem = guiSystem;

            m_RectSelectorControl = new GenericDefaultControl("RectSelector")
            {
                position = (guiState) =>
                {
                    return GUIToWorld(guiState, guiState.mousePosition);
                },
                forward = (guiState) =>
                {
                    if (Camera.current)
                        return Camera.current.transform.forward;

                    return Vector3.forward;
                },
                right = (guiState) =>
                {
                    if (Camera.current)
                        return Camera.current.transform.right;

                    return Vector3.right;
                },
                up = (guiState) =>
                {
                    if (Camera.current)
                        return Camera.current.transform.up;

                    return Vector3.up;
                }
            };

            m_RectSelectAction = new SliderAction(m_RectSelectorControl)
            {
                enableRepaint = (guiState, action) =>
                {
                    var size = m_RectStart - m_RectEnd;
                    return size != Vector3.zero && guiState.hotControl == action.ID;
                },
                onClick = (guiState, control) =>
                {
                    m_RectStart = GUIToWorld(guiState, guiState.mousePosition);
                    m_RectEnd = m_RectStart;
                    m_GUIRect = CalculateGUIRect();
                },
                onSliderBegin = (guiState, control, position) =>
                {
                    m_RectEnd = position;
                    m_GUIRect = CalculateGUIRect();

                    if (onSelectionBegin != null)
                        onSelectionBegin(this, guiState.isShiftDown);
                },
                onSliderChanged = (guiState, control, position) =>
                {
                    m_RectEnd = position;
                    m_GUIRect = CalculateGUIRect();

                    if (onSelectionChanged != null)
                        onSelectionChanged(this);
                },
                onSliderEnd = (guiState, control, position) =>
                {
                    if (onSelectionEnd != null)
                        onSelectionEnd(this);
                },
                onRepaint = (guiState, action) =>
                {
                    m_Drawer.DrawSelectionRect(m_GUIRect);
                }
            };

            m_GUISystem.AddControl(m_RectSelectorControl);
            m_GUISystem.AddAction(m_RectSelectAction);
        }

        private Vector3 GUIToWorld(IGUIState guiState, Vector2 guiPosition)
        {
            var forward = Vector3.forward;

            if (guiState.HasCurrentCamera())
                forward = Camera.current.transform.forward;

            return guiState.GUIToWorld(guiPosition, forward, Vector3.zero);
        }

        private Rect CalculateGUIRect()
        {
            return FromToRect(HandleUtility.WorldToGUIPoint(m_RectStart), HandleUtility.WorldToGUIPoint(m_RectEnd));
        }

        private Rect FromToRect(Vector2 start, Vector2 end)
        {
            Rect r = new Rect(start.x, start.y, end.x - start.x, end.y - start.y);
            if (r.width < 0)
            {
                r.x += r.width;
                r.width = -r.width;
            }
            if (r.height < 0)
            {
                r.y += r.height;
                r.height = -r.height;
            }
            return r;
        }

        public void OnGUI()
        {
            m_GUISystem.OnGUI();
        }

        bool ISelector<T>.Select(T element)
        {
            return Select(element);
        }

        protected abstract bool Select(T element);
    }
}
