using System;

namespace UnityEngine.Graphing
{
    [Serializable]
    public struct DrawingData
    {
        [SerializeField]
        private bool m_Expanded;
        
        [SerializeField]
        private Rect m_Position;

        [SerializeField]
        private int m_Width;

        public bool expanded
        {
            get { return m_Expanded; }
            set { m_Expanded = value; }
        }

        public Rect position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public int width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }
    }
}
