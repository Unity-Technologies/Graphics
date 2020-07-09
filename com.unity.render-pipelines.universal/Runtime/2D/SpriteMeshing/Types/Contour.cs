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
    }
}
