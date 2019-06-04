namespace UnityEngine.Experimental.Rendering.LWRP
{
    sealed public partial class Light2D : MonoBehaviour
    {
        //------------------------------------------------------------------------------------------
        //                                Variables/Properties
        //------------------------------------------------------------------------------------------
        [SerializeField] int                m_ShapeLightParametricSides         = 5;
        [SerializeField] float              m_ShapeLightParametricAngleOffset   = 0.0f;
        [SerializeField] float              m_ShapeLightParametricRadius        = 1.0f;
        [SerializeField] float              m_ShapeLightFalloffSize             = 0.50f;
        [SerializeField] Vector2            m_ShapeLightFalloffOffset           = Vector2.zero;
        [SerializeField] Vector3[]          m_ShapePath;

        int     m_PreviousShapeLightParametricSides         = -1;
        float   m_PreviousShapeLightParametricAngleOffset   = -1;
        float   m_PreviousShapeLightParametricRadius        = -1;
        Vector2 m_PreviousShapeLightFalloffOffset           = Vector2.negativeInfinity;


#if UNITY_EDITOR
        int     m_PreviousShapePathHash                     = -1;
#endif

        public int              shapeLightParametricSides       => m_ShapeLightParametricSides;
        public float            shapeLightParametricAngleOffset => m_ShapeLightParametricAngleOffset;
        public float            shapeLightParametricRadius      => m_ShapeLightParametricRadius;
        public float            shapeLightFalloffSize           => m_ShapeLightFalloffSize;
        public Vector2          shapeLightFalloffOffset         => m_ShapeLightFalloffOffset;
        public Vector3[]        shapePath                       => m_ShapePath;

        //==========================================================================================
        //                              Functions
        //==========================================================================================

        internal bool IsShapeLight()
        {
            return m_LightType != LightType.Point;
        }

        BoundingSphere GetShapeLightBoundingSphere()
        {
            BoundingSphere boundingSphere;

            Vector3 maximum = transform.TransformPoint(m_LocalBounds.max);
            Vector3 minimum = transform.TransformPoint(m_LocalBounds.min);
            Vector3 center = 0.5f * (maximum + minimum);
            float radius = Vector3.Magnitude(maximum - center);

            boundingSphere.radius = radius;
            boundingSphere.position = center;

            return boundingSphere;
        }

#if UNITY_EDITOR
        int GetShapePathHash()
        {
            unchecked
            {
                int hashCode = (int)2166136261;

                if (m_ShapePath != null)
                {
                    foreach (var point in m_ShapePath)
                        hashCode = hashCode * 16777619 ^ point.GetHashCode();
                }
                else
                {
                    hashCode = 0;
                }

                return hashCode;
            }
        }
#endif
    }
}
