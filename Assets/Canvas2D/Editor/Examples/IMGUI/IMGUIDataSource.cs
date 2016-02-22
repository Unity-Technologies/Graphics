using System.Collections.Generic;

namespace UnityEditor.Experimental.Graph.Examples
{
    internal class IMGUIDataSource : ICanvasDataSource
    {
        List<CanvasElement> m_Elements;

        public IMGUIDataSource(List<CanvasElement> m_Data)
        {
            m_Elements = m_Data;
        }

        public CanvasElement[] FetchElements()
        {
            return m_Elements.ToArray();
        }

        public void DeleteElement(CanvasElement e)
        {
            m_Elements.Remove(e);
        }

        public void AddElement(CanvasElement e)
        {
            m_Elements.Add(e);
        }
    }
}
