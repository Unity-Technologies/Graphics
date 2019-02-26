using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Shape;
using Unity.RenderPipeline2D.External.LibTessDotNet;
using Mesh = UnityEngine.Mesh;
using System.Linq;

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
        public float shapeLightFeathering
        {
            get { return m_ShapeLightFeathering; }
            set { m_ShapeLightFeathering = value; }
        }
        [SerializeField]
        private float m_ShapeLightFeathering = 0.50f;
        private float m_PreviousShapeLightFeathering = -1;

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

        internal static bool IsShapeLight(LightType lightProjectionType)
        {
            return lightProjectionType != LightType.Point;
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


        Bounds GetShapeLightMesh(ref Mesh mesh)
        {
            Bounds localBounds = new Bounds();
            if (m_LightType == LightType.Freeform)
                localBounds = UpdateShapeLightMesh(m_Color);
            else if(m_LightType == LightType.Parametric)
            {
                localBounds = LightUtility.GenerateParametricMesh(ref mesh, 0.5f, m_ShapeLightOffset, m_ShapeLightParametricAngleOffset, m_ShapeLightParametricSides, m_ShapeLightFeathering, m_Color, m_LightVolumeOpacity);
            }
            else if (m_LightType == LightType.Sprite)
            {
                localBounds = LightUtility.GenerateSpriteMesh(ref mesh, m_LightCookieSprite, m_Color, m_LightVolumeOpacity, 1);
            }

            return localBounds;
        }

        private List<Vector2> UpdateFeatheredShapeLightMesh(ContourVertex[] contourPoints, int contourPointCount)
        {
            List<Vector2> feathered = new List<Vector2>();
            for (int i = 0; i < contourPointCount; ++i)
            {
                int h = (i == 0) ? (contourPointCount - 1) : (i - 1);
                int j = (i + 1) % contourPointCount;

                Vector2 pp = new Vector2(contourPoints[h].Position.X, contourPoints[h].Position.Y);
                Vector2 cp = new Vector2(contourPoints[i].Position.X, contourPoints[i].Position.Y);
                Vector2 np = new Vector2(contourPoints[j].Position.X, contourPoints[j].Position.Y);

                Vector2 cpd = cp - pp;
                Vector2 npd = np - cp;
                if (cpd.magnitude < 0.001f || npd.magnitude < 0.001f)
                    continue;

                Vector2 vl = cpd.normalized;
                Vector2 vr = npd.normalized;

                vl = new Vector2(-vl.y, vl.x);
                vr = new Vector2(-vr.y, vr.x);

                Vector2 va = vl.normalized + vr.normalized;
                Vector2 vn = -va.normalized;

                if (va.magnitude > 0 && vn.magnitude > 0)
                {
                    var t = cp + (vn * m_ShapeLightFeathering);
                    feathered.Add(t);
                }
            }
            return feathered;

        }

        internal object InterpCustomVertexData(Vec3 position, object[] data, float[] weights)
        {
            return data[0];
        }


        public Bounds UpdateShapeLightMesh(Color color)
        {
            Bounds localBounds;
            Color meshInteriorColor = color;
            Color meshFeatherColor = new Color(color.r, color.g, color.b, 0);

            int pointCount = m_ShapePath.Length;
            var inputs = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount; ++i)
                inputs[i] = new ContourVertex() { Position = new Vec3() { X = m_ShapePath[i].x, Y = m_ShapePath[i].y }, Data = meshFeatherColor };

            var feathered = UpdateFeatheredShapeLightMesh(inputs, pointCount);
            int featheredPointCount = feathered.Count + pointCount;

            Tess tessI = new Tess();  // Interior
            Tess tessF = new Tess();  // Feathered Edge

            var inputsI = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount - 1; ++i)
            {
                var inputsF = new ContourVertex[4];
                inputsF[0] = new ContourVertex() { Position = new Vec3() { X = m_ShapePath[i].x, Y = m_ShapePath[i].y }, Data = meshInteriorColor };
                inputsF[1] = new ContourVertex() { Position = new Vec3() { X = feathered[i].x, Y = feathered[i].y }, Data = meshFeatherColor };
                inputsF[2] = new ContourVertex() { Position = new Vec3() { X = feathered[i + 1].x, Y = feathered[i + 1].y }, Data = meshFeatherColor };
                inputsF[3] = new ContourVertex() { Position = new Vec3() { X = m_ShapePath[i + 1].x, Y = m_ShapePath[i + 1].y }, Data = meshInteriorColor };
                tessF.AddContour(inputsF, ContourOrientation.Original);

                inputsI[i] = new ContourVertex() { Position = new Vec3() { X = m_ShapePath[i].x, Y = m_ShapePath[i].y }, Data = meshInteriorColor };
            }

            var inputsL = new ContourVertex[4];
            inputsL[0] = new ContourVertex() { Position = new Vec3() { X = m_ShapePath[pointCount - 1].x, Y = m_ShapePath[pointCount - 1].y }, Data = meshInteriorColor };
            inputsL[1] = new ContourVertex() { Position = new Vec3() { X = feathered[pointCount - 1].x, Y = feathered[pointCount - 1].y }, Data = meshFeatherColor };
            inputsL[2] = new ContourVertex() { Position = new Vec3() { X = feathered[0].x, Y = feathered[0].y }, Data = meshFeatherColor };
            inputsL[3] = new ContourVertex() { Position = new Vec3() { X = m_ShapePath[0].x, Y = m_ShapePath[0].y }, Data = meshInteriorColor };
            tessF.AddContour(inputsL, ContourOrientation.Original);

            inputsI[pointCount - 1] = new ContourVertex() { Position = new Vec3() { X = m_ShapePath[pointCount - 1].x, Y = m_ShapePath[pointCount - 1].y }, Data = meshInteriorColor };
            tessI.AddContour(inputsI, ContourOrientation.Original);

            tessI.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, InterpCustomVertexData);
            tessF.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, InterpCustomVertexData);

            var indicesI = tessI.Elements.Select(i => i).ToArray();
            var verticesI = tessI.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray();
            var colorsI = tessI.Vertices.Select(v => new Color(((Color)v.Data).r, ((Color)v.Data).g, ((Color)v.Data).b, ((Color)v.Data).a)).ToArray();

            var indicesF = tessF.Elements.Select(i => i + verticesI.Length).ToArray();
            var verticesF = tessF.Vertices.Select(v => new Vector3(v.Position.X, v.Position.Y, 0)).ToArray();
            var colorsF = tessF.Vertices.Select(v => new Color(((Color)v.Data).r, ((Color)v.Data).g, ((Color)v.Data).b, ((Color)v.Data).a)).ToArray();


            List<Vector3> finalVertices = new List<Vector3>();
            List<int> finalIndices = new List<int>();
            List<Color> finalColors = new List<Color>();
            finalVertices.AddRange(verticesI);
            finalVertices.AddRange(verticesF);
            finalIndices.AddRange(indicesI);
            finalIndices.AddRange(indicesF);
            finalColors.AddRange(colorsI);
            finalColors.AddRange(colorsF);

            var volumeColors = new Vector4[finalColors.Count];
            for (int i = 0; i < volumeColors.Length; i++)
                volumeColors[i] = new Vector4(1, 1, 1, m_LightVolumeOpacity);

            Vector3[] vertices = finalVertices.ToArray();
            m_Mesh.Clear();
            m_Mesh.vertices = vertices;
            m_Mesh.tangents = volumeColors;
            m_Mesh.colors = finalColors.ToArray();
            m_Mesh.SetIndices(finalIndices.ToArray(), MeshTopology.Triangles, 0);

            localBounds = LightUtility.CalculateBoundingSphere(ref vertices);

            return localBounds;
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
