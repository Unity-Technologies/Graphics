using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class ShapeLibrary
    {
        public List<Shape> m_Shapes = new List<Shape>();
        public RectInt m_Region;
        public LineIntersectionManager m_LineIntersectionManager;
        public Dictionary<int, ContourData> m_ContourData = new Dictionary<int, ContourData>();

        public void Clear()
        {
            m_Shapes.Clear();
            m_ContourData.Clear();
            m_LineIntersectionManager = new LineIntersectionManager(m_Region.width, m_Region.height);
        }

        public void SetRegion(RectInt region)
        {
            m_Region = region;
            m_LineIntersectionManager = new LineIntersectionManager(region.width, region.height);
        }
    }
}
