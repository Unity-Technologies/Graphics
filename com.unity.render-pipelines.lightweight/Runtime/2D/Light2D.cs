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
    sealed public partial class Light2D : MonoBehaviour
    {
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

        public enum LightOverlapMode
        {
            Additive,
            AlphaBlend
        }

        public enum LightQuality
        {
            Fast,
            Accurate
        }

        //------------------------------------------------------------------------------------------
        //                                      Static
        //------------------------------------------------------------------------------------------

        static CullingGroup m_CullingGroup;
        static List<Light2D>[] m_Lights = SetupLightArray();

        //------------------------------------------------------------------------------------------
        //                                Variables/Properties
        //------------------------------------------------------------------------------------------
        private Mesh m_Mesh = null;

        [SerializeField]
        public LightProjectionTypes lightProjectionType
        {
            get { return m_LightProjectionType; }
            set { m_LightProjectionType = value; }
        }
        [SerializeField]
        private LightProjectionTypes m_LightProjectionType = LightProjectionTypes.Shape;
        private LightProjectionTypes m_PreviousLightProjectionType = LightProjectionTypes.Shape;

        public Color color
        {
            get { return m_LightColor; }
            set { m_LightColor = value; }
        }
        [ColorUsageAttribute(false, true)]
        [SerializeField]
        public Color m_LightColor = Color.white;
        private Color m_PreviousLightColor = Color.white;

        public Sprite lightCookieSprite
        {
            get { return m_LightCookieSprite; }
            set { m_LightCookieSprite = value; }
        }
        [SerializeField]
        private Sprite m_LightCookieSprite;
        private Sprite m_PreviousLightCookieSprite = null;

        public float volumeOpacity
        {
            get { return m_LightVolumeOpacity; }
            set { m_LightVolumeOpacity = value; }
        }
        [SerializeField]
        private float m_LightVolumeOpacity = 0.0f;
        private float m_PreviousLightVolumeOpacity = 0.0f;

        [SerializeField]
        [Serialization.FormerlySerializedAs("m_ShapeLightType")]
        private LightOperation m_LightOperation = LightOperation.Type0;
        private LightOperation m_PreviousLightOperation = LightOperation.Type0;

        [SerializeField]
        int[] m_ApplyToSortingLayers = new int[1];     // These are sorting layer IDs.


        private int m_LightCullingIndex = -1;
        private Bounds m_LocalBounds;


        // TODO make these functions
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


        //==========================================================================================
        //                              Functions
        //==========================================================================================

        // TODO: This is used in the editor, make internal somehow
        public void UpdateMesh()
        {
            GetMesh(true);
        }

        // TODO: This is used in the editor, make internal somehow
        public void UpdateMaterial()
        {
            m_ShapeCookieSpriteAdditiveMaterial = null;
            m_ShapeCookieSpriteAlphaBlendMaterial = null;
            m_ShapeCookieSpriteVolumeMaterial = null;
            m_PointLightMaterial = null;
            m_PointLightVolumeMaterial = null;
            GetMaterial();
        }

        static internal List<Light2D>[] SetupLightArray()
        {
            int numLightTypes = (int)Light2DType.Count;
            List<Light2D>[] retArray = new List<Light2D>[numLightTypes];
            for (int i = 0; i < numLightTypes; i++)
                retArray[i] = new List<Light2D>();

            return retArray;
        }

        static internal void SetupCulling(Camera camera)
        {
            if (m_CullingGroup == null)
                return;

            m_CullingGroup.targetCamera = camera;

            int totalLights = 0;
            for (int lightTypeIndex = 0; lightTypeIndex < m_Lights.Length; lightTypeIndex++)
                totalLights += m_Lights[lightTypeIndex].Count;

            BoundingSphere[] boundingSpheres = new BoundingSphere[totalLights];

            int lightCullingIndex = 0;
            for (int lightTypeIndex = 0; lightTypeIndex < m_Lights.Length; lightTypeIndex++)
            {
                for (int lightIndex = 0; lightIndex < m_Lights[lightTypeIndex].Count; lightIndex++)
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

        internal bool IsLitLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? m_ApplyToSortingLayers.Contains(layer) : false;
        }

        internal void InsertLight(Light2D light)
        {
            int index = 0;
            int lightType = (int)m_LightOperation;
            while (index < m_Lights[lightType].Count && m_ShapeLightOrder > m_Lights[lightType][index].m_ShapeLightOrder)
                index++;

            m_Lights[lightType].Insert(index, this);
        }

        internal void UpdateLightOperation(LightOperation type)
        {
            if (type != m_PreviousLightOperation)
            {
                m_Lights[(int)m_LightOperation].Remove(this);
                m_LightOperation = type;
                m_PreviousLightOperation = m_LightOperation;
                InsertLight(this);
            }
        }

        internal void UpdateLightProjectionType(LightProjectionTypes type)
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

        internal BoundingSphere GetBoundingSphere()
        {
            BoundingSphere boundingSphere = new BoundingSphere();

            if (m_LightProjectionType == LightProjectionTypes.Shape)
                boundingSphere = GetShapeLightBoundingSphere();
            else
                boundingSphere = GetPointLightBoundingSphere();

            return boundingSphere;
        }

        internal Material GetVolumeMaterial()
        {
            if (m_LightProjectionType == LightProjectionTypes.Shape)
                return GetShapeLightVolumeMaterial();
            else if(m_LightProjectionType == LightProjectionTypes.Point)
                return GetPointLightVolumeMaterial();

            return null;
        }

        internal Material GetMaterial()
        {
            if (m_LightProjectionType == LightProjectionTypes.Shape)
                return GetShapeLightMaterial();
            else if(m_LightProjectionType == LightProjectionTypes.Point)
                return GetPointLightMaterial();

            return null;
        }

        internal Mesh GetMesh(bool forceUpdate = false)
        {
            if (m_Mesh == null || forceUpdate)
            {
                if (m_Mesh == null)
                    m_Mesh = new Mesh();

                if (m_LightProjectionType == LightProjectionTypes.Shape)
                {
                    m_LocalBounds = GetShapeLightMesh(ref m_Mesh);
                }
                else if(m_LightProjectionType == LightProjectionTypes.Point)
                {
                     m_LocalBounds = LightUtility.GenerateParametricMesh(ref m_Mesh, 1.412135f, Vector2.zero, 4, 0, m_LightColor, m_LightVolumeOpacity);
                }
            }

            return m_Mesh;
        }

        internal static List<Light2D> GetPointLights()
        {
            return m_Lights[(int)Light2DType.Point];
        }

        internal static List<Light2D> GetShapeLights(LightOperation lightOperation)
        {
            return m_Lights[(int)lightOperation];
        }

        internal bool IsLightVisible(Camera camera)
        {
            bool isVisible = (m_CullingGroup == null || m_CullingGroup.IsVisible(m_LightCullingIndex)) && isActiveAndEnabled;

            #if UNITY_EDITOR
                isVisible = isVisible && UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(gameObject, camera);
            #endif

            return isVisible;
        }


        private void RegisterLight()
        {
            if (m_Lights != null)
            {
                int index = (int)m_LightOperation;
                if (!m_Lights[index].Contains(this))
                    InsertLight(this);
            }
        }

        private void Awake()
        {
            if (m_ShapePath == null)
                m_ShapePath = m_Spline.m_ControlPoints.Select(x => x.position).ToArray();

            if (m_ShapePath.Length == 0)
                m_ShapePath = new Vector3[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };

            GetMesh();
        }

        private void OnEnable()
        {
            if (m_CullingGroup == null)
            {
                m_CullingGroup = new CullingGroup();
                RenderPipeline.beginCameraRendering += SetupCulling;
            }

            RegisterLight();
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

        private void LateUpdate()
        {
            // Sorting
            if(LightUtility.CheckForChange<int>(m_ShapeLightOrder, ref m_PreviousShapeLightOrder) && this.m_LightProjectionType == LightProjectionTypes.Shape)
            {
                //m_ShapeLightStyle = CookieStyles.Parametric;
                m_Lights[(int)m_LightOperation].Remove(this);
                InsertLight(this);
            }

            // If we changed blending modes then we need to clear our material
            //if(CheckForChange<BlendingModes>(m_ShapeLightOverlapMode, ref m_PreviousShapeLightBlending))
            //{
            //    m_ShapeCookieSpriteMaterial = null;
            //    m_ShapeVertexColoredMaterial = null;
            //}

            // Mesh Rebuilding
            bool rebuildMesh = false;

            rebuildMesh |= LightUtility.CheckForColorChange(m_LightColor, ref m_PreviousLightColor);
            rebuildMesh |= LightUtility.CheckForChange<float>(m_ShapeLightFeathering, ref m_PreviousShapeLightFeathering);
            rebuildMesh |= LightUtility.CheckForVector2Change(m_ShapeLightOffset, ref m_PreviousShapeLightOffset);
            rebuildMesh |= LightUtility.CheckForChange<int>(m_ShapeLightParametricSides, ref m_PreviousShapeLightParametricSides);
            rebuildMesh |= LightUtility.CheckForChange<float>(m_LightVolumeOpacity, ref m_PreviousLightVolumeOpacity);

#if UNITY_EDITOR
            var shapePathHash = GetShapePathHash();
            rebuildMesh |= m_PrevShapePathHash != shapePathHash;
            m_PrevShapePathHash = shapePathHash;
#endif

            if (rebuildMesh)
            {
                UpdateMesh();
            }

            bool rebuildMaterial = false;
            rebuildMaterial |= LightUtility.CheckForSpriteChange(m_LightCookieSprite, ref m_PreviousLightCookieSprite);
            if (rebuildMaterial)
            {
                UpdateMaterial();
                rebuildMaterial = false;
            }

            UpdateLightProjectionType(m_LightProjectionType);
            UpdateLightOperation(m_LightOperation);
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (Selection.activeGameObject != transform.gameObject)
                Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true);
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true);
        }
    }
}
