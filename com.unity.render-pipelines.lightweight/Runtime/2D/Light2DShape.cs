using UnityEngine.U2D.Shape;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    sealed public partial class Light2D : MonoBehaviour
    {
        //------------------------------------------------------------------------------------------
        //                                      Static
        //------------------------------------------------------------------------------------------

        static Material s_ShapeCookieSpriteAlphaBlendMaterial;
        static Material s_ShapeCookieSpriteAdditiveMaterial;
        static Material s_ShapeCookieSpriteVolumeMaterial;
        static Material s_ShapeVertexColoredAlphaBlendMaterial;
        static Material s_ShapeVertexColoredAdditiveMaterial;
        static Material s_ShapeVertexColoredVolumeMaterial;

        //------------------------------------------------------------------------------------------
        //                                Variables/Properties
        //------------------------------------------------------------------------------------------
        [SerializeField]
        [Serialization.FormerlySerializedAs("m_ParametricSides")]
        int m_ShapeLightParametricSides = 6;

        [SerializeField] float              m_ShapeLightParametricAngleOffset   = 0.0f;
        [SerializeField] float              m_ShapeLightFalloffSize             = 0.50f;
        [SerializeField] float              m_ShapeLightRadius                  = 1.0f;
        [SerializeField] Vector2            m_ShapeLightFalloffOffset           = Vector2.zero;
        [SerializeField] int                m_ShapeLightOrder                   = 0;
        [SerializeField] LightOverlapMode   m_ShapeLightOverlapMode             = LightOverlapMode.Additive;
        [SerializeField] Vector3[]          m_ShapePath;

        // TODO: Remove this.
        [SerializeField] Spline m_Spline = new Spline() { isExtensionsSupported = false };

        int     m_PreviousShapeLightParametricSides         = -1;
        float   m_PreviousShapeLightParametricAngleOffset   = -1;
        float   m_PreviousShapeLightFalloffSize             = -1;
        float   m_PreviousShapeLightRadius                  = -1;
        Vector2 m_PreviousShapeLightOffset                  = Vector2.negativeInfinity;
        int     m_PreviousShapeLightOrder                   = -1;

        public int          shapeLightParametricSides       => m_ShapeLightParametricSides;
        public float        shapeLightParametricAngleOffset => m_ShapeLightParametricAngleOffset;
        public float        shapeLightFalloffSize           => m_ShapeLightFalloffSize;
        public float        shapeLightRadius                => m_ShapeLightRadius;
        public Vector2      shapeLightFalloffOffset         => m_ShapeLightFalloffOffset;
        public Vector3[]    shapePath                       => m_ShapePath;

        //==========================================================================================
        //                              Functions
        //==========================================================================================

        internal static bool IsShapeLight(LightType lightType)
        {
            return lightType != LightType.Point && lightType != LightType.Global;
        }

        Material GetShapeLightVolumeMaterial()
        {
            if (m_LightType == LightType.Sprite)
            {
                // This is causing Object.op_inequality fix this
                if (s_ShapeCookieSpriteVolumeMaterial == null && m_LightCookieSprite && m_LightCookieSprite.texture != null)
                {
                    Shader shader = Shader.Find("Hidden/Light2d-Sprite-Volumetric");
                    if (shader != null)
                    {
                        s_ShapeCookieSpriteVolumeMaterial = new Material(shader);
                        s_ShapeCookieSpriteVolumeMaterial.SetTexture("_MainTex", m_LightCookieSprite.texture);
                    }
                    else
                        Debug.LogError("Missing shader Light2d-Sprite-Volumetric");
                }

                return s_ShapeCookieSpriteVolumeMaterial;
            }
            else
            {
                // This is causing Object.op_inequality fix this
                if (s_ShapeVertexColoredVolumeMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Light2d-Shape-Volumetric");
                    if (shader != null)
                        s_ShapeVertexColoredVolumeMaterial = new Material(shader);
                    else
                        Debug.LogError("Missing shader Light2d-Shape-Volumetric");
                }

                return s_ShapeVertexColoredVolumeMaterial;
            }
        }

        Material GetShapeLightMaterial()
        {
            if (m_LightType == LightType.Sprite)
            {
                // This is causing Object.op_inequality fix this
                if (s_ShapeCookieSpriteAdditiveMaterial == null && m_LightCookieSprite && m_LightCookieSprite.texture != null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Sprite-Additive");

                    if (shader != null)
                    {
                        s_ShapeCookieSpriteAdditiveMaterial = new Material(shader);
                        s_ShapeCookieSpriteAdditiveMaterial.SetTexture("_MainTex", m_LightCookieSprite.texture);
                    }
                    else
                        Debug.LogError("Missing shader Light2d-Sprite-Additive");
                }

                if (s_ShapeCookieSpriteAlphaBlendMaterial == null && m_LightCookieSprite && m_LightCookieSprite.texture != null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Sprite-Superimpose"); ;

                    if (shader != null)
                    {
                        s_ShapeCookieSpriteAlphaBlendMaterial = new Material(shader);
                        s_ShapeCookieSpriteAlphaBlendMaterial.SetTexture("_MainTex", m_LightCookieSprite.texture);
                    }
                    else
                        Debug.LogError("Missing shader Light2d-Sprite-Superimpose");
                }


                if (m_ShapeLightOverlapMode == LightOverlapMode.Additive)
                    return s_ShapeCookieSpriteAdditiveMaterial;
                else
                    return s_ShapeCookieSpriteAlphaBlendMaterial;
            }
            else
            {
                // This is causing Object.op_inequality fix this
                if (s_ShapeVertexColoredAdditiveMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Shape-Additive"); ;
                    if (shader != null)
                        s_ShapeVertexColoredAdditiveMaterial = new Material(shader);
                    else
                        Debug.LogError("Missing shader Light2d-Shape-Additive");
                }

                if (s_ShapeVertexColoredAlphaBlendMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Shape-Superimpose"); ;
                    if (shader != null)
                        s_ShapeVertexColoredAlphaBlendMaterial = new Material(shader);
                    else
                        Debug.LogError("Missing shader Light2d-Shape-Superimpose");
                }

                if (m_ShapeLightOverlapMode == LightOverlapMode.Additive)
                    return s_ShapeVertexColoredAdditiveMaterial;
                else
                    return s_ShapeVertexColoredAlphaBlendMaterial;
            }
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

        int m_PrevShapePathHash;
#endif
    }
}
