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
    // TODO: 
    //     Fix parametric mesh code so that the vertices, triangle, and color arrays are only recreated when number of sides change
    //     Change code to update mesh only when it is on screen. Maybe we can recreate a changed mesh if it was on screen last update (in the update), and if it wasn't set it dirty. If dirty, in the OnBecameVisible function create the mesh and clear the dirty flag.
    [ExecuteAlways]
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

        private enum Light2DType
        {
            ShapeType0 = 0,
            ShapeType1,
            ShapeType2,
            Point,
            Count
        }

        public enum LightOperation
        {
            Type0 = 0,
            Type1 = 1,
            Type2 = 2
        }

        public enum ParametricShapes
        {
            Circle,
            Freeform,
        }

        public enum BlendingModes
        {
            Additive,
            Superimpose, // Overlay might be confusing because of the photoshop overlay blending mode
        }

        public enum LightQuality
        {
            Fast,
            Accurate
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
        public float m_PointLightZDistance = 3;
        public LightQuality m_LightQuality = LightQuality.Fast;

        [SerializeField] int[] m_ApplyToSortingLayers = new int[1];     // These are sorting layer IDs.

        //------------------------------------------------------------------------------------------
        //                              Values for Shape light type
        //------------------------------------------------------------------------------------------
        public CookieStyles m_ShapeLightStyle = CookieStyles.Parametric;

        [SerializeField]
        [Serialization.FormerlySerializedAs("m_ShapeLightType")]
        private LightOperation m_LightOperation = LightOperation.Type0;
        private LightOperation m_PreviousLightOperation = LightOperation.Type0;

        public ParametricShapes m_ParametricShape = ParametricShapes.Circle; // This should be removed and fixed in the inspector

        private float m_PreviousShapeLightFeathering = -1;
        public float m_ShapeLightFeathering = 0.50f;

        private int m_PreviousParametricSides = -1;
        public int m_ParametricSides = 128;

        static Material m_PointLightMaterial = null;
        static Material m_PointLightVolumeMaterial = null;

        static Material m_ShapeCookieSpriteSuperimposeMaterial = null;
        static Material m_ShapeCookieSpriteAdditiveMaterial = null;
        static Material m_ShapeCookieSpriteVolumeMaterial = null;

        static Material m_ShapeVertexColoredSuperimposeMaterial = null;
        static Material m_ShapeVertexColoredAdditiveMaterial = null;
        static Material m_ShapeVertexColoredVolumeMaterial = null;

        [ColorUsageAttribute(false,true)]
        public Color m_LightColor = Color.white;
        private Color m_PreviousLightColor = Color.white;

        public Vector2 m_ShapeLightOffset;
        private Vector2 m_PreviousShapeLightOffset;

        public Sprite m_LightCookieSprite;
        private Sprite m_PreviousLightCookieSprite = null;

        [SerializeField]
        private float m_LightVolumeOpacity = 0.0f;
        private float m_PreviousLightVolumeOpacity = 0.0f;

        [SerializeField]
        private int m_ShapeLightOrder = 0;
        private int m_PreviousShapeLightOrder = 0;

        [SerializeField]
        private BlendingModes m_ShapeLightBlending = BlendingModes.Additive;
        //private BlendingModes m_PreviousShapeLightBlending = BlendingModes.Additive;

        public float LightVolumeOpacity
        {
            get { return m_LightVolumeOpacity; }
            set { m_LightVolumeOpacity = value; }
        }

        private int m_LightCullingIndex = -1;
        private Bounds m_LocalBounds;
        static CullingGroup m_CullingGroup;

        static List<Light2D>[] m_Lights = SetupLightArray();

        public LightProjectionTypes GetLightProjectionType()
        {
            return m_LightProjectionType;
        }

        static public List<Light2D>[] SetupLightArray()
        {
            int numLightTypes = (int)Light2DType.Count;
            List<Light2D>[] retArray = new List<Light2D>[numLightTypes];
            for (int i = 0; i < numLightTypes; i++)
                retArray[i] = new List<Light2D>();

            return retArray;
        }

        public BoundingSphere GetBoundingSphere()
        {
            BoundingSphere boundingSphere = new BoundingSphere();
            if (m_LightProjectionType == LightProjectionTypes.Shape)
            {
                Vector3 maximum = transform.TransformPoint(m_LocalBounds.max);
                Vector3 minimum = transform.TransformPoint(m_LocalBounds.min);
                Vector3 center = 0.5f * (maximum + minimum);
                float radius = Vector3.Magnitude(maximum - center);

                boundingSphere.radius = radius;
                boundingSphere.position = center;
            }
            else
            {
                boundingSphere.radius = m_PointLightOuterRadius;
                boundingSphere.position = transform.position;
            }
            return boundingSphere;
        }

        static public void SetupCulling(Camera camera)
        {
            if (m_CullingGroup == null)
                return;

            m_CullingGroup.targetCamera = camera;

            int totalLights = 0;
            for (int lightTypeIndex = 0; lightTypeIndex < m_Lights.Length; lightTypeIndex++)
                totalLights += m_Lights[lightTypeIndex].Count;

            BoundingSphere[] boundingSpheres = new BoundingSphere[totalLights];

            int lightCullingIndex = 0;
            for(int lightTypeIndex=0; lightTypeIndex < m_Lights.Length; lightTypeIndex++)
            {
                for(int lightIndex=0; lightIndex < m_Lights[lightTypeIndex].Count; lightIndex++)
                {
                    Light2D light = m_Lights[lightTypeIndex][lightIndex];
                    if (light != null)
                    {
                        boundingSpheres[lightCullingIndex] = light.GetBoundingSphere();
                        light.m_LightCullingIndex = lightCullingIndex++;
                    }
                }
            }

            m_CullingGroup.SetBoundingSpheres(boundingSpheres);
        }

        public void InsertLight(Light2D light)
        {
            int index = 0;
            int lightType = (int)m_LightOperation;
            while (index < m_Lights[lightType].Count && m_ShapeLightOrder > m_Lights[lightType][index].m_ShapeLightOrder)
                index++;

            m_Lights[lightType].Insert(index, this);
        }

        public void UpdateLightOperation(LightOperation type)
        {
            if (type != m_PreviousLightOperation)
            {
                m_Lights[(int)m_LightOperation].Remove(this);
                m_LightOperation = type;
                m_PreviousLightOperation = m_LightOperation;
                InsertLight(this);
            }
        }

        public void UpdateLightProjectionType(LightProjectionTypes type)
        {
            if (type != m_PreviousLightProjectionType)
            {
                // Remove the old value
                int index = (int)m_LightOperation;
                if (m_Lights[index].Contains(this))
                    m_Lights[index].Remove(this);

                // Add the new value
                index = (int)m_LightOperation;
                if (!m_Lights[index].Contains(this))
                    m_Lights[index].Add(this);

                m_LightProjectionType = type;
                m_PreviousLightProjectionType = m_LightProjectionType;
            }
        }

        public LightOperation lightOperation
        {
            get { return m_LightOperation; }
            set { UpdateLightOperation(value); }
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
                Vector2 vn = va.normalized;

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

            var volumeColors = new Vector4[finalColors.Count];
            for (int i = 0; i < volumeColors.Length; i++)
                volumeColors[i] = new Vector4(1, 1, 1, m_LightVolumeOpacity);

            Vector3[] vertices = finalVertices.ToArray();
            m_Mesh.Clear();
            m_Mesh.vertices = vertices;
            m_Mesh.tangents = volumeColors;
            m_Mesh.colors = finalColors.ToArray();
            m_Mesh.SetIndices(finalIndices.ToArray(), MeshTopology.Triangles, 0);

            m_LocalBounds = LightUtility.CalculateBoundingSphere(ref vertices);
        }

        public Material GetVolumeMaterial()
        {
            if (m_LightProjectionType == LightProjectionTypes.Shape)
            {
                if (m_ShapeLightStyle == CookieStyles.Sprite)
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
                        if(shader != null)
                            m_ShapeVertexColoredVolumeMaterial = new Material(shader);
                        else
                            Debug.LogError("Missing shader Light2d-Shape-Volumetric");
                    }

                    return m_ShapeVertexColoredVolumeMaterial;
                }
            }
            else if(m_LightProjectionType == LightProjectionTypes.Point)
            {
                if (m_PointLightVolumeMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Light2d-Point-Volumetric");
                    if(shader != null )
                    m_PointLightVolumeMaterial = new Material(shader);
                }

                return m_PointLightVolumeMaterial;
            }

            return null;
        }

        public Material GetMaterial()
        {
            if (m_LightProjectionType == LightProjectionTypes.Shape)
            {
                if (m_ShapeLightStyle == CookieStyles.Sprite)
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

                    if (m_ShapeCookieSpriteSuperimposeMaterial == null && m_LightCookieSprite && m_LightCookieSprite.texture != null)
                    {
                        Shader shader = Shader.Find("Hidden/Light2D-Sprite-Superimpose"); ;

                        if (shader != null)
                        {
                            m_ShapeCookieSpriteSuperimposeMaterial = new Material(shader);
                            m_ShapeCookieSpriteSuperimposeMaterial.SetTexture("_MainTex", m_LightCookieSprite.texture);
                        }
                        else
                            Debug.LogError("Missing shader Light2d-Sprite-Superimpose");
                    }


                    if (m_ShapeLightBlending == BlendingModes.Additive)
                        return m_ShapeCookieSpriteAdditiveMaterial;
                    else
                        return m_ShapeCookieSpriteSuperimposeMaterial;
                }
                else
                {
                    // This is causing Object.op_inequality fix this
                    if (m_ShapeVertexColoredAdditiveMaterial == null)
                    {
                        Shader shader = Shader.Find("Hidden/Light2D-Shape-Additive"); ;
                        if(shader != null)
                            m_ShapeVertexColoredAdditiveMaterial = new Material(shader);
                        else
                            Debug.LogError("Missing shader Light2d-Shape-Additive");
                    }

                    if (m_ShapeVertexColoredSuperimposeMaterial == null)
                    {
                        Shader shader = Shader.Find("Hidden/Light2D-Shape-Superimpose"); ;
                        if (shader != null)
                            m_ShapeVertexColoredSuperimposeMaterial = new Material(shader);
                        else
                            Debug.LogError("Missing shader Light2d-Shape-Superimpose");
                    }

                    if (m_ShapeLightBlending == BlendingModes.Additive)
                        return m_ShapeVertexColoredAdditiveMaterial;
                    else
                        return m_ShapeVertexColoredSuperimposeMaterial;
                }
            }
            if(m_LightProjectionType == LightProjectionTypes.Point)
            {
                if (m_PointLightMaterial == null)
                {
                    Shader shader = Shader.Find("Hidden/Light2D-Point");
                    if (shader != null)
                        m_PointLightMaterial = new Material(shader);
                    else
                        Debug.LogError("Missing shader Light2D-Point");
                }

                return m_PointLightMaterial;
            }

            return null;
        }


        public Mesh GetMesh(bool forceUpdate = false)
        {
            if (m_Mesh == null || forceUpdate)
            {
                if (m_Mesh == null)
                    m_Mesh = new Mesh();

                if (m_LightProjectionType == LightProjectionTypes.Shape)
                {
                    if (m_ShapeLightStyle == CookieStyles.Parametric)
                    {
                        if (m_ParametricShape == ParametricShapes.Freeform)
                            UpdateShapeLightMesh(m_LightColor);
                        else
                        {
                            m_LocalBounds = LightUtility.GenerateParametricMesh(ref m_Mesh, 0.5f, m_ShapeLightOffset, m_ParametricSides, m_ShapeLightFeathering, m_LightColor, m_LightVolumeOpacity);
                        }
                    }
                    else if (m_ShapeLightStyle == CookieStyles.Sprite)
                    {
                        m_LocalBounds = LightUtility.GenerateSpriteMesh(ref m_Mesh, m_LightCookieSprite, m_LightColor, m_LightVolumeOpacity, 1);
                    }
                }
                else if(m_LightProjectionType == LightProjectionTypes.Point)
                {
                     m_LocalBounds = LightUtility.GenerateParametricMesh(ref m_Mesh, 1.412135f, Vector2.zero, 4, 0, m_LightColor, m_LightVolumeOpacity);
                }
            }

            return m_Mesh;
        }

        public bool IsLitLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? m_ApplyToSortingLayers.Contains(layer) : false;
        }

        public void UpdateMesh()
        {
            GetMesh(true);
        }

        public void UpdateMaterial()
        {
            m_ShapeCookieSpriteAdditiveMaterial = null;
            m_ShapeCookieSpriteSuperimposeMaterial = null;
            m_ShapeCookieSpriteVolumeMaterial = null;
            m_PointLightMaterial = null;
            m_PointLightVolumeMaterial = null;
            GetMaterial();
        }

        private void OnDisable()
        {
            bool anyLightLeft = false;

            if (m_Lights != null)
            {
                for (int i = 0; i < m_Lights.Length; i++)
                {
                    if (m_Lights[i].Contains(this))
                        m_Lights[i].Remove(this);

                    if (m_Lights[i].Count > 0)
                        anyLightLeft = true;
                }
            }

            if (!anyLightLeft && m_CullingGroup != null)
            {
                m_CullingGroup.Dispose();
                m_CullingGroup = null;
                RenderPipeline.beginCameraRendering -= SetupCulling;
            }
        }

        public static List<Light2D> GetPointLights()
        {
            return m_Lights[(int)Light2DType.Point];
        }

        public static List<Light2D> GetShapeLights(LightOperation lightOperation)
        {
            return m_Lights[(int)lightOperation];
        }

        void RegisterLight()
        {
            if (m_Lights != null)
            {
                int index = (int)m_LightOperation;
                if (!m_Lights[index].Contains(this))
                    InsertLight(this);
            }
        }

        void Awake()
        {
            GetMesh();

            if (spline.GetPointCount() == 0)
            {
                spline.InsertPointAt(0, new Vector3(-0.5f, -0.5f));
                spline.InsertPointAt(1, new Vector3(0.5f, -0.5f));
                spline.InsertPointAt(2, new Vector3(0.5f, 0.5f));
                spline.InsertPointAt(3, new Vector3(-0.5f, 0.5f));
            }
        }

        void OnEnable()
        {
            if (m_CullingGroup == null)
            {
                m_CullingGroup = new CullingGroup();
                RenderPipeline.beginCameraRendering += SetupCulling;
            }

            RegisterLight();
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
            // Sorting
            if(CheckForChange<int>(m_ShapeLightOrder, ref m_PreviousShapeLightOrder) && this.m_LightProjectionType == LightProjectionTypes.Shape)
            {
                //m_ShapeLightStyle = CookieStyles.Parametric;
                m_Lights[(int)m_LightOperation].Remove(this);
                InsertLight(this);
            }

            // If we changed blending modes then we need to clear our material
            //if(CheckForChange<BlendingModes>(m_ShapeLightBlending, ref m_PreviousShapeLightBlending))
            //{
            //    m_ShapeCookieSpriteMaterial = null;
            //    m_ShapeVertexColoredMaterial = null;
            //}

            // Mesh Rebuilding
            bool rebuildMesh = false;

            rebuildMesh |= CheckForColorChange(m_LightColor, ref m_PreviousLightColor);
            rebuildMesh |= CheckForChange<float>(m_ShapeLightFeathering, ref m_PreviousShapeLightFeathering);
            rebuildMesh |= CheckForVector2Change(m_ShapeLightOffset, ref m_PreviousShapeLightOffset);
            rebuildMesh |= CheckForChange<int>(m_ParametricSides, ref m_PreviousParametricSides);
            rebuildMesh |= CheckForChange<float>(m_LightVolumeOpacity, ref m_PreviousLightVolumeOpacity);

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
            UpdateLightOperation(m_LightOperation);
        }

        public bool IsLightVisible(Camera camera)
        {
            bool isVisible = (m_CullingGroup == null || m_CullingGroup.IsVisible(m_LightCullingIndex)) && isActiveAndEnabled;

#if UNITY_EDITOR
            isVisible = isVisible && UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(gameObject, camera);
#endif
            return isVisible;
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
