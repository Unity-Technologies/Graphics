using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Experimental.Graph.Examples
{
    internal class SimpleDataSource : ICanvasDataSource
    {
        List<CanvasElement> m_Elements = new List<CanvasElement>();

        public SimpleDataSource()
        {
            m_Elements.Add(new SimpleBox(Vector2.zero, 200.0f));
            m_Elements.Add(new MoveableBox(new Vector2(400.0f, 400.0f), 200.0f));
            m_Elements.Add(new ResizableBox(new Vector2(400.0f, 200.0f), 100.0f));
            m_Elements.Add(new WWWImageBox(new Vector2(300.0f, 300.0f), 200.0f));
            m_Elements.Add(new IMGUIControls(new Vector2(100.0f, 200.0f), 100.0f));
        }

        public CanvasElement[] FetchElements()
        {
            return m_Elements.ToArray();
        }

        public void DeleteElement(CanvasElement e)
        {
            m_Elements.Remove(e);
        }
    }
}
