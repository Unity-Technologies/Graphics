using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.Universal
{
    internal class Contour
    {
        public ContourData m_ContourData;
        public Shape m_Shape;
        public bool m_IsOuterEdge;

        public Contour(Shape shape, ContourData contourData, bool isOuterEdge)
        {
            m_Shape = shape;
            m_ContourData = contourData;
            m_IsOuterEdge = isOuterEdge;
        }


        public float CalculateArea()
        {
            // Shoelace area calculation
            List<Vector2> vertices = m_ContourData.m_Vertices;

            float area = 0;
            if (vertices.Count > 2)
            {
                Vector3 prevPoint = vertices[vertices.Count - 1];
                for (int i = 0; i < vertices.Count; i++)
                {
                    Vector3 curPoint = m_ContourData.m_Vertices[i];
                    area += prevPoint.x * curPoint.y - curPoint.x * prevPoint.y;
                    prevPoint = curPoint;
                }
            }
            return area;
        }

    }
}
