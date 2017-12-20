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
        Right,
        Bottom,
        Left
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

            bool moveWhileResize = anchor == ResizeHandleAnchor.Top || anchor == ResizeHandleAnchor.Left;

            if (anchor == ResizeHandleAnchor.Left || anchor == ResizeHandleAnchor.Right)
            {
                resizeDirection = ResizeDirection.Horizontal;
            }
            else
            {
                resizeDirection = ResizeDirection.Vertical;
            }

            this.AddManipulator(new Draggable(mouseDelta => OnResize(mouseDelta, resizeDirection, moveWhileResize)));
        }

        void OnResize(Vector2 resizeDelta, ResizeDirection direction, bool moveWhileResize)
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
            if (!moveWhileResize)
            {
                newLayout.width = Mathf.Max(m_ResizeTarget.layout.width + normalizedResizeDelta.x, 60f);
                newLayout.height = Mathf.Max(m_ResizeTarget.layout.height + normalizedResizeDelta.y, 60f);

                m_ResizeTarget.layout = newLayout;

                return;
            }

            float previousFarX = m_ResizeTarget.layout.x + m_ResizeTarget.layout.width;
            float previousFarY = m_ResizeTarget.layout.y + m_ResizeTarget.layout.height;

            newLayout.width = Mathf.Max(m_ResizeTarget.layout.width - normalizedResizeDelta.x, 60f);
            newLayout.height = Mathf.Max(m_ResizeTarget.layout.height - normalizedResizeDelta.y, 60f);

            newLayout.x = Mathf.Min(m_ResizeTarget.layout.x + normalizedResizeDelta.x, previousFarX - 60f);
            newLayout.y = Mathf.Min(m_ResizeTarget.layout.y + normalizedResizeDelta.y, previousFarY - 60f);

            m_ResizeTarget.layout = newLayout;
        }
    }
}
