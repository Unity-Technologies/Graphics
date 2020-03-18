using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
#endif

namespace UnityEngine.Experimental.Rendering.Universal
{
    class Light2DManager : IDisposable
    {
        const int k_BlendStyleCount = 4;    // This must match the array size of m_LightBlendStyles in _2DRendererData.
        static Light2DManager s_Instance = new Light2DManager();

        Light2DManager m_PrevInstance;
        List<Light2D>[] m_Lights;
        CullingGroup m_CullingGroup;
        BoundingSphere[] m_BoundingSpheres;

        internal static List<Light2D>[] lights => s_Instance.m_Lights;

        internal static CullingGroup cullingGroup
        {
            get => s_Instance.m_CullingGroup;
            set => s_Instance.m_CullingGroup = value;
        }

        internal static BoundingSphere[] boundingSpheres
        {
            get => s_Instance.m_BoundingSpheres;
            set => s_Instance.m_BoundingSpheres = value;
        }

        internal static bool GetGlobalColor(int sortingLayerIndex, int blendStyleIndex, out Color color)
        {
            bool  foundGlobalColor = false;
            color = Color.black;

            // This should be rewritten to search only global lights
            List<Light2D> lights = s_Instance.m_Lights[blendStyleIndex];
            for (int i = 0; i < lights.Count; ++i)
            {
                Light2D light = lights[i];
                if (light.lightType == Light2D.LightType.Global && light.IsLitLayer(sortingLayerIndex))
                {
                    bool inCurrentPrefabStage = true;
#if UNITY_EDITOR
                    // If we found the first global light in our prefab stage
                    inCurrentPrefabStage = PrefabStageUtility.GetPrefabStage(light.gameObject) == PrefabStageUtility.GetCurrentPrefabStage();
#endif

                    if (inCurrentPrefabStage)
                    {
                        color = light.color * light.intensity;
                        return true;
                    }
                    else
                    {
                        if (!foundGlobalColor)
                        {
                            color = light.color * light.intensity;
                            foundGlobalColor = true;
                        }
                    }
                }
            }

            return foundGlobalColor;
        }

        internal static bool ContainsDuplicateGlobalLight(int sortingLayerIndex, int blendStyleIndex)
        {
            int globalLightCount = 0;

            // This should be rewritten to search only global lights
            List<Light2D> lights = s_Instance.m_Lights[blendStyleIndex];
            for (int i = 0; i < lights.Count; i++)
            {
                Light2D light = lights[i];
                if (light.lightType == Light2D.LightType.Global && light.IsLitLayer(sortingLayerIndex))
                {
#if UNITY_EDITOR
                    // If we found the first global light in our prefab stage
                    if (PrefabStageUtility.GetPrefabStage(light.gameObject) == PrefabStageUtility.GetCurrentPrefabStage())
#endif
                    {
                        if (globalLightCount > 0)
                            return true;

                        globalLightCount++;
                    }
                }
            }

            return false;
        }

        internal Light2DManager()
        {
            m_PrevInstance = s_Instance;
            s_Instance = this;

            m_Lights = new List<Light2D>[k_BlendStyleCount];
            for (int i = 0; i < m_Lights.Length; ++i)
                m_Lights[i] = new List<Light2D>();
        }

        public void Dispose()
        {
            s_Instance = m_PrevInstance;
        }
    }

    /// <summary>
    /// Class <c>Light2D</c> is a 2D light which can be used with the 2D Renderer.
    /// </summary>
    /// 
    [ExecuteAlways, DisallowMultipleComponent]
    [AddComponentMenu("Rendering/2D/Light 2D (Experimental)")]
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

        [UnityEngine.Animations.NotKeyable]
        [SerializeField]
        LightType m_LightType = LightType.Parametric;
        LightType m_PreviousLightType = (LightType)LightType.Parametric;

        [SerializeField, FormerlySerializedAs("m_LightOperationIndex")]
        int m_BlendStyleIndex = 0;


        [SerializeField]
        float m_FalloffIntensity = 0.5f;

        [ColorUsage(false)]
        [SerializeField]
        Color m_Color = Color.white;
        [SerializeField]
        float m_Intensity = 1;

        [SerializeField] float m_LightVolumeOpacity = 0.0f;
        [SerializeField] int[] m_ApplyToSortingLayers = new int[1];     // These are sorting layer IDs. If we need to update this at runtime make sure we add code to update global lights
        [SerializeField] Sprite m_LightCookieSprite = null;
        [SerializeField] bool m_UseNormalMap = false;

        [SerializeField] int m_LightOrder = 0;
        [SerializeField] bool m_AlphaBlendOnOverlap = false;

        int m_PreviousLightOrder = -1;
        int m_PreviousBlendStyleIndex;
        float       m_PreviousLightVolumeOpacity;
        bool        m_PreviousLightCookieSpriteExists = false;
        Sprite      m_PreviousLightCookieSprite     = null;
        Mesh        m_Mesh;
        int         m_LightCullingIndex             = -1;
        Bounds      m_LocalBounds;
        

        [Range(0,1)]
        [SerializeField] float m_ShadowIntensity    = 0.0f;
        [Range(0,1)]
        [SerializeField] float m_ShadowVolumeIntensity = 0.0f;

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
        public int blendStyleIndex { get => m_BlendStyleIndex; set => m_BlendStyleIndex = value; }

        /// <summary>
        /// Specifies the darkness of the shadow
        /// </summary>
        public float shadowIntensity { get => m_ShadowIntensity; set => m_ShadowIntensity = Mathf.Clamp01(value); }

        /// <summary>
        /// Specifies the darkness of the shadow
        /// </summary>
        public float shadowVolumeIntensity { get => m_ShadowVolumeIntensity; set => m_ShadowVolumeIntensity = Mathf.Clamp01(value); }


        /// <summary>
        /// The lights current color
        /// </summary>
        public Color color
        {
            get { return m_Color; }
            set { m_Color = value; }
        }

        /// <summary>
        /// The lights current intensity
        /// </summary>
        public float intensity
        {
            get { return m_Intensity; }
            set { m_Intensity = value; }
        }

        /// <summary>
        /// The lights current intensity
        /// </summary>
        public float volumeOpacity => m_LightVolumeOpacity;
        public Sprite lightCookieSprite => m_LightCookieSprite;
        public float falloffIntensity => m_FalloffIntensity;
        public bool useNormalMap => m_UseNormalMap;
        public bool alphaBlendOnOverlap => m_AlphaBlendOnOverlap;
        public int lightOrder { get => m_LightOrder; set => m_LightOrder = value; }

        internal int lightCullingIndex => m_LightCullingIndex;
        static SortingLayer[] s_SortingLayers;

#if UNITY_EDITOR
        public static string s_IconsPath = "Packages/com.unity.render-pipelines.universal/Editor/2D/Resources/SceneViewIcons/";
        public static string s_ParametricLightIconPath = s_IconsPath + "ParametricLight.png";
        public static string s_FreeformLightIconPath = s_IconsPath + "FreeformLight.png";
        public static string s_SpriteLightIconPath = s_IconsPath + "SpriteLight.png";
        public static string s_PointLightIconPath = s_IconsPath + "PointLight.png";
        public static string s_GlobalLightIconPath = s_IconsPath + "GlobalLight.png";
        public static string[] s_LightIconPaths = new string[] { s_ParametricLightIconPath, s_FreeformLightIconPath, s_SpriteLightIconPath, s_PointLightIconPath, s_GlobalLightIconPath };
#endif
        

        internal static void SetupCulling(ScriptableRenderContext context, Camera camera)
        {
            if (Light2DManager.cullingGroup == null)
                return;

            Light2DManager.cullingGroup.targetCamera = camera;

            int totalLights = 0;
            for (int blendStyleIndex = 0; blendStyleIndex < Light2DManager.lights.Length; ++blendStyleIndex)
                totalLights += Light2DManager.lights[blendStyleIndex].Count;

            if (Light2DManager.boundingSpheres == null)
                Light2DManager.boundingSpheres = new BoundingSphere[Mathf.Max(1024, 2 * totalLights)];
            else if (totalLights > Light2DManager.boundingSpheres.Length)
                Light2DManager.boundingSpheres = new BoundingSphere[2 * totalLights];

            int currentLightCullingIndex = 0;
            for (int blendStyleIndex = 0; blendStyleIndex < Light2DManager.lights.Length; ++blendStyleIndex)
            {
                var lightsPerBlendStyle = Light2DManager.lights[blendStyleIndex];

                for (int lightIndex = 0; lightIndex < lightsPerBlendStyle.Count; ++lightIndex)
                {
                    Light2D light = lightsPerBlendStyle[lightIndex];
                    if (light == null)
                        continue;

                    Light2DManager.boundingSpheres[currentLightCullingIndex] = light.GetBoundingSphere();
                    light.m_LightCullingIndex = currentLightCullingIndex++;
                }
            }

            Light2DManager.cullingGroup.SetBoundingSpheres(Light2DManager.boundingSpheres);
            Light2DManager.cullingGroup.SetBoundingSphereCount(currentLightCullingIndex);
        }

        internal static List<Light2D> GetLightsByBlendStyle(int blendStyleIndex)
        {
            return Light2DManager.lights[blendStyleIndex];
        }

        internal int GetTopMostLitLayer()
        {
            int largestIndex = -1;
            int largestLayer = 0;

            SortingLayer[] layers;
            if (Application.isPlaying)
            {
                if (s_SortingLayers == null)
                    s_SortingLayers = SortingLayer.layers;

                layers = s_SortingLayers;
            }
            else
                layers = SortingLayer.layers;


            for (int i = 0; i < m_ApplyToSortingLayers.Length; ++i)
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

        void UpdateMesh()
        {
            GetMesh(true);
        }

        internal bool IsLitLayer(int layer)
        {
            return m_ApplyToSortingLayers != null ? Array.IndexOf(m_ApplyToSortingLayers, layer) >= 0 : false;
        }

        void InsertLight()
        {
            var lightList = Light2DManager.lights[m_BlendStyleIndex];
            int index = 0;

            while (index < lightList.Count && m_LightOrder > lightList[index].m_LightOrder)
                index++;

            lightList.Insert(index, this);
        }

        void UpdateBlendStyle()
        {
            if (m_BlendStyleIndex == m_PreviousBlendStyleIndex)
                return;

            Light2DManager.lights[m_PreviousBlendStyleIndex].Remove(this);
            m_PreviousBlendStyleIndex = m_BlendStyleIndex;
            InsertLight();

            if (m_LightType == LightType.Global)
                ErrorIfDuplicateGlobalLight();
        }

        internal BoundingSphere GetBoundingSphere()
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
                    m_Mesh.Clear();
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
            bool isVisible = (Light2DManager.cullingGroup == null || Light2DManager.cullingGroup.IsVisible(m_LightCullingIndex)) && isActiveAndEnabled;

#if UNITY_EDITOR
            isVisible &= UnityEditor.SceneManagement.StageUtility.IsGameObjectRenderedByCamera(gameObject, camera);
#endif
            return isVisible;
        }

        internal void ErrorIfDuplicateGlobalLight()
        {
            for (int i = 0; i < m_ApplyToSortingLayers.Length; ++i)
            {
                int sortingLayer = m_ApplyToSortingLayers[i];

                if(Light2DManager.ContainsDuplicateGlobalLight(sortingLayer, blendStyleIndex))
                    Debug.LogError("More than one global light on layer " + SortingLayer.IDToName(sortingLayer) + " for light blend style index " + m_BlendStyleIndex);
            }
        }

        private void Awake()
        {
            GetMesh();
        }

        void OnEnable()
        {
            // This has to stay in OnEnable() because we need to re-initialize the static variables after a domain reload.
            if (Light2DManager.cullingGroup == null)
            {
                Light2DManager.cullingGroup = new CullingGroup();
                RenderPipelineManager.beginCameraRendering += SetupCulling;
            }

            if (!Light2DManager.lights[m_BlendStyleIndex].Contains(this))
                InsertLight();

            m_PreviousBlendStyleIndex = m_BlendStyleIndex;

            if (m_LightType == LightType.Global)
                ErrorIfDuplicateGlobalLight();

            m_PreviousLightType = m_LightType;
        }

        private void OnDisable()
        {
            bool anyLightLeft = false;

            for (int i = 0; i < Light2DManager.lights.Length; ++i)
            {
                Light2DManager.lights[i].Remove(this);

                if (Light2DManager.lights[i].Count > 0)
                    anyLightLeft = true;
            }

            if (!anyLightLeft && Light2DManager.cullingGroup != null)
            {
                Light2DManager.cullingGroup.Dispose();
                Light2DManager.cullingGroup = null;
                RenderPipelineManager.beginCameraRendering -= SetupCulling;
            }
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
            for(int blendStyleIndex = 0; blendStyleIndex < Light2DManager.lights.Length; blendStyleIndex++)
            {
                List<Light2D> lights = Light2DManager.lights[blendStyleIndex];
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
            UpdateBlendStyle();

            bool rebuildMesh = false;

            // Sorting. InsertLight() will make sure the lights are sorted.
            if (LightUtility.CheckForChange(m_LightOrder, ref m_PreviousLightOrder))
            {
                Light2DManager.lights[(int)m_BlendStyleIndex].Remove(this);
                InsertLight();
            }

            if (m_LightType != m_PreviousLightType)
            {
                if (m_LightType == LightType.Global)
                    ErrorIfDuplicateGlobalLight();
                else
                    rebuildMesh = true;

                m_PreviousLightType = m_LightType;
            }

            // Mesh Rebuilding
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightFalloffSize, ref m_PreviousShapeLightFalloffSize);
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightParametricRadius, ref m_PreviousShapeLightParametricRadius);
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightParametricSides, ref m_PreviousShapeLightParametricSides);
            rebuildMesh |= LightUtility.CheckForChange(m_LightVolumeOpacity, ref m_PreviousLightVolumeOpacity);
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightParametricAngleOffset, ref m_PreviousShapeLightParametricAngleOffset);
            rebuildMesh |= LightUtility.CheckForChange(m_LightCookieSprite != null, ref m_PreviousLightCookieSpriteExists);
            rebuildMesh |= LightUtility.CheckForChange(m_LightCookieSprite, ref m_PreviousLightCookieSprite);
            rebuildMesh |= LightUtility.CheckForChange(m_ShapeLightFalloffOffset, ref m_PreviousShapeLightFalloffOffset);

#if UNITY_EDITOR
            rebuildMesh |= LightUtility.CheckForChange(LightUtility.GetShapePathHash(m_ShapePath), ref m_PreviousShapePathHash);
#endif
            if(rebuildMesh && m_LightType != LightType.Global)
                UpdateMesh();
        }


#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawIcon(transform.position, s_LightIconPaths[(int)m_LightType], true);
        }

        void Reset()
        {
            m_ShapePath = new Vector3[] { new Vector3(-0.5f, -0.5f), new Vector3(0.5f, -0.5f), new Vector3(0.5f, 0.5f), new Vector3(-0.5f, 0.5f) };
        }
#endif
    }
}
