using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Shape;
using Unity.Mathematics;
using Unity.RenderPipeline2D.External.LibTessDotNet;
using Mesh = UnityEngine.Mesh;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    // TODO: 
    //     Fix parametric mesh code so that the vertices, triangle, and color arrays are only recreated when number of sides change
    //     Change code to update mesh only when it is on screen. Maybe we can recreate a changed mesh if it was on screen last update (in the update), and if it wasn't set it dirty. If dirty, in the OnBecameVisible function create the mesh and clear the dirty flag.
    [ExecuteInEditMode]
    public class Light2D : MonoBehaviour
    {
        private Mesh m_Mesh = null;

        public enum LightProjectionTypes
        {
            Shape = 0,
            Point = 1
        }

        public enum CookieStyles
        {
            Parametric = 0,
            //FreeForm=1,
            Sprite = 2,
        }

        private const int NUM_LIGHT_TYPES = 4;  // Right now its point, ambient, specular, and rim
        private enum Light2DTypes
        {
            Specular = 0,
            LocalAmbient,
            Rim,
            Point
        }

        public enum ShapeLightTypes
        {
            Specular,
            LocalAmbient,
            Rim
        }

        public enum ParametricShapes
        {
            Circle,
            Freeform,
        }

        [SerializeField]
        private LightProjectionTypes m_LightProjectionType = LightProjectionTypes.Shape;
        private LightProjectionTypes m_PreviousLightProjectionType = LightProjectionTypes.Shape;

        //------------------------------------------------------------------------------------------
        //                              Values for Point light type
        //------------------------------------------------------------------------------------------
        public float m_PointLightInnerAngle = 360;
        public float m_PointLightOuterAngle = 360;
        public float m_PointLightInnerRadius = 1;
        public float m_PointLightOuterRadius = 1;
        public bool m_CastsShadows = true;
        public Color m_ShadowColor;
        public bool m_CastsSoftShadows = true;

        public int m_ApplyToLayers = 0;

        //------------------------------------------------------------------------------------------
        //                              Values for Shape light type
        //------------------------------------------------------------------------------------------
        public CookieStyles m_ShapeLightStyle = CookieStyles.Parametric;

        [SerializeField]
        private ShapeLightTypes m_ShapeLightType = ShapeLightTypes.Specular;
        private ShapeLightTypes m_PreviousShapeLightType = ShapeLightTypes.Specular;

        public ParametricShapes m_ParametricShape = ParametricShapes.Circle; // This should be removed and fixed in the inspector

        private float m_PreviousShapeLightFeathering = -1;
        public float m_ShapeLightFeathering = 0.50f;

        private int m_PreviousParametricSides = -1;
        public int m_ParametricSides = 128;
        static Material m_ShapeCookieSpriteMaterial = null;
        static Material m_ShapeVertexColoredMaterial = null;

        [ColorUsageAttribute(false,true)]
        public Color m_LightColor = Color.white;
        private Color m_PreviousLightColor = Color.white;

        public Vector2 m_ShapeLightOffset;
        private Vector2 m_PreviousShapeLightOffset;

        public Sprite m_LightCookieSprite;
        private Sprite m_PreviousLightCookieSprite = null;

        static List<Light2D>[] m_Lights = SetupLightArray();
        
        static public List<Light2D>[] SetupLightArray()
        {
            List<Light2D>[] retArray = new List<Light2D>[NUM_LIGHT_TYPES];
            for (int i = 0; i < NUM_LIGHT_TYPES; i++)
                retArray[i] = new List<Light2D>();

            return retArray;
        }

        public void UpdateShapeLightType(ShapeLightTypes type)
        {
            if (m_LightProjectionType == LightProjectionTypes.Shape)
            {
                if (type != m_PreviousShapeLightType)
                {
                    m_Lights[(int)m_ShapeLightType].Remove(this);
                    m_Lights[(int)type].Add(this);
                    m_ShapeLightType = type;
                    m_PreviousShapeLightType = m_ShapeLightType;
                }
            }
        }

        public void UpdateLightProjectionType(LightProjectionTypes type)
        {
            if (type != m_PreviousLightProjectionType)
            {
                // Remove the old value
                int index = (int)Light2DTypes.Point;
                if (m_PreviousLightProjectionType == LightProjectionTypes.Shape)
                    index = (int)m_ShapeLightType;
                if (m_Lights[index].Contains(this))
                    m_Lights[index].Remove(this);

                // Add the new value
                index = (int)Light2DTypes.Point;
                if (type == LightProjectionTypes.Shape)
                    index = (int)m_ShapeLightType;
                if (!m_Lights[index].Contains(this))
                    m_Lights[index].Add(this);

                m_LightProjectionType = type;
                m_PreviousLightProjectionType = m_LightProjectionType;
            }
        }


        public ShapeLightTypes ShapeLightType
        {
            get { return m_ShapeLightType; }
            set { UpdateShapeLightType(value); }
        }

        public LightProjectionTypes LightProjectionType
        {
            get { return m_LightProjectionType; }
            set { UpdateLightProjectionType(value); }
        }


        [SerializeField]
        Spline m_Spline = new Spline() { isExtensionsSupported = false };

        public Spline spline
        {
            get { return m_Spline; }
        }

        private List<Vector2> UpdateFeatheredShapeLightMesh(ContourVertex[] contourPoints, int contourPointCount)
        {

            List<Vector2> feathered = new List<Vector2>();
            for (int i = 0; i < contourPointCount; ++i)
            {
                int h = (i == 0) ? (contourPointCount - 1) : (i - 1);
                int j = (i + 1) % contourPointCount;

                float2 pp = new float2(contourPoints[h].Position.X, contourPoints[h].Position.Y);
                float2 cp = new float2(contourPoints[i].Position.X, contourPoints[i].Position.Y);
                float2 np = new float2(contourPoints[j].Position.X, contourPoints[j].Position.Y);

                float2 cpd = cp - pp;
                float2 npd = np - cp;
                if (math.length(cpd) < 0.001f || math.length(npd) < 0.001f)
                    continue;

                float2 vl = math.normalize(cpd);
                float2 vr = math.normalize(npd);

                vl = new float2(-vl.y, vl.x);
                vr = new float2(-vr.y, vr.x);

                float2 va = math.normalize(vl) + math.normalize(vr);
                float2 vn = math.normalize(va);

                if (math.length(va) > 0 && math.length(vn) > 0)
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

        public void UpdateShapeLightMesh(Color color)
        {

            Color meshInteriorColor = color;
            Color meshFeatherColor = new Color(color.r, color.g, color.b, 0);

            int pointCount = spline.GetPointCount();
            var inputs = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount; ++i)
                inputs[i] = new ContourVertex() { Position = new Vec3() { X = spline.GetPosition(i).x, Y = spline.GetPosition(i).y }, Data = meshInteriorColor };
            
            var feathered = UpdateFeatheredShapeLightMesh(inputs, pointCount);
            int featheredPointCount = feathered.Count + pointCount;

            Tess tessI = new Tess();  // Interior
            Tess tessF = new Tess();  // Feathered Edge

            var inputsI = new ContourVertex[pointCount];
            for (int i = 0; i < pointCount - 1; ++i)
            {
                var inputsF = new ContourVertex[4];
                inputsF[0] = new ContourVertex() { Position = new Vec3() { X = spline.GetPosition(i).x, Y = spline.GetPosition(i).y }, Data = meshFeatherColor };
                inputsF[1] = new ContourVertex() { Position = new Vec3() { X = feathered[i].x, Y = feathered[i].y }, Data = meshInteriorColor };
                inputsF[2] = new ContourVertex() { Position = new Vec3() { X = feathered[i + 1].x, Y = feathered[i + 1].y },  Data = meshInteriorColor };
                inputsF[3] = new ContourVertex() { Position = new Vec3() { X = spline.GetPosition(i + 1).x, Y = spline.GetPosition(i + 1).y },  Data = meshFeatherColor };
                tessF.AddContour(inputsF, ContourOrientation.Original);

                inputsI[i] = new ContourVertex() { Position = new Vec3() { X = feathered[i].x, Y = feathered[i].y }, Data = meshInteriorColor };
            }

            var inputsL = new ContourVertex[4];
            inputsL[0] = new ContourVertex() { Position = new Vec3() { X = spline.GetPosition(pointCount - 1).x, Y = spline.GetPosition(pointCount - 1).y }, Data = meshFeatherColor };
            inputsL[1] = new ContourVertex() { Position = new Vec3() { X = feathered[pointCount - 1].x, Y = feathered[pointCount - 1].y }, Data = meshInteriorColor };
            inputsL[2] = new ContourVertex() { Position = new Vec3() { X = feathered[0].x, Y = feathered[0].y }, Data = meshInteriorColor };
            inputsL[3] = new ContourVertex() { Position = new Vec3() { X = spline.GetPosition(0).x, Y = spline.GetPosition(0).y }, Data = meshFeatherColor };
            tessF.AddContour(inputsL, ContourOrientation.Original);

            inputsI[pointCount-1] = new ContourVertex() { Position = new Vec3() { X = feathered[pointCount - 1].x, Y = feathered[pointCount - 1].y }, Data = meshInteriorColor };
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

            m_Mesh.Clear();
            m_Mesh.vertices = finalVertices.ToArray();
            m_Mesh.colors = finalColors.ToArray();
            m_Mesh.SetIndices(finalIndices.ToArray(), MeshTopology.Triangles, 0);
        }

        public Material GetMaterial()
        {
            if (m_LightProjectionType == LightProjectionTypes.Shape)
            {
                if (m_ShapeLightStyle == CookieStyles.Sprite)
                {
                    // This is causing Object.op_inequality fix this
                    if (m_ShapeCookieSpriteMaterial == null && m_LightCookieSprite && m_LightCookieSprite.texture != null)
                    {
                        Shader shader = Shader.Find("Hidden/Light2DSprite");
                        m_ShapeCookieSpriteMaterial = new Material(shader);
                        m_ShapeCookieSpriteMaterial.SetTexture("_MainTex", m_LightCookieSprite.texture);
                    }

                    return m_ShapeCookieSpriteMaterial;
                }
                else
                {
                    // This is causing Object.op_inequality fix this
                    if (m_ShapeVertexColoredMaterial == null)
                    {
                        Shader shader = Shader.Find("Hidden/Light2DVertexColored");
                        m_ShapeVertexColoredMaterial = new Material(shader);
                    }

                    return m_ShapeVertexColoredMaterial;
                }
            }

            return null;
        }

        public Mesh GenerateParametricMesh(float radius, int sides, float feathering, Color color)
        {

            float angleOffset = Mathf.PI / 2.0f;
            if (sides < 3)
            {
                radius = 0.70710678118654752440084436210485f * radius;
                angleOffset = Mathf.PI / 4.0f;
                sides = 4;
            }

            // Return a shape with radius = 1
            Vector3[] vertices;
            int[] triangles;
            Color[] colors;

            int centerIndex;
            if (feathering <= 0.0f || feathering >= 1.0f)
            {
                vertices = new Vector3[1 + sides];
                triangles = new int[3 * sides];
                colors = new Color[1 + sides];
                centerIndex = sides;
            }
            else
            {
                vertices = new Vector3[1 + 2 * sides];
                colors = new Color[1 + 2 * sides];
                triangles = new int[3 * 3 * sides];
                centerIndex = 2 * sides;
            }


            Vector3 posOffset = new Vector3(m_ShapeLightOffset.x, m_ShapeLightOffset.y);
            Color transparentColor = new Color(color.r, color.g, color.b, 0);
            vertices[centerIndex] = Vector3.zero + posOffset;
            colors[centerIndex] = color;

            float radiansPerSide = 2 * Mathf.PI / sides;
            for (int i = 0; i < sides; i++)
            {
                float endAngle = (i + 1) * radiansPerSide;
                Vector3 endPoint = new Vector3(radius * Mathf.Cos(endAngle + angleOffset), radius * Mathf.Sin(endAngle + angleOffset), 0) + posOffset;

                int vertexIndex;
                if (feathering <= 0.0f)
                {
                    vertexIndex = (i + 1) % sides;
                    vertices[vertexIndex] = endPoint;
                    colors[vertexIndex] = color;

                    int triangleIndex = 3 * i;
                    triangles[triangleIndex] = (i + 1) % sides;
                    triangles[triangleIndex + 1] = i;
                    triangles[triangleIndex + 2] = centerIndex;
                }
                else if (feathering >= 1.0f)
                {
                    vertexIndex = (i + 1) % sides;
                    vertices[vertexIndex] = endPoint;
                    colors[vertexIndex] = transparentColor;

                    int triangleIndex = 3 * i;
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex + 1] = i;
                    triangles[triangleIndex + 2] = centerIndex;
                }
                else
                {
                    Vector3 endSplitPoint = (1 - feathering) * endPoint;
                    vertexIndex = (2 * i + 2) % (2 * sides);

                    vertices[vertexIndex] = endPoint;
                    vertices[vertexIndex + 1] = endSplitPoint;

                    colors[vertexIndex] = transparentColor;
                    colors[vertexIndex + 1] = color;

                    // Triangle 1 (Tip)
                    int triangleIndex = 9 * i;
                    triangles[triangleIndex] = vertexIndex + 1;
                    triangles[triangleIndex + 1] = 2 * i + 1;
                    triangles[triangleIndex + 2] = centerIndex;

                    // Triangle 2 (Upper Top Left)
                    triangles[triangleIndex + 3] = vertexIndex;
                    triangles[triangleIndex + 4] = 2 * i;
                    triangles[triangleIndex + 5] = 2 * i + 1;

                    // Triangle 2 (Bottom Top Left)
                    triangles[triangleIndex + 6] = vertexIndex + 1;
                    triangles[triangleIndex + 7] = vertexIndex;
                    triangles[triangleIndex + 8] = 2 * i + 1;
                }
            }

            m_Mesh.Clear();
            m_Mesh.vertices = vertices;
            m_Mesh.colors = colors;
            m_Mesh.triangles = triangles;

            return m_Mesh;
        }

        public Mesh GenerateSpriteMesh(Sprite sprite, Color color)
        {

            //if (m_Mesh == null)
            //{
            if (sprite != null)
            {
                Vector2[] vertices2d = sprite.vertices;
                Vector3[] vertices3d = new Vector3[vertices2d.Length];
                Color[] colors = new Color[vertices2d.Length];

                ushort[] triangles2d = sprite.triangles;
                int[] triangles3d = new int[triangles2d.Length];


                Vector3 center = 0.5f * (sprite.bounds.min + sprite.bounds.max);

                for (int vertexIdx = 0; vertexIdx < vertices2d.Length; vertexIdx++)
                {
                    Vector3 pos = new Vector3(vertices2d[vertexIdx].x, vertices2d[vertexIdx].y) - center;
                    pos = new Vector3(vertices2d[vertexIdx].x / sprite.bounds.size.x, vertices2d[vertexIdx].y / sprite.bounds.size.y);

                    //vertices3d[vertexIdx] = new Vector3(pos.x + m_ShapeLightOffset.x, pos.y + m_ShapeLightOffset.y);
                    vertices3d[vertexIdx] = pos;
                    colors[vertexIdx] = color;
                }

                for (int triangleIdx = 0; triangleIdx < triangles2d.Length; triangleIdx++)
                {
                    triangles3d[triangleIdx] = (int)triangles2d[triangleIdx];
                }

                m_Mesh.Clear();
                m_Mesh.vertices = vertices3d;
                m_Mesh.uv = sprite.uv;
                m_Mesh.triangles = triangles3d;
                m_Mesh.colors = colors;
                //}
            }


            return m_Mesh;
        }

        public Mesh GetMesh(bool forceUpdate = false)
        {
            if (m_Mesh == null || forceUpdate)
            {
                if (m_Mesh == null)
                    m_Mesh = new Mesh();

                //float savedA = m_LightColor.a;
                Color adjColor = m_LightColor;

                // This is done so we don't start going white on a non white light if the intensity is high enough
                //float maxColor = adjColor.r > adjColor.b ? adjColor.r : adjColor.b;
                //maxColor = maxColor > adjColor.g ? maxColor : adjColor.g;
                //if (maxColor > 1.0f)
                //    adjColor = (1 / maxColor) * adjColor;
                //adjColor.a = savedA;

                if (m_ShapeLightStyle == CookieStyles.Parametric)
                {
                    if (m_ParametricShape == ParametricShapes.Freeform)
                        UpdateShapeLightMesh(adjColor);
                    else
                        m_Mesh = GenerateParametricMesh(0.5f, m_ParametricSides, m_ShapeLightFeathering, adjColor);
                }
                else if (m_ShapeLightStyle == CookieStyles.Sprite)
                {
                    m_Mesh = GenerateSpriteMesh(m_LightCookieSprite, adjColor);
                }
            }

            return m_Mesh;
        }


        public bool IsLitLayer(int layer)
        {
            return m_ApplyToLayers == layer;
        }

        public void UpdateMesh()
        {
            GetMesh(true);
        }

        public void UpdateMaterial()
        {
            m_ShapeCookieSpriteMaterial = null;
            GetMaterial();
        }

        private void OnDestroy()
        {
            if (m_Lights != null)
            {
                for (int i = 0; i < m_Lights.Length; i++)
                {
                    if (m_Lights[i].Contains(this))
                        m_Lights[i].Remove(this);
                }
            }
        }

        public static List<Light2D> GetPointLights()
        {
            return m_Lights[(int)Light2DTypes.Point];
        }

        public static List<Light2D> GetSpecularLights()
        {
            return m_Lights[(int)Light2DTypes.Specular];
        }

        public static List<Light2D> GetAmbientLights()
        {
            return m_Lights[(int)Light2DTypes.LocalAmbient];
        }

        public static List<Light2D> GetRimLights()
        {
            return m_Lights[(int)Light2DTypes.Rim];
        }

        void RegisterLight()
        {
            if (m_Lights != null)
            {
                int index = (int)Light2DTypes.Point;
                if (m_LightProjectionType == LightProjectionTypes.Shape)
                    index = (int)m_ShapeLightType;

                if (!m_Lights[index].Contains(this))
                    m_Lights[index].Add(this);
            }
        }

        void Awake()
        {
            GetMesh();
            RegisterLight();


            if (spline.GetPointCount() == 0)
            {
                spline.InsertPointAt(0, new Vector3(-0.5f, -0.5f));
                spline.InsertPointAt(1, new Vector3(0.5f, -0.5f));
                spline.InsertPointAt(2, new Vector3(0.5f, 0.5f));
                spline.InsertPointAt(3, new Vector3(-0.5f, 0.5f));
            }
        }

        void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                RegisterLight();
#endif
        }


        bool CheckForColorChange(Color i, ref Color j)
        {
            bool retVal = i.r != j.r || i.g != j.g || i.b != j.b || i.a != j.a;
            j = i;
            return retVal;
        }

        bool CheckForVector2Change(Vector2 i, ref Vector2 j)
        {
            bool retVal = i.x != j.x || i.y != j.y;
            j = i;
            return retVal;
        }

        bool CheckForSpriteChange(Sprite i, ref Sprite j)
        {
            // If both are null
            bool retVal = false;

            // If one is not null but the other is
            if (i == null ^ j == null)
                retVal = true;

            // if both are not null then do another test
            if (i != null && j != null)
                retVal = i.GetInstanceID() != j.GetInstanceID();

            j = i;
            return retVal;
        }

        bool CheckForChange<T>(T a, ref T b)
        {
            int compareResult = Comparer<T>.Default.Compare(a, b);
            b = a;
            return compareResult != 0;
        }

        private void LateUpdate()
        {
            bool rebuildMesh = false;

            rebuildMesh |= CheckForColorChange(m_LightColor, ref m_PreviousLightColor);
            rebuildMesh |= CheckForChange<float>(m_ShapeLightFeathering, ref m_PreviousShapeLightFeathering);
            rebuildMesh |= CheckForVector2Change(m_ShapeLightOffset, ref m_PreviousShapeLightOffset);
            rebuildMesh |= CheckForChange<int>(m_ParametricSides, ref m_PreviousParametricSides);
            
            //rebuildMesh |= CheckForChange<>
            if (rebuildMesh)
            {
                UpdateMesh();
                rebuildMesh = false;
            }

            bool rebuildMaterial = false;
            rebuildMaterial |= CheckForSpriteChange(m_LightCookieSprite, ref m_PreviousLightCookieSprite);
            if (rebuildMaterial)
            {
                UpdateMaterial();
                rebuildMaterial = false;
            }

            UpdateLightProjectionType(m_LightProjectionType);
            UpdateShapeLightType(m_ShapeLightType);
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (Selection.activeGameObject != transform.gameObject)
                Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true);
#endif
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true);
        }
    }
}
