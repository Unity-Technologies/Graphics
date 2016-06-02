using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal.Experimental
{
    internal class QuadTreeNode<T> where T : IBounds
    {
        private Rect m_BoundingRect;
        private static Color m_DebugFillColor = new Color(1.0f, 1.0f, 1.0f, 0.01f);
        private static Color m_DebugWireColor = new Color(1.0f, 0.0f, 0.0f, 0.5f);
        private static Color m_DebugBoxFillColor = new Color(1.0f, 0.0f, 0.0f, 0.01f);
        private const float kSmallestAreaForQuadTreeNode = 10.0f;

        List<T> m_Elements = new List<T>();
        List<QuadTreeNode<T>> m_ChildrenNodes = new List<QuadTreeNode<T>>(4);

        public QuadTreeNode(Rect r)
        {
            m_BoundingRect = r;
        }

        public bool isEmpty { get { return (m_BoundingRect.width == 0 && m_BoundingRect.height == 0) || m_ChildrenNodes.Count == 0; } }
        public Rect boundingRect { get { return m_BoundingRect; } }

        public int CountItemsIncludingChildren()
        {
            return Count(true);
        }

        public int CountLocalItems()
        {
            return Count(false);
        }

        private int Count(bool recursive)
        {
            int count = m_Elements.Count;

            if (recursive)
            {
                foreach (QuadTreeNode<T> node in m_ChildrenNodes)
                    count += node.Count(recursive);
            }
            return count;
        }

        public List<T> GetElementsIncludingChildren()
        {
            return Elements(true);
        }

        public List<T> GetElements()
        {
            return Elements(false);
        }

        private List<T> Elements(bool recursive)
        {
            List<T> results = new List<T>();

            if (recursive)
            {
                foreach (QuadTreeNode<T> node in m_ChildrenNodes)
                    results.AddRange(node.Elements(recursive));
            }

            results.AddRange(m_Elements);
            return results;
        }

        public List<T> IntersectsWith(Rect queryArea)
        {
            List<T> results = new List<T>();

            foreach (T item in m_Elements)
            {
                if (RectUtils.Intersects(item.boundingRect, queryArea))
                {
                    results.Add(item);
                }
            }

            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                if (node.isEmpty)
                    continue;

                if (RectUtils.Intersects(node.boundingRect, queryArea))
                {
                    // the node completely contains the queryArea
                    // recurse down and stop
                    results.AddRange(node.IntersectsWith(queryArea));
                    break;
                }
            }

            return results;
        }

        public List<T> ContainedBy(Rect queryArea)
        {
            List<T> results = new List<T>();

            foreach (T item in m_Elements)
            {
                if (RectUtils.Contains(item.boundingRect, queryArea))
                {
                    results.Add(item);
                }
                else if (queryArea.Overlaps(item.boundingRect))
                {
                    results.Add(item);
                }
            }

            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                if (node.isEmpty)
                    continue;

                if (RectUtils.Contains(node.boundingRect, queryArea))
                {
                    // the node completely contains the queryArea
                    // recurse down and stop
                    results.AddRange(node.ContainedBy(queryArea));
                    break;
                }

                if (RectUtils.Contains(queryArea, node.boundingRect))
                {
                    // the queryArea completely contains this node
                    // just add everything under this node, recursively
                    results.AddRange(node.Elements(true));
                    continue;
                }

                if (node.boundingRect.Overlaps(queryArea))
                {
                    // the node intesects
                    // recurse and continue iterating siblings
                    results.AddRange(node.ContainedBy(queryArea));
                }
            }

            return results;
        }

        public void Remove(T item)
        {
            m_Elements.Remove(item);
            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                node.Remove(item);
            }
        }

        public void Insert(T item)
        {
            if (!RectUtils.Contains(m_BoundingRect, item.boundingRect))
            {
                Rect intersection = new Rect();
                if (!RectUtils.Intersection(item.boundingRect, m_BoundingRect, out intersection))
                {
                    // Ignore elements completely outside the quad tree
                    return;
                }
            }

            if (m_ChildrenNodes.Count == 0)
                Subdivide();

            // insert into children nodes
            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                if (RectUtils.Contains(node.boundingRect, item.boundingRect))
                {
                    node.Insert(item);
                    return;
                }
            }

            // item is not completely contained in any of the children nodes
            // insert here
            m_Elements.Add(item);
        }

        private void Subdivide()
        {
            if ((m_BoundingRect.height * m_BoundingRect.width) <= kSmallestAreaForQuadTreeNode)
                return;

            float halfWidth = (m_BoundingRect.width / 2f);
            float halfHeight = (m_BoundingRect.height / 2f);

            m_ChildrenNodes.Add(new QuadTreeNode<T>(new Rect(m_BoundingRect.position.x, m_BoundingRect.position.y, halfWidth, halfHeight)));
            m_ChildrenNodes.Add(new QuadTreeNode<T>(new Rect(m_BoundingRect.xMin, m_BoundingRect.yMin + halfHeight, halfWidth, halfHeight)));
            m_ChildrenNodes.Add(new QuadTreeNode<T>(new Rect(m_BoundingRect.xMin + halfWidth, m_BoundingRect.yMin, halfWidth, halfHeight)));
            m_ChildrenNodes.Add(new QuadTreeNode<T>(new Rect(m_BoundingRect.xMin + halfWidth, m_BoundingRect.yMin + halfHeight, halfWidth, halfHeight)));
        }

        public void DebugDraw(Vector2 offset)
        {
            UnityEditor.Experimental.UIHelpers.ApplyWireMaterial();
            Rect screenSpaceRect = m_BoundingRect;
            screenSpaceRect.x += offset.x;
            screenSpaceRect.y += offset.y;

            Handles.DrawSolidRectangleWithOutline(screenSpaceRect, m_DebugFillColor, m_DebugWireColor);
            foreach (QuadTreeNode<T> node in m_ChildrenNodes)
            {
                node.DebugDraw(offset);
            }

            foreach (IBounds i in Elements(false))
            {
                Rect o = i.boundingRect;
                o.x += offset.x;
                o.y += offset.y;
                Handles.DrawSolidRectangleWithOutline(o, m_DebugBoxFillColor, Color.yellow);
            }
        }
    };
}
