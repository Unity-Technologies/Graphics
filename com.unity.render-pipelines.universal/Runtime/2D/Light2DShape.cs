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

        public int              shapeLightParametricSides       => m_ShapeLightParametricSides;
        public float            shapeLightParametricAngleOffset => m_ShapeLightParametricAngleOffset;
        public float            shapeLightParametricRadius      => m_ShapeLightParametricRadius;
        public float            shapeLightFalloffSize           => m_ShapeLightFalloffSize;
        public Vector3[]        shapePath                       => m_ShapePath;
    }
}
