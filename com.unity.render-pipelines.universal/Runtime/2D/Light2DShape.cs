using System;
using Unity.Mathematics;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public sealed partial class Light2D
    {
        //------------------------------------------------------------------------------------------
        //                                Variables/Properties
        //------------------------------------------------------------------------------------------
        [SerializeField] int                m_ShapeLightParametricSides         = 5;
        [SerializeField] float              m_ShapeLightParametricAngleOffset   = 0.0f;
        [SerializeField] float              m_ShapeLightParametricRadius        = 1.0f;
        [SerializeField] float              m_ShapeLightFalloffSize             = 0.50f;
        [SerializeField] Vector2            m_ShapeLightFalloffOffset           = Vector2.zero;
        [SerializeField] Vector3[]          m_ShapePath                         = null;

        float   m_PreviousShapeLightFalloffSize             = -1;


        [Obsolete]
        public int              shapeLightParametricSides       => m_ShapeLightParametricSides;
        [Obsolete]
        public float            shapeLightParametricAngleOffset => m_ShapeLightParametricAngleOffset;
        [Obsolete]
        public float            shapeLightParametricRadius      => m_ShapeLightParametricRadius;
        public float            shapeLightFalloffSize           => m_ShapeLightFalloffSize;
        [Obsolete]
        public Vector2          shapeLightFalloffOffset         => m_ShapeLightFalloffOffset;
        public Vector3[]        shapePath                       => m_ShapePath;

        //==========================================================================================
        //                              Functions
        //==========================================================================================

        float CalculateBoundingSphereRadius(Bounds bounds)
        {
            var maxBound = Vector3.Max(bounds.max, bounds.max + (Vector3)m_ShapeLightFalloffOffset);
            var minBound = Vector3.Min(bounds.min, bounds.min + (Vector3)m_ShapeLightFalloffOffset);
            return Vector3.Magnitude(maxBound - minBound) * 0.5f;
        }

        void UpgradeFromParametricLight()
        {
            if ((int)lightType == 0)// parametric light
            {
                // upgrade it to shape light
                var sides = m_ShapeLightParametricSides;
                var radius = m_ShapeLightParametricRadius;

                var radiansPerSide = 2 * Mathf.PI / sides;
                m_ShapePath = new Vector3[sides];
                for (var i = 0; i < sides; i++)
                {
                    var endAngle = (i + 1) * radiansPerSide;
                    m_ShapePath[i] = new Vector3(math.cos(endAngle), math.sin(endAngle), 0) * radius;
                }

                m_LightType = LightType.Freeform;
            }
        }
    }
}
