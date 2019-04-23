using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    // TODO: 
    //     Fix parametric mesh code so that the vertices, triangle, and color arrays are only recreated when number of sides change
    //     Change code to update mesh only when it is on screen. Maybe we can recreate a changed mesh if it was on screen last update (in the update), and if it wasn't set it dirty. If dirty, in the OnBecameVisible function create the mesh and clear the dirty flag.
    [ExecuteAlways, DisallowMultipleComponent]
    sealed public partial class Light2D : MonoBehaviour
    {
        /// <summary>
        /// an enumeration of the types of light
        /// </summary>
        public enum LightType
        {
            Parametric = 0,
            Freeform = 1,
            Sprite = 2,
            Point = 3,
            Global = 4
        }

        /// <summary>
        ///  Light overlap modes. For typical lighting use additive. To override a lights color with the color of another light use AlphaBlend.
        /// </summary>
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
        static Dictionary<int, Color>[] s_GlobalClearColors = SetupGlobalClearColors();

        internal static Dictionary<int, Color>[] globalClearColors { get { return s_GlobalClearColors; } }

        //------------------------------------------------------------------------------------------
        //                                Variables/Properties
        //------------------------------------------------------------------------------------------

        
        [UnityEngine.Animations.NotKeyable]
        [SerializeField]
        LightType m_LightType = LightType.Parametric;
        LightType m_PreviousLightType = (LightType)LightType.Parametric;

        [SerializeField]
        int m_LightOperationIndex = 0;

        [SerializeField]
        float m_FalloffIntensity = 0.5f;
            
        [ColorUsage(false, false)]
        [SerializeField]
        Color m_Color = Color.white;
        Color m_PreviousColor = Color.white;
        [SerializeField]
        float m_Intensity = 1;
        float m_PreviousIntensity = 1;

        [SerializeField] float m_LightVolumeOpacity = 0.0f;
        [SerializeField] int[] m_ApplyToSortingLayers = new int[1];     // These are sorting layer IDs. If we need to update this at runtime make sure we add code to update global lights
        [SerializeField] Sprite m_LightCookieSprite = null;
        [SerializeField] bool m_UseNormalMap = false;

        [SerializeField] int m_LightOrder = 0;
        [SerializeField] LightOverlapMode m_LightOverlapMode = LightOverlapMode.Additive;

        int m_PreviousLightOrder = -1;
        int m_PreviousLightOperationIndex;
        float       m_PreviousLightVolumeOpacity;
        Sprite      m_PreviousLightCookieSprite     = null;
        Mesh        m_Mesh;
        int         m_LightCullingIndex             = -1;
        Bounds      m_LocalBounds;

        internal struct LightStats
        {
            public int totalLights;
            public int totalNormalMapUsage;
            public int totalVolumetricUsage;
        }

        /// <summary>
        /// The lights current type
        /// </summary>
        public LightType lightType
        {
            get => m_LightType;
            set => m_LightType = value;
        }

        /// <summary>
        /// The lights current operation index
        /// </summary>
        public int lightOperationIndex => m_LightOperationIndex;

        /// <summary>
        /// The lights current color
        /// </summary>
        public Color color
        {
            get { return m_Color; }
            set
            {
                AddGlobalLight(this, true);
                m_Color = value;
            }
        }

        /// <summary>
        /// The lights current intensity
        /// </summary>
        public float intensity
        {
            get { return m_Intensity; }
            set
            {
                AddGlobalLight(this, true);
                m_Intensity = value;
            }
        }

        /// <summary>
        /// The lights current intensity
        /// </summary>
        public float volumeOpacity => m_LightVolumeOpacity;
        public Sprite lightCookieSprite => m_LightCookieSprite;
        public float falloffIntensity => m_FalloffIntensity;
        public bool useNormalMap => m_UseNormalMap;
        public LightOverlapMode lightOverlapMode => m_LightOverlapMode;
        public int lightOrder => m_LightOrder;


        //==========================================================================================
        //                              Functions
        //==========================================================================================

        internal static Dictionary<int, Color>[] SetupGlobalClearColors()
        {
            Dictionary<int,Color>[] globalClearColors = new Dictionary<int, Color>[k_LightOperationCount];
            for(int i=0;i<k_LightOperationCount;i++)
            {
                globalClearColors[i] = new Dictionary<int, Color>();
            }
            return globalClearColors;
        }

        internal static List<Light2D>[] SetupLightArray()
        {
            List<Light2D>[] retArray = new List<Light2D>[k_LightOperationCount];

            for (int i = 0; i < retArray.Length; i++)
                retArray[i] = new List<Light2D>();

            return retArray;
        }

        internal static void SetupCulling(Camera camera)
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

        internal static List<Light2D> GetLightsByLightOperation(int lightOpIndex)
        {
            return s_Lights[lightOpIndex];
        }

        internal int GetTopMostLitLayer()
        {
            int largestIndex = -1;
            int largestLayer = 0;

            // TODO: SortingLayer.layers allocates the memory for the returned array.
            // An alternative to this is to keep m_ApplyToSortingLayers sorted by using SortingLayer.GetLayerValueFromID in the comparer.
            SortingLayer[] layers = SortingLayer.layers;    
            for(int i = 0; i < m_ApplyToSortingLayers.Length; ++i)
            {
                for(int layer = layers.Length - 1; layer >= largestLayer; --layer)
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

        internal void UpdateMesh()
        {
            GetMesh(true);
        }

        internal bool IsLitLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? Array.IndexOf(m_ApplyToSortingLayers, layer) >= 0 : false;
        }

        void InsertLight()
        {
            var lightList = s_Lights[m_LightOperationIndex];
            int index = 0;

            while (index < lightList.Count && m_LightOrder > lightList[index].m_LightOrder)
                index++;

            lightList.Insert(index, this);
        }

        void UpdateLightOperation()
        {
            if (m_LightOperationIndex == m_PreviousLightOperationIndex)
                return;

            if(m_LightType == LightType.Global)
            {
                RemoveGlobalLight(m_PreviousLightOperationIndex, this);
                AddGlobalLight(this);
            }

            s_Lights[m_PreviousLightOperationIndex].Remove(this);
            m_PreviousLightOperationIndex = m_LightOperationIndex;
            InsertLight();
        }

        BoundingSphere GetBoundingSphere()
        {
            return IsShapeLight() ? GetShapeLightBoundingSphere() : GetPointLightBoundingSphere();
        }

        internal Mesh GetMesh(bool forceUpdate = false)
        {
            if (m_Mesh != null && !forceUpdate)
                return m_Mesh;

            if (m_Mesh == null)
                m_Mesh = new Mesh();

            Color combinedColor = m_Intensity * m_Color;
            switch (m_LightType)
            {
                case LightType.Freeform:
                    m_LocalBounds = LightUtility.GenerateShapeMesh(ref m_Mesh, m_ShapePath, m_ShapeLightFalloffSize);
                    break;
                case LightType.Parametric:
                    m_LocalBounds = LightUtility.GenerateParametricMesh(ref m_Mesh, m_ShapeLightParametricRadius, m_ShapeLightFalloffSize, m_ShapeLightParametricAngleOffset, m_ShapeLightParametricSides);
                    break;
                case LightType.Sprite:
                    m_LocalBounds = LightUtility.GenerateSpriteMesh(ref m_Mesh, m_LightCookieSprite, 1);
                    break;
                case LightType.Point:
                    m_LocalBounds = LightUtility.GenerateParametricMesh(ref m_Mesh, 1.412135f, 0, 0, 4);
                    break;
            }

            return m_Mesh;
        }

        internal bool IsLightVisible(Camera camera)
        {
            bool isVisible = (s_CullingGroup == null || s_CullingGroup.IsVisible(m_LightCullingIndex)) && isActiveAndEnabled;

#if UNITY_EDITOR
            isVisible &= UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(gameObject, camera);
#endif
            return isVisible;
        }

        static internal void AddGlobalLight(Light2D light2D, bool overwriteColor = false)
        {
            for (int i = 0; i < light2D.m_ApplyToSortingLayers.Length; i++)
            {
                int sortingLayer = light2D.m_ApplyToSortingLayers[i];
                Dictionary<int, Color> globalColorOp = s_GlobalClearColors[light2D.m_LightOperationIndex];
                if (!globalColorOp.ContainsKey(sortingLayer))
                {
                    globalColorOp.Add(sortingLayer, light2D.m_Intensity * light2D.m_Color);
                }
                else
                {
                    globalColorOp[sortingLayer] = light2D.m_Intensity * light2D.m_Color;
                    if(!overwriteColor)
                        Debug.LogError("More than one global light on layer " + SortingLayer.IDToName(sortingLayer) + " for light operation index " + light2D.m_LightOperationIndex);
                }
            }
        }


        static internal void RemoveGlobalLight(int lightOperationIndex, Light2D light2D)
        {
            for (int i = 0; i < light2D.m_ApplyToSortingLayers.Length; i++)
            {
                int sortingLayer = light2D.m_ApplyToSortingLayers[i];
                Dictionary<int, Color> globalColorOp = s_GlobalClearColors[lightOperationIndex];
                if (globalColorOp.ContainsKey(sortingLayer))
                    globalColorOp.Remove(sortingLayer);
            }
        }


        private void Awake()
        {
            if (m_ShapePath == null || m_ShapePath.Length == 0)
                m_ShapePath = new Vector3[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };

            GetMesh();
        }

        private void OnEnable()
        {
            // This has to stay in OnEnable() because we need to re-initialize the static variables after a domain reload.
            if (s_CullingGroup == null)
            {
                s_CullingGroup = new CullingGroup();
                RenderPipeline.beginCameraRendering += SetupCulling;
            }

            if (!s_Lights[m_LightOperationIndex].Contains(this))
                InsertLight();

            m_PreviousLightOperationIndex = m_LightOperationIndex;

            if (m_LightType == LightType.Global)
                AddGlobalLight(this);

            m_PreviousLightType = m_LightType;
        }

        private void OnDisable()
        {
            bool anyLightLeft = false;

            for (int i = 0; i < s_Lights.Length; ++i)
            {
                s_Lights[i].Remove(this);

                if (s_Lights[i].Count > 0)
                    anyLightLeft = true;
            }

            if (!anyLightLeft && s_CullingGroup != null)
            {
                s_CullingGroup.Dispose();
                s_CullingGroup = null;
                RenderPipeline.beginCameraRendering -= SetupCulling;
            }

            if (m_LightType == LightType.Global)
                RemoveGlobalLight(m_LightOperationIndex, this);
        }

        internal List<Vector2> GetFalloffShape()
        {
            List<Vector2> shape = new List<Vector2>();
            List<Vector2> extrusionDir = new List<Vector2>();
            LightUtility.GetFalloffShape(m_ShapePath, ref extrusionDir);
            for (int i = 0; i < m_ShapePath.Length; i++)
            {
                Vector2 position = new Vector2();
                position.x = m_ShapePath[i].x + this.shapeLightFalloffSize * extrusionDir[i].x;
                position.y = m_ShapePath[i].y + this.shapeLightFalloffSize * extrusionDir[i].y;
                shape.Add(position);
            }
            return shape;
        }

        static internal LightStats GetLightStatsByLayer(int layer)
        {
            LightStats returnStats = new LightStats();
            for(int lightOpIndex=0; lightOpIndex < s_Lights.Length; lightOpIndex++)
            {
                List<Light2D> lights = s_Lights[lightOpIndex];
                for (int lightIndex = 0; lightIndex < lights.Count; lightIndex++)
                {
                    Light2D light = lights[lightIndex];

                    if (light.IsLitLayer(layer))
                    {
                        returnStats.totalLights++;
                        if (light.useNormalMap)
                            returnStats.totalNormalMapUsage++;
                        if (light.volumeOpacity > 0)
                            returnStats.totalVolumetricUsage++;
                    }
                }

            }
            return returnStats;
        }

        private void LateUpdate()
        {
            UpdateLightOperation();

            bool rebuildMesh = false;

            // Sorting. InsertLight() will make sure the lights are sorted.
            if (LightUtility.CheckForChange(m_LightOrder, ref m_PreviousLightOrder))
            {
                s_Lights[(int)m_LightOperationIndex].Remove(this);
                InsertLight();
            }

            if (m_LightType != m_PreviousLightType)
            {
                if(m_PreviousLightType == LightType.Global)
                    RemoveGlobalLight(m_LightOperationIndex, this);

                if (m_LightType == LightType.Global)
                    AddGlobalLight(this);
                else
                    rebuildMesh = true;

                m_PreviousLightType = m_LightType;
            }

            // Mesh Rebuilding
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightParametricRadius, ref m_PreviousShapeLightParametricRadius);
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightParametricSides, ref m_PreviousShapeLightParametricSides);
            rebuildMesh |= LightUtility.CheckForChange(m_LightVolumeOpacity, ref m_PreviousLightVolumeOpacity);
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightParametricAngleOffset, ref m_PreviousShapeLightParametricAngleOffset);
            rebuildMesh |= LightUtility.CheckForChange(m_LightCookieSprite, ref m_PreviousLightCookieSprite);
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightFalloffOffset, ref m_PreviousShapeLightFalloffOffset);

#if UNITY_EDITOR
            rebuildMesh |= LightUtility.CheckForChange(GetShapePathHash(), ref m_PreviousShapePathHash);
#endif
            if(rebuildMesh && m_LightType != LightType.Global)
                UpdateMesh();

            bool updateGlobalColor = LightUtility.CheckForChange(m_Color, ref m_PreviousColor) || LightUtility.CheckForChange(m_Intensity, ref m_PreviousIntensity);
            if (updateGlobalColor && m_LightType == LightType.Global)
                Light2D.AddGlobalLight(this, true);
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawIcon(transform.position, "PointLight Gizmo", true);
        }
    }
}
