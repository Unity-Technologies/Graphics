using System;
using System.Collections.Generic;
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
                    break;
                }
                case ResizeHandleAnchor.TopRight:
                {
                    AddToClassList("diagonal");
                    AddToClassList("top-right");
                    break;
                }
                case ResizeHandleAnchor.Right:
                {
                    AddToClassList("horizontal");
                    AddToClassList("right");
                    break;
                }
                case ResizeHandleAnchor.BottomRight:
                {
                    AddToClassList("diagonal");
                    AddToClassList("bottom-right");
                    break;
                }
                case ResizeHandleAnchor.Bottom:
                {
                    AddToClassList("vertical");
                    AddToClassList("bottom");
                    break;
                }
                case ResizeHandleAnchor.BottomLeft:
                {
                    AddToClassList("diagonal");
                    AddToClassList("bottom-left");
                    break;
                }
                case ResizeHandleAnchor.Left:
                {
                    AddToClassList("horizontal");
                    AddToClassList("left");
                    break;
                }
                case ResizeHandleAnchor.TopLeft:
                {
                    AddToClassList("diagonal");
                    AddToClassList("top-left");
                    break;
                }
            }

            ResizeDirection resizeDirection;

            if (anchor == ResizeHandleAnchor.Left || anchor == ResizeHandleAnchor.Right)
            {
                resizeDirection = ResizeDirection.Horizontal;
            }
            else if (anchor == ResizeHandleAnchor.Top || anchor == ResizeHandleAnchor.Bottom)
            {
                resizeDirection = ResizeDirection.Vertical;
            }
            else
            {
                resizeDirection = ResizeDirection.Any;
            }

            bool moveWhileResizeHorizontal = anchor == ResizeHandleAnchor.TopLeft || anchor == ResizeHandleAnchor.BottomLeft || anchor == ResizeHandleAnchor.Left;
            bool moveWhileResizeVertical = anchor == ResizeHandleAnchor.TopLeft || anchor == ResizeHandleAnchor.TopRight || anchor == ResizeHandleAnchor.Top;

            this.AddManipulator(new Draggable(mouseDelta => OnResize(mouseDelta, resizeDirection, moveWhileResizeHorizontal, moveWhileResizeVertical, anchor)));
            RegisterCallback<MouseUpEvent>(HandleDraggableMouseUp);
        }

        void OnResize(Vector2 resizeDelta, ResizeDirection direction, bool moveWhileResizeHorizontal, bool moveWhileresizeVertical, ResizeHandleAnchor anchor)
        {
            Vector2 normalizedResizeDelta = resizeDelta / 2f;

            Vector2 minSize = new Vector2(60f, 60f);

            if (!Mathf.Approximately(m_ResizeTarget.style.minWidth.value, 0f))
            {
                minSize.x = m_ResizeTarget.style.minWidth;
            }

            if (!Mathf.Approximately(m_ResizeTarget.style.minHeight.value, 0f))
            {
                minSize.y = m_ResizeTarget.style.minHeight.value;
            }

            if (direction == ResizeDirection.Vertical)
            {
                normalizedResizeDelta.x = 0f;
            }
            else if (direction == ResizeDirection.Horizontal)
            {
                normalizedResizeDelta.y = 0f;
            }

            Rect newLayout = m_ResizeTarget.layout;

            if (anchor == ResizeHandleAnchor.Left || anchor == ResizeHandleAnchor.TopLeft || anchor == ResizeHandleAnchor.BottomLeft)
            {
                newLayout.xMin = Mathf.Min(newLayout.xMin + normalizedResizeDelta.x, newLayout.xMax - minSize.x);
            }
            else
            {
                newLayout.xMax = Mathf.Max(newLayout.xMax + normalizedResizeDelta.x, newLayout.xMin + minSize.x);
            }

            if (anchor == ResizeHandleAnchor.Top || anchor == ResizeHandleAnchor.TopLeft || anchor == ResizeHandleAnchor.TopRight)
            {
                newLayout.yMin = Mathf.Min(newLayout.yMin + normalizedResizeDelta.y, newLayout.yMax - minSize.y);
            }
            else
            {
                newLayout.yMax = Mathf.Max(newLayout.yMax + normalizedResizeDelta.y, newLayout.yMin + minSize.y);
            }

            if (stayWithinParentBounds)
            {
                newLayout.xMin = Mathf.Max(newLayout.xMin, 0f);
                newLayout.yMin = Mathf.Max(newLayout.yMin, 0f);
                newLayout.xMax = Mathf.Min(newLayout.xMax, m_ResizeTarget.parent.layout.width);
                newLayout.yMax = Mathf.Min(newLayout.yMax, m_ResizeTarget.parent.layout.height);
            }

            m_ResizeTarget.layout = newLayout;
        }

        void HandleDraggableMouseUp(MouseUpEvent evt)
        {
            if (OnResizeFinished != null)
            {
                OnResizeFinished();
            }
        }
    }
}
