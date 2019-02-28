using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Shape;
using Unity.RenderPipeline2D.External.LibTessDotNet;
using Mesh = UnityEngine.Mesh;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering.LWRP
{
    sealed public partial class Light2D : MonoBehaviour
    {
        //------------------------------------------------------------------------------------------
        //                                      Static
        //------------------------------------------------------------------------------------------

        static Material m_ShapeCookieSpriteAlphaBlendMaterial = null;
        static Material m_ShapeCookieSpriteAdditiveMaterial = null;
        static Material m_ShapeCookieSpriteVolumeMaterial = null;
        static Material m_ShapeVertexColoredAlphaBlendMaterial = null;
        static Material m_ShapeVertexColoredAdditiveMaterial = null;
        static Material m_ShapeVertexColoredVolumeMaterial = null;

        //------------------------------------------------------------------------------------------
        //                                Variables/Properties
        //------------------------------------------------------------------------------------------
        public float shapeLightFalloffSize
        {
            get { return m_ShapeLightFalloffSize; }
            set { m_ShapeLightFalloffSize = value; }
        }

        [SerializeField]
        private float m_ShapeLightFalloffSize = 0.50f;
        private float m_PreviousShapeLightFalloffSize = -1;

        public int shapeLightParametricSides
        {
            get { return m_ShapeLightParametricSides; }
            set { m_ShapeLightParametricSides = value; }
        }
        [SerializeField]
        [Serialization.FormerlySerializedAs("m_ParametricSides")]
        private int m_ShapeLightParametricSides = 6;
        private int m_PreviousShapeLightParametricSides = -1;

        public float shapeLightParametricAngleOffset
        {
            get { return m_ShapeLightParametricAngleOffset; }
            set { m_ShapeLightParametricAngleOffset = value; }
        }
        [SerializeField]
        private float m_ShapeLightParametricAngleOffset = 0;
        private float m_PreviousShapeLightParametricAngleOffset = -1;

        public float shapeLightRadius
        {
            get { return m_ShapeLightRadius; }
            set { m_ShapeLightRadius = value; }
        }
        [SerializeField]
        private float m_ShapeLightRadius;
        private float m_PreviousShapeLightRadius;

        public Vector2 shapeLightOffset
        {
            get { return m_ShapeLightOffset; }
            set { m_ShapeLightOffset = value; }
        }
        [SerializeField]
        private Vector2 m_ShapeLightOffset;
        private Vector2 m_PreviousShapeLightOffset;


        [SerializeField]
        private int m_ShapeLightOrder = 0;
        private int m_PreviousShapeLightOrder = 0;

        [SerializeField]
        private LightOverlapMode m_ShapeLightOverlapMode = LightOverlapMode.Additive;
        //private BlendingModes m_PreviousShapeLightBlending = BlendingModes.Additive;

        [SerializeField]
        private Spline m_Spline = new Spline() { isExtensionsSupported = false };
        private int m_SplineHash;

        [SerializeField]
        Vector3[] m_ShapePath;
        public Vector3[] shapePath => m_ShapePath;


        //==========================================================================================
        //                              Functions
        //==========================================================================================

        internal static bool IsShapeLight(LightType lightType)
        {
            return lightType != LightType.Point;
        }

        Material GetShapeLightVolumeMaterial()
        {
            if (m_LightType == LightType.Sprite)
            {
                // This is causing Object.op_inequality fix this
                if (m_ShapeCookieSpriteVolumeMaterial == null && m_LightCookieSprite && m_LightCookieSprite.texture != null)
                {
                    Shader shader = Shader.Find("Hidden/Light2d-Sprite-Volumetric");
                    if (shader != null)
                    {
                        m_ShapeCookieSpriteVolumeMaterial = new Material(shader);
                        m_ShapeCookieSpriteVolumeMaterial.SetTexture("_MainTex", m_LightCookieSprite.texture);
                    }
                    else
                        Debug.LogError("Missing shader Light2d-Sprite-Volumetric");
                }

                return m_ShapeCookieSpriteVolumeMaterial;
            }
            else
            {
                // This is causing Object.op_inequality fix this
                if (m_ShapeVertexColoredVolumeMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Light2d-Shape-Volumetric");
                    if (shader != null)
                        m_ShapeVertexColoredVolumeMaterial = new Material(shader);
                    else
                        Debug.LogError("Missing shader Light2d-Shape-Volumetric");
                }

                return m_ShapeVertexColoredVolumeMaterial;
            }
        }

        Material GetShapeLightMaterial()
        {
            if (m_LightType == LightType.Sprite)
            {
                // This is causing Object.op_inequality fix this
                if (m_ShapeCookieSpriteAdditiveMaterial == null && m_LightCookieSprite && m_LightCookieSprite.texture != null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Sprite-Additive");

                    if (shader != null)
                    {
                        m_ShapeCookieSpriteAdditiveMaterial = new Material(shader);
                        m_ShapeCookieSpriteAdditiveMaterial.SetTexture("_MainTex", m_LightCookieSprite.texture);
                    }
                    else
                        Debug.LogError("Missing shader Light2d-Sprite-Additive");
                }

                if (m_ShapeCookieSpriteAlphaBlendMaterial == null && m_LightCookieSprite && m_LightCookieSprite.texture != null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Sprite-Superimpose"); ;

                    if (shader != null)
                    {
                        m_ShapeCookieSpriteAlphaBlendMaterial = new Material(shader);
                        m_ShapeCookieSpriteAlphaBlendMaterial.SetTexture("_MainTex", m_LightCookieSprite.texture);
                    }
                    else
                        Debug.LogError("Missing shader Light2d-Sprite-Superimpose");
                }


                if (m_ShapeLightOverlapMode == LightOverlapMode.Additive)
                    return m_ShapeCookieSpriteAdditiveMaterial;
                else
                    return m_ShapeCookieSpriteAlphaBlendMaterial;
            }
            else
            {
                // This is causing Object.op_inequality fix this
                if (m_ShapeVertexColoredAdditiveMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Shape-Additive"); ;
                    if (shader != null)
                        m_ShapeVertexColoredAdditiveMaterial = new Material(shader);
                    else
                        Debug.LogError("Missing shader Light2d-Shape-Additive");
                }

                if (m_ShapeVertexColoredAlphaBlendMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Shape-Superimpose"); ;
                    if (shader != null)
                        m_ShapeVertexColoredAlphaBlendMaterial = new Material(shader);
                    else
                        Debug.LogError("Missing shader Light2d-Shape-Superimpose");
                }

                if (m_ShapeLightOverlapMode == LightOverlapMode.Additive)
                    return m_ShapeVertexColoredAdditiveMaterial;
                else
                    return m_ShapeVertexColoredAlphaBlendMaterial;
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
