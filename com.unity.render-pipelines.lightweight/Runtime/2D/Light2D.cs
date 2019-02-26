using System.Collections.Generic;
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
        public enum LightType
        {
            Parametric = 0,
            Freeform = 1,
            Sprite = 2,
            Point = 3
        }

        public enum LightOverlapMode
        {
            Additive,
            AlphaBlend
        }

        //------------------------------------------------------------------------------------------
        //                                      Static/Constants
        //------------------------------------------------------------------------------------------

        const int k_LightOperationCount = 4;    // This must match the array size of m_LightOperations in _2DRendererData.
        static List<Light2D>[] s_Lights = SetupLightArray();
        static CullingGroup s_CullingGroup;
        static BoundingSphere[] s_BoundingSpheres;

        //------------------------------------------------------------------------------------------
        //                                Variables/Properties
        //------------------------------------------------------------------------------------------

        [SerializeField]
        [Serialization.FormerlySerializedAs("m_LightProjectionType")]
        LightType m_LightType = LightType.Parametric;

        [SerializeField]
        [Serialization.FormerlySerializedAs("m_ShapeLightType")]
        [Serialization.FormerlySerializedAs("m_LightOperation")]
        int m_LightOperationIndex;

        [ColorUsage(false, true)]
        [SerializeField]
        [Serialization.FormerlySerializedAs("m_LightColor")]
        Color m_Color = Color.white;

        [SerializeField] float  m_LightVolumeOpacity    = 0.0f;
        [SerializeField] int[]  m_ApplyToSortingLayers  = new int[1];     // These are sorting layer IDs.
        [SerializeField] Sprite m_LightCookieSprite     = null;

        LightType   m_PreviousLightType             = LightType.Parametric;
        int         m_PreviousLightOperationIndex;
        Color       m_PreviousColor                 = Color.white;
        float       m_PreviousLightVolumeOpacity;
        Sprite      m_PreviousLightCookieSprite;
        Mesh        m_Mesh;
        int         m_LightCullingIndex             = -1;
        Bounds      m_LocalBounds;

        public LightType lightType
        {
            get => m_LightType;
            set => UpdateLightProjectionType(value);
        }

        public int      lightOperationIndex => m_LightOperationIndex;
        public Color    color               => m_Color;
        public float    volumeOpacity       => m_LightVolumeOpacity;
        public Sprite   lightCookieSprite   => m_LightCookieSprite;

        //==========================================================================================
        //                              Functions
        //==========================================================================================

        static internal List<Light2D>[] SetupLightArray()
        {
            List<Light2D>[] retArray = new List<Light2D>[k_LightOperationCount];

            for (int i = 0; i < retArray.Length; i++)
                retArray[i] = new List<Light2D>();

            return retArray;
        }

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

        static internal void SetupCulling(Camera camera)
        {
            if (s_CullingGroup == null)
                return;

            s_CullingGroup.targetCamera = camera;

            int totalLights = 0;
            for (int lightOpIndex = 0; lightOpIndex < s_Lights.Length; ++lightOpIndex)
                totalLights += s_Lights[lightOpIndex].Count;

            if (s_BoundingSpheres == null)
                s_BoundingSpheres = new BoundingSphere[Mathf.Max(1024, 2 * totalLights)];
            else if (totalLights > s_BoundingSpheres.Length)
                s_BoundingSpheres = new BoundingSphere[2 * totalLights];

            int currentLightCullingIndex = 0;
            for (int lightOpIndex = 0; lightOpIndex < s_Lights.Length; ++lightOpIndex)
            {
                var lightsPerLightOp = s_Lights[lightOpIndex];

                for (int lightIndex = 0; lightIndex < lightsPerLightOp.Count; ++lightIndex)
                {
                    Light2D light = lightsPerLightOp[lightIndex];
                    if (light == null)
                        continue;

                    s_BoundingSpheres[currentLightCullingIndex] = light.GetBoundingSphere();
                    light.m_LightCullingIndex = currentLightCullingIndex++;
                }
            }

            s_CullingGroup.SetBoundingSpheres(s_BoundingSpheres);
            s_CullingGroup.SetBoundingSphereCount(currentLightCullingIndex);
        }

        internal int GetTopMostLitLayer()
        {
            int largestIndex = -1;
            int largestLayer = 0;

            SortingLayer[] layers = SortingLayer.layers;
            for(int i=0;i< m_ApplyToSortingLayers.Length;i++)
            {
                for(int layer=layers.Length-1;layer>=largestLayer; layer--)
                {
                    if (layers[layer].id == m_ApplyToSortingLayers[i])
                    {
                        largestIndex = i;
                        largestLayer = layer;
                    }
                }
            }

            if (largestIndex >= 0)
                return m_ApplyToSortingLayers[largestIndex];
            else
                return -1;
        }

        internal bool IsLitLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? m_ApplyToSortingLayers.Contains(layer) : false;
        }

        internal void InsertLight(Light2D light)
        {
            int index = 0;
            int lightType = (int)m_LightOperationIndex;
            while (index < s_Lights[lightType].Count && m_ShapeLightOrder > s_Lights[lightType][index].m_ShapeLightOrder)
                index++;

            s_Lights[lightType].Insert(index, this);
        }

        internal void UpdateLightOperation(int lightOpIndex)
        {
            if (lightOpIndex != m_PreviousLightOperationIndex)
            {
                s_Lights[(int)m_LightOperationIndex].Remove(this);
                m_LightOperationIndex = lightOpIndex;
                m_PreviousLightOperationIndex = m_LightOperationIndex;
                InsertLight(this);
            }
        }

        internal void UpdateLightProjectionType(LightType type)
        {
            if (type != m_PreviousLightType)
            {
                // Remove the old value
                int index = (int)m_LightOperationIndex;
                if (s_Lights[index].Contains(this))
                    s_Lights[index].Remove(this);

                // Add the new value
                index = (int)m_LightOperationIndex;
                if (!s_Lights[index].Contains(this))
                    s_Lights[index].Add(this);

                m_LightType = type;
                m_PreviousLightType = m_LightType;
            }
        }

        internal BoundingSphere GetBoundingSphere()
        {
            BoundingSphere boundingSphere = new BoundingSphere();

            if (Light2D.IsShapeLight(m_LightType))
                boundingSphere = GetShapeLightBoundingSphere();
            else
                boundingSphere = GetPointLightBoundingSphere();

            return boundingSphere;
        }

        internal Material GetVolumeMaterial()
        {
            if (Light2D.IsShapeLight(m_LightType))
                return GetShapeLightVolumeMaterial();
            else if(m_LightType == LightType.Point)
                return GetPointLightVolumeMaterial();

            return null;
        }

        internal Material GetMaterial()
        {
            if (Light2D.IsShapeLight(m_LightType))
                return GetShapeLightMaterial();
            else if(m_LightType == LightType.Point)
                return GetPointLightMaterial();

            return null;
        }

        internal Mesh GetMesh(bool forceUpdate = false)
        {
            if (m_Mesh == null || forceUpdate)
            {
                if (m_Mesh == null)
                    m_Mesh = new Mesh();

                if (IsShapeLight(m_LightType))
                {
                    m_LocalBounds = GetShapeLightMesh(ref m_Mesh);
                }
                else if(m_LightType == LightType.Point)
                {
                     m_LocalBounds = LightUtility.GenerateParametricMesh(ref m_Mesh, 1.412135f, Vector2.zero, 0, 4, 0, m_Color, m_LightVolumeOpacity);
                }
            }

            return m_Mesh;
        }

        internal static List<Light2D> GetShapeLights(int lightOpIndex)
        {
            return s_Lights[lightOpIndex];
        }

        internal bool IsLightVisible(Camera camera)
        {
            bool isVisible = (s_CullingGroup == null || s_CullingGroup.IsVisible(m_LightCullingIndex)) && isActiveAndEnabled;

            #if UNITY_EDITOR
                isVisible = isVisible && UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(gameObject, camera);
            #endif

            return isVisible;
        }


        private void RegisterLight()
        {
            if (s_Lights != null)
            {
                int index = (int)m_LightOperationIndex;
                if (!s_Lights[index].Contains(this))
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
            if (s_CullingGroup == null)
            {
                s_CullingGroup = new CullingGroup();
                RenderPipeline.beginCameraRendering += SetupCulling;
            }

            RegisterLight();
        }

        private void OnDisable()
        {
            bool anyLightLeft = false;

            if (s_Lights != null)
            {
                for (int i = 0; i < s_Lights.Length; i++)
                {
                    if (s_Lights[i].Contains(this))
                        s_Lights[i].Remove(this);

                    if (s_Lights[i].Count > 0)
                        anyLightLeft = true;
                }
            }

            if (!anyLightLeft && s_CullingGroup != null)
            {
                s_CullingGroup.Dispose();
                s_CullingGroup = null;
                RenderPipeline.beginCameraRendering -= SetupCulling;
            }
        }

        private void LateUpdate()
        {
            // Sorting
            if(LightUtility.CheckForChange<int>(m_ShapeLightOrder, ref m_PreviousShapeLightOrder) && Light2D.IsShapeLight(this.m_LightType))
            {
                //m_ShapeLightStyle = CookieStyles.Parametric;
                s_Lights[(int)m_LightOperationIndex].Remove(this);
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

            rebuildMesh |= LightUtility.CheckForColorChange(m_Color, ref m_PreviousColor);
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

            UpdateLightProjectionType(m_LightType);
            UpdateLightOperation(m_LightOperationIndex);
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
