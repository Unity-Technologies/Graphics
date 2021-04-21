namespace UnityEngine.Experimental.Rendering.Universal
{
    public sealed partial class Light2D
    {
        [SerializeField] int                m_ShapeLightParametricSides         = 5;
        [SerializeField] float              m_ShapeLightParametricAngleOffset   = 0.0f;
        [SerializeField] float              m_ShapeLightParametricRadius        = 1.0f;
        [SerializeField] float              m_ShapeLightFalloffSize             = 0.50f;
        [SerializeField] Vector2            m_ShapeLightFalloffOffset           = Vector2.zero;
        [SerializeField] Vector3[]          m_ShapePath                         = null;

        float     m_PreviousShapeLightFalloffSize             = -1;
        int       m_PreviousShapeLightParametricSides         = -1;
        float     m_PreviousShapeLightParametricAngleOffset   = -1;
        float     m_PreviousShapeLightParametricRadius        = -1;
        int       m_PreviousShapePathHash                     = -1;
        LightType m_PreviousLightType                         = LightType.Parametric;

        public int              shapeLightParametricSides       => m_ShapeLightParametricSides;
        public float            shapeLightParametricAngleOffset => m_ShapeLightParametricAngleOffset;
        public float            shapeLightParametricRadius      => m_ShapeLightParametricRadius;
        public float            shapeLightFalloffSize           => m_ShapeLightFalloffSize;

        public Vector3[] shapePath
        {
            get { return m_ShapePath; }
            internal set { m_ShapePath = value; }
        }

        internal void SetShapePath(Vector3[] path)
        {
            m_ShapePath = path;
        }
    }
}
