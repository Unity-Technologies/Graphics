using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental
{
    /// <summary>
    /// CanvasLayout : the base class for vertical and horizontal layouts
    /// WARNING: these layout classes have pretty limited usage.
    /// </summary>
    /// <remarks>
    /// Do not use this class directly. Use on of the specializations or derive your own
    /// </remarks>
    internal class CanvasLayout
    {
        protected CanvasElement m_Owner;
        protected CanvasLayout m_Parent;
        protected Vector4 m_Padding = Vector4.zero;
        protected float m_Left;
        protected float m_Height;
        protected float m_Width;
        protected List<CanvasLayout> m_Children = new List<CanvasLayout>();
        protected List<CanvasElement> m_Elements = new List<CanvasElement>();

        public float height
        {
            get
            {
                float maxHeight = m_Height;
                foreach (CanvasLayout l in m_Children)
                {
                    maxHeight = Mathf.Max(maxHeight, l.height);
                }
                return maxHeight + paddingTop + paddingBottom;
            }
        }

        public float width
        {
            get
            {
                float maxWidth = m_Width;
                foreach (CanvasLayout l in m_Children)
                {
                    maxWidth = Mathf.Max(maxWidth, l.width);
                }
                return maxWidth + paddingLeft + paddingRight;
            }
        }

        public CanvasLayout(CanvasElement e)
        {
            m_Owner = e;
        }

        public CanvasLayout(CanvasLayout p)
        {
            m_Parent = p;
            m_Owner = p.Owner();
        }

        public CanvasElement Owner()
        {
            if (m_Owner != null)
                return m_Owner;

            if (m_Parent != null)
                return m_Parent.Owner();

            return null;
        }

        public float left
        {
            get { return m_Left; }
            set { m_Left = value; }
        }

        public float paddingLeft
        {
            get { return m_Padding.w; }
            set { m_Padding.w = value; }
        }

        public float paddingRight
        {
            get { return m_Padding.y; }
            set { m_Padding.y = value; }
        }

        public float paddingTop
        {
            get { return m_Padding.x; }
            set { m_Padding.x = value; }
        }

        public float paddingBottom
        {
            get { return m_Padding.z; }
            set { m_Padding.z = value; }
        }

        public virtual void LayoutElement(CanvasElement c)
        {
            m_Elements.Add(c);
        }

        public virtual void LayoutElements(CanvasElement[] arrayOfElements)
        {
            for (int a = 0; a < arrayOfElements.Length; a++)
            {
                m_Elements.Add(arrayOfElements[a]);
            }
        }

        public void AddSpacer(int pixels)
        {
            float collapsedFactor = m_Owner.IsCollapsed() ? 0.0f : 1.0f;
            m_Height += pixels * collapsedFactor;
        }

        public virtual void DebugDraw()
        {
        }
    };
}
