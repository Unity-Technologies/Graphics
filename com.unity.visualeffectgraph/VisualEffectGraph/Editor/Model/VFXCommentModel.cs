using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEditor.Experimental.VFX
{
    public class VFXCommentModel : VFXElementModel<VFXElementModel,VFXElementModel>
    {

        public override bool CanAddChild(VFXElementModel element, int index = -1)
        {
            return false;
        }

        public void UpdateCollapsed(bool collapsed) {}

        public Vector2 UIPosition
        {
            get { return m_UIPosition; }
            set
            {
                if (value != m_UIPosition)
                {
                    m_UIPosition = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }
        public Vector2 UISize
        {
            get { return m_UISize; }
            set
            {
                if (value != m_UISize)
                {
                    m_UISize = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public string Title
        {
            get { return m_Title; }
            set
            {
                if(m_Title != value)
                {
                    m_Title = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }

            }
        }
        public Color Color
        {
            get { return m_Color; }
            set
            {
                if(m_Color != value)
                {
                    m_Color = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }

            }
        }
        public string Body
        {
            get { return m_Body; }
            set
            {
                if(m_Body != value)
                {
                    m_Body = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }

            }
        }

        private Vector2 m_UIPosition;
        private Vector2 m_UISize;

        private string m_Title;
        private string m_Body;
        private Color m_Color;
    }
}
