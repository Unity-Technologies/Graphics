#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif

namespace UnityEngine.Rendering.LWRP
{
    public enum ShadowCascadesOption
    {
        NoCascades,
        TwoCascades,
        FourCascades,
    }

    public enum ShadowQuality
    {
        Disabled,
        HardShadows,
        SoftShadows,
    }

    public enum ShadowResolution
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    public enum MsaaQuality
    {
        Disabled = 1,
        _2x = 2,
        _4x = 4,
        _8x = 8
    }

    public enum Downsampling
    {
        None,
        _2xBilinear,
        _4xBox,
        _4xBilinear
    }

    internal enum DefaultMaterialType
    {
        Standard,
        Particle,
        Terrain,
        UnityBuiltinDefault
    }

    public enum LightRenderingMode
    {
        Disabled = 0,
        PerVertex = 2,
        PerPixel = 1,
    }

    public enum ShaderVariantLogLevel
    {
        Disabled,
        OnlyLightweightRPShaders,
        AllShaders,
    }

    public enum RendererType
    {
        Custom,
        ForwardRenderer,
    }

    public class LightweightRenderPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
    {
        Shader m_DefaultShader;
        internal ScriptableRenderer m_Renderer;

        // Default values set when a new LightweightRenderPipeline asset is created
        [SerializeField] int k_AssetVersion = 4;

        [SerializeField] RendererType m_RendererType = RendererType.ForwardRenderer;
        [SerializeField] internal ScriptableRendererData m_RendererData = null;
        
        // General settings
        [SerializeField] bool m_RequireDepthTexture = false;
        [SerializeField] bool m_RequireOpaqueTexture = false;
        [SerializeField] Downsampling m_OpaqueDownsampling = Downsampling._2xBilinear;

        // Quality settings
        [SerializeField] bool m_SupportsHDR = false;
        [SerializeField] MsaaQuality m_MSAA = MsaaQuality.Disabled;
        [SerializeField] float m_RenderScale = 1.0f;
        // TODO: Shader Quality Tiers

        // Main directional light Settings
        [SerializeField] LightRenderingMode m_MainLightRenderingMode = LightRenderingMode.PerPixel;
        [SerializeField] bool m_MainLightShadowsSupported = true;
        [SerializeField] ShadowResolution m_MainLightShadowmapResolution = ShadowResolution._2048;

        // Additional lights settings
        [SerializeField] LightRenderingMode m_AdditionalLightsRenderingMode = LightRenderingMode.PerPixel;
        [SerializeField] int m_AdditionalLightsPerObjectLimit = 4;
        [SerializeField] bool m_AdditionalLightShadowsSupported = false;
        [SerializeField] ShadowResolution m_AdditionalLightsShadowmapResolution = ShadowResolution._512;

        // Shadows Settings
        [SerializeField] float m_ShadowDistance = 50.0f;
        [SerializeField] ShadowCascadesOption m_ShadowCascades = ShadowCascadesOption.NoCascades;
        [SerializeField] float m_Cascade2Split = 0.25f;
        [SerializeField] Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
        [SerializeField] float m_ShadowDepthBias = 1.0f;
        [SerializeField] float m_ShadowNormalBias = 1.0f;
        [SerializeField] bool m_SoftShadowsSupported = false;

        // Advanced settings
        [SerializeField] bool m_UseSRPBatcher = true;
        [SerializeField] bool m_SupportsDynamicBatching = false;
        [SerializeField] bool m_MixedLightingSupported = true;

        // Deprecated settings
        [SerializeField] ShadowQuality m_ShadowType = ShadowQuality.HardShadows;
        [SerializeField] bool m_LocalShadowsSupported = false;
        [SerializeField] ShadowResolution m_LocalShadowsAtlasResolution = ShadowResolution._256;
        [SerializeField] int m_MaxPixelLights = 0;
        [SerializeField] ShadowResolution m_ShadowAtlasResolution = ShadowResolution._256;

        [SerializeField] ShaderVariantLogLevel m_ShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;

#if UNITY_EDITOR
        [NonSerialized]
        internal LightweightRenderPipelineEditorResources m_EditorResourcesAsset;

        static readonly string s_SearchPathProject = "Assets";
        static readonly string s_SearchPathPackage = "Packages/com.unity.render-pipelines.lightweight";

        public static LightweightRenderPipelineAsset Create()
        {
            var instance = CreateInstance<LightweightRenderPipelineAsset>();

            instance.LoadBuiltinRendererData();
            instance.m_EditorResourcesAsset = LoadResourceFile<LightweightRenderPipelineEditorResources>();
            return instance;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateLightweightPipelineAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                AssetDatabase.CreateAsset(Create(), pathName);
            }
        }

        [MenuItem("Assets/Create/Rendering/Lightweight Render Pipeline/Pipeline Asset", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateLightweightPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateLightweightPipelineAsset>(),
                "LightweightRenderPipelineAsset.asset", null, null);
        }

        //[MenuItem("Assets/Create/Rendering/Lightweight Pipeline Editor Resources", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateLightweightPipelineEditorResources()
        {
            var instance = CreateInstance<LightweightRenderPipelineEditorResources>();
            AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(LightweightRenderPipelineEditorResources).Name));
        }

        static T LoadResourceFile<T>() where T : ScriptableObject
        {
            T resourceAsset = null;
            var guids = AssetDatabase.FindAssets(typeof(T).Name + " t:scriptableobject", new[] {s_SearchPathProject});
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                resourceAsset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (resourceAsset != null)
                    break;
            }

            // There's currently an issue that prevents FindAssets from find resources withing the package folder.
            if (resourceAsset == null)
            {
                string path = s_SearchPathPackage + "/Runtime/Data/" + typeof(T).Name + ".asset";
                resourceAsset = AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return resourceAsset;
        }

        LightweightRenderPipelineEditorResources editorResources
        {
            get
            {
                if (m_EditorResourcesAsset == null)
                    m_EditorResourcesAsset = LoadResourceFile<LightweightRenderPipelineEditorResources>();

                return m_EditorResourcesAsset;
            }
        }
#endif

        public ScriptableRendererData LoadBuiltinRendererData()
        {
            switch (m_RendererType)
            {
                // Forward Renderer is the fallback renderer that works on all platforms
                default:
#if UNITY_EDITOR
                    m_RendererData = LoadResourceFile<ForwardRendererData>();
#else
                    m_RendererData = null;
#endif
                    break;
            }

            return m_RendererData;
        }

        protected override RenderPipeline CreatePipeline()
        {
            if (m_RendererData == null)
                LoadBuiltinRendererData();

            // If no data we can't create pipeline instance
            if (m_RendererData == null)
                return null;

            m_Renderer = m_RendererData.InternalCreateRenderer();
            return new LightweightRenderPipeline(this);
        }

        Material GetMaterial(DefaultMaterialType materialType)
        {
#if UNITY_EDITOR
            if (editorResources == null)
                return null;

            switch (materialType)
            {
                case DefaultMaterialType.Standard:
                    return editorResources.litMaterial;

                case DefaultMaterialType.Particle:
                    return editorResources.particleLitMaterial;

                case DefaultMaterialType.Terrain:
                    return editorResources.terrainLitMaterial;

                // Unity Builtin Default
                default:
                    return null;
            }
#else
            return null;
#endif
        }

        public ScriptableRenderer scriptableRenderer
        {
            get
            {
                if (m_RendererData == null)
                    CreatePipeline();

                if (m_RendererData.isInvalidated || m_Renderer == null)
                    m_Renderer = m_RendererData.InternalCreateRenderer();

                return m_Renderer;
            }
        }

        public bool supportsCameraDepthTexture
        {
            get { return m_RequireDepthTexture; }
            set { m_RequireDepthTexture = value; }
        }

        public bool supportsCameraOpaqueTexture
        {
            get { return m_RequireOpaqueTexture; }
            set { m_RequireOpaqueTexture = value; }
        }

        public Downsampling opaqueDownsampling
        {
            get { return m_OpaqueDownsampling; }
        }

        public bool supportsHDR
        {
            get { return m_SupportsHDR; }
            set { m_SupportsHDR = value; }
        }

        public int msaaSampleCount
        {
            get { return (int)m_MSAA; }
            set { m_MSAA = (MsaaQuality)value; }
        }

        public float renderScale
        {
            get { return m_RenderScale; }
            set { m_RenderScale = ValidateRenderScale(value); }
        }

        public LightRenderingMode mainLightRenderingMode
        {
            get { return m_MainLightRenderingMode; }
        }

        public bool supportsMainLightShadows
        {
            get { return m_MainLightShadowsSupported; }
        }

        public int mainLightShadowmapResolution
        {
            get { return (int)m_MainLightShadowmapResolution; }
        }

        public LightRenderingMode additionalLightsRenderingMode
        {
            get { return m_AdditionalLightsRenderingMode; }
        }

        public int maxAdditionalLightsCount
        {
            get { return m_AdditionalLightsPerObjectLimit; }
            set { m_AdditionalLightsPerObjectLimit = ValidatePerObjectLights(value); }
        }

        public bool supportsAdditionalLightShadows
        {
            get { return m_AdditionalLightShadowsSupported; }
        }

        public int additionalLightsShadowmapResolution
        {
            get { return (int)m_AdditionalLightsShadowmapResolution; }
        }

        public float shadowDistance
        {
            get { return m_ShadowDistance; }
            set { m_ShadowDistance = Mathf.Max(0.0f, value); }
        }

        public ShadowCascadesOption shadowCascadeOption
        {
            get { return m_ShadowCascades; }
            set { m_ShadowCascades = value; }
        }

        public float cascade2Split
        {
            get { return m_Cascade2Split; }
        }

        public Vector3 cascade4Split
        {
            get { return m_Cascade4Split; }
        }

        public float shadowDepthBias
        {
            get { return m_ShadowDepthBias; }
            set { m_ShadowDepthBias = ValidateShadowBias(value); }
        }

        public float shadowNormalBias
        {
            get { return m_ShadowNormalBias; }
            set { m_ShadowNormalBias = ValidateShadowBias(value); }
        }

        public bool supportsSoftShadows
        {
            get { return m_SoftShadowsSupported; }
        }

        public bool supportsDynamicBatching
        {
            get { return m_SupportsDynamicBatching; }
            set { m_SupportsDynamicBatching = value; }
        }

        public bool supportsMixedLighting
        {
            get { return m_MixedLightingSupported; }
        }

        public ShaderVariantLogLevel shaderVariantLogLevel
        {
            get { return m_ShaderVariantLogLevel; }
            set { m_ShaderVariantLogLevel = value; }
        }
        
        public bool useSRPBatcher
        {
            get { return m_UseSRPBatcher; }
            set { m_UseSRPBatcher = value; }
        }

        public override Material defaultMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Standard); }
        }

        public override Material defaultParticleMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        public override Material defaultLineMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        public override Material defaultTerrainMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Terrain); }
        }

        public override Material defaultUIMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Material defaultUIOverdrawMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Material defaultUIETC1SupportedMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Material default2DMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Shader defaultShader
        {
            get
            {
                if (m_DefaultShader == null)
                    m_DefaultShader = Shader.Find(ShaderUtils.GetShaderPath(ShaderPathID.Lit));
                return m_DefaultShader;
            }
        }

#if UNITY_EDITOR
        public override Shader autodeskInteractiveShader
        {
            get { return editorResources.autodeskInteractiveShader; }
        }

        public override Shader autodeskInteractiveTransparentShader
        {
            get { return editorResources.autodeskInteractiveTransparentShader; }
        }

        public override Shader autodeskInteractiveMaskedShader
        {
            get { return editorResources.autodeskInteractiveMaskedShader; }
        }

        public override Shader terrainDetailLitShader
        {
            get { return editorResources.terrainDetailLitShader; }
        }

        public override Shader terrainDetailGrassShader
        {
            get { return editorResources.terrainDetailGrassShader; }
        }

        public override Shader terrainDetailGrassBillboardShader
        {
            get { return editorResources.terrainDetailGrassBillboardShader; }
        }
#endif

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (k_AssetVersion < 3)
            {
                k_AssetVersion = 3;
                m_SoftShadowsSupported = (m_ShadowType == ShadowQuality.SoftShadows);
            }

            if (k_AssetVersion < 4)
            {
                k_AssetVersion = 4;
                m_AdditionalLightShadowsSupported = m_LocalShadowsSupported;
                m_AdditionalLightsShadowmapResolution = m_LocalShadowsAtlasResolution;
                m_AdditionalLightsPerObjectLimit = m_MaxPixelLights;
                m_MainLightShadowmapResolution = m_ShadowAtlasResolution;
            }
        }

        float ValidateShadowBias(float value)
        {
            return Mathf.Max(0.0f, Mathf.Min(value, LightweightRenderPipeline.maxShadowBias));
        }

        int ValidatePerObjectLights(int value)
        {
            return System.Math.Max(0, System.Math.Min(value, LightweightRenderPipeline.maxPerObjectLights));
        }

        float ValidateRenderScale(float value)
        {
            return Mathf.Max(LightweightRenderPipeline.minRenderScale, Mathf.Min(value, LightweightRenderPipeline.maxRenderScale));
        }
    }
}
