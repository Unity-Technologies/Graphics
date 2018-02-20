using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.ShaderGraph.Drawing
{
    enum ResizeDirection
    {
        Any,
        Vertical,
        Horizontal
    }

    public enum ResizeHandleAnchor
    {
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left,
        TopLeft
    }

    public class ResizeSideHandle : VisualElement
    {
        VisualElement m_ResizeTarget;

        bool m_StayWithinParentBounds;

        public bool stayWithinParentBounds
        {
            get { return m_StayWithinParentBounds; }
            set { m_StayWithinParentBounds = value; }
        }

        bool m_MaintainAspectRatio;

        public bool maintainAspectRatio
        {
            get { return m_MaintainAspectRatio; }
            set { m_MaintainAspectRatio = value; }
        }

        public Action OnResizeFinished;

        bool m_DockingLeft;
        bool m_DockingTop;
        bool m_Dragging;

        float m_InitialAspectRatio;

        Rect m_ResizeBeginLayout;
        Vector2 m_ResizeBeginMousePosition;

        public ResizeSideHandle(VisualElement resizeTarget, ResizeHandleAnchor anchor)
        {
            m_ResizeTarget = resizeTarget;

            AddToClassList("resize");

            switch (anchor)
            {
                case ResizeHandleAnchor.Top:
                {
                    AddToClassList("vertical");
                    AddToClassList("top");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromTop);
                    break;
                }
                case ResizeHandleAnchor.TopRight:
                {
                    AddToClassList("diagonal");
                    AddToClassList("top-right");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromTopRight);
                    break;
                }
                case ResizeHandleAnchor.Right:
                {
                    AddToClassList("horizontal");
                    AddToClassList("right");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromRight);
                    break;
                }
                case ResizeHandleAnchor.BottomRight:
                {
                    AddToClassList("diagonal");
                    AddToClassList("bottom-right");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromBottomRight);
                    break;
                }
                case ResizeHandleAnchor.Bottom:
                {
                    AddToClassList("vertical");
                    AddToClassList("bottom");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromBottom);
                    break;
                }
                case ResizeHandleAnchor.BottomLeft:
                {
                    AddToClassList("diagonal");
                    AddToClassList("bottom-left");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromBottomLeft);
                    break;
                }
                case ResizeHandleAnchor.Left:
                {
                    AddToClassList("horizontal");
                    AddToClassList("left");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromLeft);
                    break;
                }
                case ResizeHandleAnchor.TopLeft:
                {
                    AddToClassList("diagonal");
                    AddToClassList("top-left");
                    RegisterCallback<MouseMoveEvent>(HandleResizeFromTopLeft);
                    break;
                }
            }

            RegisterCallback<MouseDownEvent>(HandleMouseDown);
            RegisterCallback<MouseUpEvent>(HandleDraggableMouseUp);

            m_ResizeTarget.RegisterCallback<PostLayoutEvent>(InitialLayoutSetup);
        }

        void InitialLayoutSetup(PostLayoutEvent evt)
        {
            m_ResizeTarget.UnregisterCallback<PostLayoutEvent>(InitialLayoutSetup);
            m_InitialAspectRatio = m_ResizeTarget.layout.width / m_ResizeTarget.layout.height;
        }

        Vector2 GetMinSize()
        {
            Vector2 minSize = new Vector2(60f, 60f);

            if (!Mathf.Approximately(m_ResizeTarget.style.minWidth.value, 0f))
            {
                minSize.x = m_ResizeTarget.style.minWidth;
            }

            if (!Mathf.Approximately(m_ResizeTarget.style.minHeight.value, 0f))
            {
                minSize.y = m_ResizeTarget.style.minHeight.value;
            }

            return minSize;
        }

        float GetMaxHorizontalExpansion(bool expandingLeft)
        {
            float maxHorizontalExpansion;

            if (expandingLeft)
            {
                maxHorizontalExpansion = m_ResizeBeginLayout.x;
            }
            else
            {
                maxHorizontalExpansion = m_ResizeTarget.parent.layout.width - m_ResizeBeginLayout.xMax;
            }

            if (maintainAspectRatio)
            {
                if (!m_DockingTop)
                {
                    maxHorizontalExpansion = Mathf.Min(maxHorizontalExpansion, m_ResizeBeginLayout.y);
                }
                else
                {
                    maxHorizontalExpansion = Mathf.Min(maxHorizontalExpansion, m_ResizeTarget.parent.layout.height - m_ResizeBeginLayout.yMax);
                }
            }

            return maxHorizontalExpansion;
        }

        float GetMaxVerticalExpansion(bool expandingUp)
        {
            float maxVerticalExpansion;

            if (expandingUp)
            {
                maxVerticalExpansion = m_ResizeBeginLayout.y;
            }
            else
            {
                maxVerticalExpansion = m_ResizeTarget.parent.layout.height - m_ResizeBeginLayout.yMax;
            }

            if (maintainAspectRatio)
            {
                if (!m_DockingLeft)
                {
                    maxVerticalExpansion = Mathf.Min(maxVerticalExpansion, m_ResizeBeginLayout.x);
                }
                else
                {
                    maxVerticalExpansion = Mathf.Min(maxVerticalExpansion, m_ResizeTarget.parent.layout.width - m_ResizeBeginLayout.xMax);
                }
            }

            return maxVerticalExpansion;
        }

        void HandleResizeFromTop(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
            {
                return;
            }

            Vector2 restrictedMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseMoveEvent.mousePosition);

            restrictedMousePosition.y = Mathf.Min(restrictedMousePosition.y, m_ResizeBeginLayout.yMax - GetMinSize().y);

            if (stayWithinParentBounds)
            {
                restrictedMousePosition.y = Mathf.Max(restrictedMousePosition.y, m_ResizeBeginMousePosition.y - GetMaxVerticalExpansion(true));
            }

            Vector2 delta = restrictedMousePosition - m_ResizeBeginMousePosition;

            Rect newLayout = m_ResizeBeginLayout;

            newLayout.yMin = m_ResizeBeginLayout.yMin + delta.y;

            if (maintainAspectRatio)
            {
                if (m_DockingLeft)
                {
                    newLayout.width = newLayout.height * m_InitialAspectRatio;
                }
                else
                {
                    newLayout.xMin = newLayout.xMax - (newLayout.height * m_InitialAspectRatio);
                }
            }

            m_ResizeTarget.layout = newLayout;

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromTopRight(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
            {
                return;
            }

            Vector2 restrictedMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseMoveEvent.mousePosition);

            restrictedMousePosition.x = Mathf.Max(restrictedMousePosition.x, m_ResizeBeginLayout.xMin + GetMinSize().x);
            restrictedMousePosition.y = Mathf.Min(restrictedMousePosition.y, m_ResizeBeginLayout.yMax - GetMinSize().y);

            if (stayWithinParentBounds)
            {
                restrictedMousePosition.x = Mathf.Min(restrictedMousePosition.x, m_ResizeBeginMousePosition.x + GetMaxHorizontalExpansion(false));
                restrictedMousePosition.y = Mathf.Max(restrictedMousePosition.y, 0f);
            }

            Vector2 delta = restrictedMousePosition - m_ResizeBeginMousePosition;

            Rect newLayout = m_ResizeBeginLayout;

            newLayout.width += delta.x;
            newLayout.yMin += delta.y;

            if (maintainAspectRatio)
            {
                if (newLayout.width < newLayout.height * m_InitialAspectRatio)
                {
                    newLayout.yMin = Mathf.Min(newLayout.yMax - newLayout.width / m_InitialAspectRatio, newLayout.yMax - GetMinSize().y);
                }
                else
                {
                    newLayout.width = newLayout.height * m_InitialAspectRatio;
                }
            }

            m_ResizeTarget.layout = newLayout;

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromRight(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
            {
                return;
            }

            Vector2 restrictedMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseMoveEvent.mousePosition);

            restrictedMousePosition.x = Mathf.Max(restrictedMousePosition.x, m_ResizeBeginLayout.xMin + GetMinSize().x);

            if (stayWithinParentBounds)
            {
                restrictedMousePosition.x = Mathf.Min(restrictedMousePosition.x, m_ResizeBeginMousePosition.x + GetMaxHorizontalExpansion(false));
            }

            Vector2 delta = restrictedMousePosition - m_ResizeBeginMousePosition;

            Rect newLayout = m_ResizeBeginLayout;

            newLayout.xMax = m_ResizeBeginLayout.xMax + delta.x;

            if (maintainAspectRatio)
            {
                if (m_DockingTop)
                {
                    newLayout.height = newLayout.width / m_InitialAspectRatio;
                }
                else
                {
                    newLayout.yMin = newLayout.yMax - (newLayout.width / m_InitialAspectRatio);
                }
            }

            m_ResizeTarget.layout = newLayout;

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromBottomRight(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
            {
                return;
            }

            Vector2 restrictedMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseMoveEvent.mousePosition);

            restrictedMousePosition.x = Mathf.Max(restrictedMousePosition.x, m_ResizeBeginLayout.xMin + GetMinSize().x);
            restrictedMousePosition.y = Mathf.Max(restrictedMousePosition.y, m_ResizeBeginLayout.yMin + GetMinSize().y);

            if (stayWithinParentBounds)
            {
                restrictedMousePosition.x = Mathf.Min(restrictedMousePosition.x, m_ResizeBeginMousePosition.x + GetMaxHorizontalExpansion(false));
                restrictedMousePosition.y = Mathf.Min(restrictedMousePosition.y, m_ResizeBeginMousePosition.y + GetMaxVerticalExpansion(false));
            }

            Vector2 delta = restrictedMousePosition - m_ResizeBeginMousePosition;

            Rect newLayout = m_ResizeBeginLayout;

            newLayout.size += delta;

            if (maintainAspectRatio)
            {
                if (newLayout.width < newLayout.height * m_InitialAspectRatio)
                {
                    newLayout.height = newLayout.width / m_InitialAspectRatio;
                }
                else
                {
                    newLayout.width = newLayout.height * m_InitialAspectRatio;
                }
            }

            m_ResizeTarget.layout = newLayout;

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromBottom(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
            {
                return;
            }

            Vector2 restrictedMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseMoveEvent.mousePosition);

            restrictedMousePosition.y = Mathf.Max(restrictedMousePosition.y, m_ResizeBeginLayout.yMin + GetMinSize().y);

            if (stayWithinParentBounds)
            {
                restrictedMousePosition.y = Mathf.Min(restrictedMousePosition.y, m_ResizeBeginMousePosition.y + GetMaxVerticalExpansion(false));
            }

            Vector2 delta = restrictedMousePosition - m_ResizeBeginMousePosition;

            Rect newLayout = m_ResizeBeginLayout;

            newLayout.yMax = m_ResizeBeginLayout.yMax + delta.y;

            if (maintainAspectRatio)
            {
                if (m_DockingLeft)
                {
                    newLayout.width = newLayout.height * m_InitialAspectRatio;
                }
                else
                {
                    newLayout.xMin = newLayout.xMax - (newLayout.height * m_InitialAspectRatio);
                }
            }

            m_ResizeTarget.layout = newLayout;

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromBottomLeft(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
            {
                return;
            }

            Vector2 restrictedMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseMoveEvent.mousePosition);

            restrictedMousePosition.x = Mathf.Min(restrictedMousePosition.x, m_ResizeBeginLayout.xMax - GetMinSize().x);
            restrictedMousePosition.y = Mathf.Max(restrictedMousePosition.y, m_ResizeBeginLayout.yMin + GetMinSize().y);

            if (stayWithinParentBounds)
            {
                restrictedMousePosition.x = Mathf.Max(restrictedMousePosition.x, 0f);
                restrictedMousePosition.y = Mathf.Min(restrictedMousePosition.y, m_ResizeBeginMousePosition.y + GetMaxVerticalExpansion(false));
            }

            Vector2 delta = restrictedMousePosition - m_ResizeBeginMousePosition;

            Rect newLayout = m_ResizeBeginLayout;

            newLayout.xMin += delta.x;
            newLayout.height += delta.y;

            if (maintainAspectRatio)
            {
                if (newLayout.width < newLayout.height * m_InitialAspectRatio)
                {
                    newLayout.height = newLayout.width / m_InitialAspectRatio;
                }
                else
                {
                    newLayout.xMin = Mathf.Min(newLayout.xMax - newLayout.height * m_InitialAspectRatio, newLayout.xMax - GetMinSize().x);
                }
            }

            m_ResizeTarget.layout = newLayout;

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromLeft(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
            {
                return;
            }

            Vector2 restrictedMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseMoveEvent.mousePosition);

            restrictedMousePosition.x = Mathf.Min(restrictedMousePosition.x, m_ResizeBeginLayout.xMax - GetMinSize().x);

            if (stayWithinParentBounds)
            {
                restrictedMousePosition.x = Mathf.Max(restrictedMousePosition.x, m_ResizeBeginMousePosition.x - GetMaxHorizontalExpansion(true));
            }

            Vector2 delta = restrictedMousePosition - m_ResizeBeginMousePosition;

            Rect newLayout = m_ResizeBeginLayout;

            newLayout.xMin = m_ResizeBeginLayout.xMin + delta.x;

            if (maintainAspectRatio)
            {
                if (m_DockingTop)
                {
                    newLayout.height = newLayout.width / m_InitialAspectRatio;
                }
                else
                {
                    newLayout.yMin = newLayout.yMax - (newLayout.width / m_InitialAspectRatio);
                }
            }

            m_ResizeTarget.layout = newLayout;

            mouseMoveEvent.StopPropagation();
        }

        void HandleResizeFromTopLeft(MouseMoveEvent mouseMoveEvent)
        {
            if (!m_Dragging)
            {
                return;
            }

            Vector2 restrictedMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseMoveEvent.mousePosition);

            restrictedMousePosition.x = Mathf.Min(restrictedMousePosition.x, m_ResizeBeginLayout.xMax - GetMinSize().x);
            restrictedMousePosition.y = Mathf.Min(restrictedMousePosition.y, m_ResizeBeginLayout.yMax - GetMinSize().y);

            if (stayWithinParentBounds)
            {
                restrictedMousePosition.x = Mathf.Max(restrictedMousePosition.x, 0f);
                restrictedMousePosition.y = Mathf.Max(restrictedMousePosition.y, 0f);
            }

            Vector2 delta = restrictedMousePosition - m_ResizeBeginMousePosition;

            Rect newLayout = m_ResizeBeginLayout;

            newLayout.xMin += delta.x;
            newLayout.yMin += delta.y;

            if (maintainAspectRatio)
            {
                if (newLayout.width < newLayout.height * m_InitialAspectRatio)
                {
                    newLayout.yMin = Mathf.Min(newLayout.yMax - newLayout.width / m_InitialAspectRatio, newLayout.yMax - GetMinSize().y);
                }
                else
                {
                    newLayout.xMin = Mathf.Min(newLayout.xMax - newLayout.height * m_InitialAspectRatio, newLayout.xMax - GetMinSize().x);
                }
            }

            m_ResizeTarget.layout = newLayout;

            mouseMoveEvent.StopPropagation();
        }

        void HandleMouseDown(MouseDownEvent mouseDownEvent)
        {
            m_Dragging = true;

            m_DockingLeft = m_ResizeTarget.layout.center.x / m_ResizeTarget.parent.layout.width < .5f;
            m_DockingTop = m_ResizeTarget.layout.center.y / m_ResizeTarget.parent.layout.height < .5f;

            m_ResizeBeginLayout = m_ResizeTarget.layout;
            m_ResizeBeginMousePosition = m_ResizeTarget.parent.WorldToLocal(mouseDownEvent.mousePosition);

            m_Dragging = true;
            this.TakeMouseCapture();
            mouseDownEvent.StopPropagation();
        }

        void HandleDraggableMouseUp(MouseUpEvent mouseUpEvent)
        {
            m_Dragging = false;

            if (this.HasMouseCapture())
            {
                this.ReleaseMouseCapture();
            }

            if (OnResizeFinished != null)
            {
                OnResizeFinished();
            }
        }
    }
}
