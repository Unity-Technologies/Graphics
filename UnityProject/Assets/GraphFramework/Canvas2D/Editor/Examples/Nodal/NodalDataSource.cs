using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;

namespace UnityEditor
{
    internal class NodalDataSource : ICanvasDataSource
    {
        List<CanvasElement> m_Elements = new List<CanvasElement>();

        public NodalDataSource()
        {
            m_Elements.Add(new InvisibleBorderContainer(new Vector2(630.0f, 0.0f), 200.0f, true));
            m_Elements.Add(new InvisibleBorderContainer(new Vector2(630.0f, 210.0f), 200.0f, false));
            m_Elements.Add(new Circle(new Vector2(630.0f, 420.0f), 200.0f));
            m_Elements.Add(new Node(Vector2.zero, 200.0f, typeof(Vector3), this));
            m_Elements.Add(new Node(new Vector2(210.0f, 0.0f), 200.0f, typeof(int), this));
            m_Elements.Add(new Node(new Vector2(420.0f, 0.0f), 200.0f, typeof(Color), this));
            m_Elements.Add(new Node(new Vector2(0.0f, 210.0f), 200.0f, typeof(float), this));
            m_Elements.Add(new FloatingBox(new Vector2(210.0f, 210.0f), 200.0f));
        }

        public CanvasElement[] FetchElements()
        {
            return m_Elements.ToArray();
        }

        public void DeleteElement(CanvasElement e)
        {
            m_Elements.Remove(e);
        }

        public void Connect(NodeAnchor a, NodeAnchor b)
        {
            m_Elements.Add(new Edge<NodeAnchor>(this, a, b));
        }
    }
}
