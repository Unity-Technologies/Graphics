using System.Collections.Generic;
using UnityEngine;

namespace UnityEditorInternal.Experimental
{
    internal class QuadTree<T> where T : IBounds
    {
        private QuadTreeNode<T> m_Root;
        private Rect m_Rectangle;
        private Vector2 m_ScreenSpaceOffset = Vector2.zero;

        public QuadTree()
        {
            Clear();
        }

        public Vector2 screenSpaceOffset
        {
            get { return m_ScreenSpaceOffset; }
            set
            {
                m_ScreenSpaceOffset = value;
            }
        }

        public Rect rectangle
        {
            get { return m_Rectangle; }
        }

        public void Clear()
        {
            SetSize(new Rect(0, 0, 1, 1));
        }

        public void SetSize(Rect rectangle)
        {
            m_Root = null;
            m_Rectangle = rectangle;
            m_Root = new QuadTreeNode<T>(m_Rectangle);
        }

        public int count { get { return m_Root.CountItemsIncludingChildren(); } }

        public void Insert(List<T> items)
        {
            foreach (T i in items)
            {
                Insert(i);
            }
        }

        public void Insert(T item)
        {
            m_Root.Insert(item);
        }

        public void Remove(T item)
        {
            m_Root.Remove(item);
        }

        public List<T> IntersectsWith(Rect area)
        {
            area.x -= m_ScreenSpaceOffset.x;
            area.y -= m_ScreenSpaceOffset.y;
            return m_Root.IntersectsWith(area);
        }

        public List<T> ContainedBy(Rect area)
        {
            area.x -= m_ScreenSpaceOffset.x;
            area.y -= m_ScreenSpaceOffset.y;
            return m_Root.ContainedBy(area);
        }

        public List<T> Elements()
        {
            return m_Root.GetElementsIncludingChildren();
        }

        public void DebugDraw()
        {
            m_Root.DebugDraw(m_ScreenSpaceOffset);
        }
    }
}
