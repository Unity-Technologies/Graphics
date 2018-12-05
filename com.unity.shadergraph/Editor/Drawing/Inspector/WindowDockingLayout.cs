using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEditor.ShaderGraph.Drawing
{
    [Serializable]
    class WindowDockingLayout
    {
        [SerializeField]
        bool m_DockingLeft;

        public bool dockingLeft
        {
            get { return m_DockingLeft; }
        }

        [SerializeField]
        bool m_DockingTop;

        public bool dockingTop
        {
            get { return m_DockingTop; }
        }

        [SerializeField]
        float m_VerticalOffset;

        public float verticalOffset
        {
            get { return m_VerticalOffset; }
        }

        [SerializeField]
        float m_HorizontalOffset;

        public float horizontalOffset
        {
            get { return m_HorizontalOffset; }
        }

        [SerializeField]
        Vector2 m_Size;

        public Vector2 size
        {
            get { return m_Size; }
        }

        public void CalculateDockingCornerAndOffset(Rect layout, Rect parentLayout)
        {
            Vector2 layoutCenter = new Vector2(layout.x + layout.width * .5f, layout.y + layout.height * .5f);
            layoutCenter /= parentLayout.size;

            m_DockingLeft = layoutCenter.x < .5f;
            m_DockingTop = layoutCenter.y < .5f;

            if (m_DockingLeft)
            {
                m_HorizontalOffset = layout.x;
            }
            else
            {
                m_HorizontalOffset = parentLayout.width - layout.x - layout.width;
            }

            if (m_DockingTop)
            {
                m_VerticalOffset = layout.y;
            }
            else
            {
                m_VerticalOffset = parentLayout.height - layout.y - layout.height;
            }

            m_Size = layout.size;
        }

        public void ClampToParentWindow()
        {
            m_HorizontalOffset = Mathf.Max(0f, m_HorizontalOffset);
            m_VerticalOffset = Mathf.Max(0f, m_VerticalOffset);
        }

        public void ApplyPosition(VisualElement target)
        {
            if (dockingLeft)
            {
                target.style.right = float.NaN;
                target.style.left = horizontalOffset;
            }
            else
            {
                target.style.left = float.NaN;
                target.style.right = horizontalOffset;
            }

            if (dockingTop)
            {
                target.style.bottom = float.NaN;
                target.style.top = verticalOffset;
            }
            else
            {
                target.style.top = float.NaN;
                target.style.bottom = verticalOffset;
            }
        }

        public void ApplySize(VisualElement target)
        {
            target.style.width = size.x;
            target.style.height = size.y;
        }

        public Rect GetLayout(Rect parentLayout)
        {
            Rect layout = new Rect();

            layout.size = size;

            if (dockingLeft)
            {
                layout.x = horizontalOffset;
            }
            else
            {
                layout.x = parentLayout.width - size.x - horizontalOffset;
            }

            if (dockingTop)
            {
                layout.y = verticalOffset;
            }
            else
            {
                layout.y = parentLayout.height - size.y - verticalOffset;
            }

            return layout;
        }
    }
}
