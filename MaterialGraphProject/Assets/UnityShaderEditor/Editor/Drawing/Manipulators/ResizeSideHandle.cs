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
                    AddToClassList("vertical");
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
