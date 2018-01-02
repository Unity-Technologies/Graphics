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

        ResizeHandleAnchor m_HandleAnchor;

        public ResizeSideHandle(VisualElement resizeTarget, ResizeHandleAnchor anchor, string[] styleClasses)
        {
            m_ResizeTarget = resizeTarget;

            foreach (string styleClass in styleClasses)
            {
                AddToClassList(styleClass);
            }

            m_HandleAnchor = anchor;

            ResizeDirection resizeDirection;

            bool moveWhileResizeHorizontal = anchor == ResizeHandleAnchor.TopLeft || anchor == ResizeHandleAnchor.BottomLeft || anchor == ResizeHandleAnchor.Left;
            bool moveWhileResizeVertical = anchor == ResizeHandleAnchor.TopLeft || anchor == ResizeHandleAnchor.TopRight || anchor == ResizeHandleAnchor.Top;

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

            this.AddManipulator(new Draggable(mouseDelta => OnResize(mouseDelta, resizeDirection, moveWhileResizeHorizontal, moveWhileResizeVertical)));
        }

        void OnResize(Vector2 resizeDelta, ResizeDirection direction, bool moveWhileResizeHorizontal, bool moveWhileresizerVertical)
        {
            Vector2 normalizedResizeDelta = resizeDelta / 2f;

            if (direction == ResizeDirection.Vertical)
            {
                normalizedResizeDelta.x = 0f;
            }
            else if (direction == ResizeDirection.Horizontal)
            {
                normalizedResizeDelta.y = 0f;
            }

            Rect newLayout = m_ResizeTarget.layout;

            // Resize form bottom/right
            if (!moveWhileResizeHorizontal)
            {
                newLayout.width = Mathf.Max(newLayout.width + normalizedResizeDelta.x, 60f);
                normalizedResizeDelta.x = 0f;
            }

            if (!moveWhileresizerVertical)
            {
                newLayout.height = Mathf.Max(newLayout.height + normalizedResizeDelta.y, 60f);
                normalizedResizeDelta.y = 0f;
            }

            float previousFarX = m_ResizeTarget.layout.x + m_ResizeTarget.layout.width;
            float previousFarY = m_ResizeTarget.layout.y + m_ResizeTarget.layout.height;

            newLayout.width = Mathf.Max(newLayout.width - normalizedResizeDelta.x, 60f);
            newLayout.height = Mathf.Max(newLayout.height - normalizedResizeDelta.y, 60f);

            newLayout.x = Mathf.Min(newLayout.x + normalizedResizeDelta.x, previousFarX - 60f);
            newLayout.y = Mathf.Min(newLayout.y + normalizedResizeDelta.y, previousFarY - 60f);

            m_ResizeTarget.layout = newLayout;
        }
    }
}
