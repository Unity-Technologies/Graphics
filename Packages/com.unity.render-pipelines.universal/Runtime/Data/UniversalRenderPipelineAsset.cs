using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using UnityEditorInternal;
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif
using System.ComponentModel;
using System.Linq;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
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

    public enum LightCookieResolution
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    public enum LightCookieFormat
    {
        GrayscaleLow,
        GrayscaleHigh,
        ColorLow,
        ColorHigh,
        ColorHDR,
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
        Sprite,
        UnityBuiltinDefault,
        SpriteMask,
        Decal
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
        [InspectorName("Only URP Shaders")]
        OnlyUniversalRPShaders,
        [InspectorName("All Shaders")]
        AllShaders
    }

    [Obsolete("PipelineDebugLevel is unused and has no effect.", false)]
    public enum PipelineDebugLevel
    {
        Disabled,
        Profiling,
    }

    public enum RendererType
    {
        Custom,
        UniversalRenderer,
        _2DRenderer,
        [Obsolete("ForwardRenderer has been renamed (UnityUpgradable) -> UniversalRenderer", true)]
        ForwardRenderer = UniversalRenderer,
    }

    public enum ColorGradingMode
    {
        LowDynamicRange,
        HighDynamicRange
    }

    /// <summary>
    /// Defines if Unity discards or stores the render targets of the DrawObjects Passes. Selecting the Store option significantly increases the memory bandwidth on mobile and tile-based GPUs.
    /// </summary>
    public enum StoreActionsOptimization
    {
        /// <summary>Unity uses the Discard option by default, and falls back to the Store option if it detects any injected Passes.</summary>
        Auto,
        /// <summary>Unity discards the render targets of render Passes that are not reused later (lower memory bandwidth).</summary>
        Discard,
        /// <summary>Unity stores all render targets of each Pass (higher memory bandwidth).</summary>
        Store
    }

    /// <summary>
    /// Defines the update frequency for the Volume Framework.
    /// </summary>
    public enum VolumeFrameworkUpdateMode
    {
        [InspectorName("Every Frame")]
        EveryFrame = 0,
        [InspectorName("Via Scripting")]
        ViaScripting = 1,
        [InspectorName("Use Pipeline Settings")]
        UsePipelineSettings = 2,
    }

    /// <summary>
    /// Defines the upscaling filter selected by the user the universal render pipeline asset.
    /// </summary>
    public enum UpscalingFilterSelection
    {
        [InspectorName("Automatic")]
        Auto,
        [InspectorName("Bilinear")]
        Linear,
        [InspectorName("Nearest-Neighbor")]
        Point,
        [InspectorName("FidelityFX Super Resolution 1.0")]
        FSR
    }

    [ExcludeFromPreset]
#if UNITY_EDITOR
    [ShaderKeywordFilter.ApplyRulesIfTagsEqual("RenderPipeline", "UniversalPipeline")]
#endif
    public partial class UniversalRenderPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        // Defaults for renderer features that are not dependent on other settings.
        // These are the filter rules if no such renderer features are present.

        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.ScreenSpaceOcclusion)]

        // TODO: decal settings needs some rework before we can filter DBufferMRT/DecalNormalBlend.
        // Atm the setup depends on the technique but settings are present for both at the same time.
        //[ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT2, ShaderKeywordStrings.DBufferMRT3})]
        //[ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] {ShaderKeywordStrings.DecalNormalBlendLow, ShaderKeywordStrings.DecalNormalBlendMedium, ShaderKeywordStrings.DecalNormalBlendHigh})]
        private const bool k_RendererFeatureDefaults = true;

        // Platform specific filtering overrides

        [ShaderKeywordFilter.ApplyRulesIfGraphicsAPI(GraphicsDeviceType.OpenGLES2)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.LightLayers)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.RenderPassEnabled)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] {ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen})]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] {ShaderKeywordStrings._DETAIL_MULX2, ShaderKeywordStrings._DETAIL_SCALED})]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] {ShaderKeywordStrings._CLEARCOAT, ShaderKeywordStrings._CLEARCOATMAP})]
        private const bool k_GLES2Defaults = true;

        [ShaderKeywordFilter.ApplyRulesIfGraphicsAPI(GraphicsDeviceType.OpenGLES2, GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        private const bool k_CommonGLDefaults = true;
#endif
        Shader m_DefaultShader;
        ScriptableRenderer[] m_Renderers = new ScriptableRenderer[1];

        // Default values set when a new UniversalRenderPipeline asset is created
        [SerializeField] int k_AssetVersion = 9;
        [SerializeField] int k_AssetPreviousVersion = 9;

        // Deprecated settings for upgrading sakes
        [SerializeField] RendererType m_RendererType = RendererType.UniversalRenderer;
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use m_RendererDataList instead.")]
        [SerializeField] internal ScriptableRendererData m_RendererData = null;

        // Renderer settings
        [SerializeField] internal ScriptableRendererData[] m_RendererDataList = new ScriptableRendererData[1];
        [SerializeField] internal int m_DefaultRendererIndex = 0;

        // General settings
        [SerializeField] bool m_RequireDepthTexture = false;
        [SerializeField] bool m_RequireOpaqueTexture = false;
        [SerializeField] Downsampling m_OpaqueDownsampling = Downsampling._2xBilinear;
        [SerializeField] bool m_SupportsTerrainHoles = true;
        [SerializeField] StoreActionsOptimization m_StoreActionsOptimization = StoreActionsOptimization.Auto;

        // Quality settings
        [SerializeField] bool m_SupportsHDR = true;
        [SerializeField] MsaaQuality m_MSAA = MsaaQuality.Disabled;
        [SerializeField] float m_RenderScale = 1.0f;
        [SerializeField] UpscalingFilterSelection m_UpscalingFilter = UpscalingFilterSelection.Auto;
        [SerializeField] bool m_FsrOverrideSharpness = false;
        [SerializeField] float m_FsrSharpness = FSRUtils.kDefaultSharpnessLinear;
        // TODO: Shader Quality Tiers

        // Main directional light Settings
        [SerializeField] LightRenderingMode m_MainLightRenderingMode = LightRenderingMode.PerPixel;

#if UNITY_EDITOR // multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
        // User can change cascade count at runtime so we have to include both MainLightShadows and MainLightShadowCascades.
        // ScreenSpaceShadows renderer feature has separate filter attribute for keeping MainLightShadowScreen.
        // NOTE: off variants are atm always removed when shadows are supported
        [ShaderKeywordFilter.SelectIf(true, keywordNames: new string[] {ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades})]
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: new string[] {ShaderKeywordStrings.MainLightShadows, ShaderKeywordStrings.MainLightShadowCascades, ShaderKeywordStrings.MainLightShadowScreen})]
#endif
        [SerializeField] bool m_MainLightShadowsSupported = true;
        [SerializeField] ShadowResolution m_MainLightShadowmapResolution = ShadowResolution._2048;

        // Additional lights settings
#if UNITY_EDITOR // multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
        // clustered renderer can override PerVertex/PerPixel to be disabled
        // NOTE: off variants are atm always kept when additional lights are enabled due to XR perf reasons
        [ShaderKeywordFilter.SelectIf(LightRenderingMode.PerVertex, keywordNames: new string[] {"", ShaderKeywordStrings.AdditionalLightsVertex})]
        [ShaderKeywordFilter.RemoveIf(LightRenderingMode.PerVertex, keywordNames: ShaderKeywordStrings.AdditionalLightShadows)]
        [ShaderKeywordFilter.SelectIf(LightRenderingMode.PerPixel, keywordNames: new string[] {"", ShaderKeywordStrings.AdditionalLightsPixel})]
        [ShaderKeywordFilter.RemoveIf(LightRenderingMode.Disabled, keywordNames: new string[] {ShaderKeywordStrings.AdditionalLightsVertex, ShaderKeywordStrings.AdditionalLightsPixel})]
#endif
        [SerializeField] LightRenderingMode m_AdditionalLightsRenderingMode = LightRenderingMode.PerPixel;
        [SerializeField] int m_AdditionalLightsPerObjectLimit = 4;
#if UNITY_EDITOR // multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.AdditionalLightShadows)]
#endif
        [SerializeField] bool m_AdditionalLightShadowsSupported = false;
        [SerializeField] ShadowResolution m_AdditionalLightsShadowmapResolution = ShadowResolution._2048;

        [SerializeField] int m_AdditionalLightsShadowResolutionTierLow = AdditionalLightsDefaultShadowResolutionTierLow;
        [SerializeField] int m_AdditionalLightsShadowResolutionTierMedium = AdditionalLightsDefaultShadowResolutionTierMedium;
        [SerializeField] int m_AdditionalLightsShadowResolutionTierHigh = AdditionalLightsDefaultShadowResolutionTierHigh;

        // Reflection Probes
#if UNITY_EDITOR // multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
        [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: ShaderKeywordStrings.ReflectionProbeBlending)]
#endif
        [SerializeField] bool m_ReflectionProbeBlending = false;
#if UNITY_EDITOR // multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
        [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: ShaderKeywordStrings.ReflectionProbeBoxProjection)]
#endif
        [SerializeField] bool m_ReflectionProbeBoxProjection = false;

        // Shadows Settings
        [SerializeField] float m_ShadowDistance = 50.0f;
        [SerializeField] int m_ShadowCascadeCount = 1;
        [SerializeField] float m_Cascade2Split = 0.25f;
        [SerializeField] Vector2 m_Cascade3Split = new Vector2(0.1f, 0.3f);
        [SerializeField] Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
        [SerializeField] float m_CascadeBorder = 0.2f;
        [SerializeField] float m_ShadowDepthBias = 1.0f;
        [SerializeField] float m_ShadowNormalBias = 1.0f;
#if UNITY_EDITOR // multi_compile_fragment _ _SHADOWS_SOFT
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.SoftShadows)]
        [SerializeField] bool m_AnyShadowsSupported = true;

        // No option to force soft shadows -> we'll need to keep the off variant around
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.SoftShadows)]
#endif
        [SerializeField] bool m_SoftShadowsSupported = false;
        [SerializeField] bool m_ConservativeEnclosingSphere = false;
        [SerializeField] int m_NumIterationsEnclosingSphere = 64;

        // Light Cookie Settings
        [SerializeField] LightCookieResolution m_AdditionalLightsCookieResolution = LightCookieResolution._2048;
        [SerializeField] LightCookieFormat m_AdditionalLightsCookieFormat = LightCookieFormat.ColorHigh;

        // Advanced settings
        [SerializeField] bool m_UseSRPBatcher = true;
        [SerializeField] bool m_SupportsDynamicBatching = false;
#if UNITY_EDITOR
        // multi_compile _ LIGHTMAP_SHADOW_MIXING
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.LightmapShadowMixing)]
        // multi_compile _ SHADOWS_SHADOWMASK
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.ShadowsShadowMask)]
#endif
        [SerializeField] bool m_MixedLightingSupported = true;
#if UNITY_EDITOR
        // multi_compile_fragment _ _LIGHT_LAYERS
        [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: ShaderKeywordStrings.LightLayers)]
        // TODO: Filtering WriteRenderingLayers requires different filter triggers for different passes (i.e. per-pass filter attributes)
#endif
        [SerializeField] bool m_SupportsLightLayers = false;
        [SerializeField] [Obsolete] PipelineDebugLevel m_DebugLevel;

        // Adaptive performance settings
        [SerializeField] bool m_UseAdaptivePerformance = true;

        // Post-processing settings
        [SerializeField] ColorGradingMode m_ColorGradingMode = ColorGradingMode.LowDynamicRange;
        [SerializeField] int m_ColorGradingLutSize = 32;
#if UNITY_EDITOR // multi_compile_local_fragment _ _USE_FAST_SRGB_LINEAR_CONVERSION
        [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: ShaderKeywordStrings.UseFastSRGBLinearConversion)]
#endif
        [SerializeField] bool m_UseFastSRGBLinearConversion = false;

        // Deprecated settings
        [SerializeField] ShadowQuality m_ShadowType = ShadowQuality.HardShadows;
        [SerializeField] bool m_LocalShadowsSupported = false;
        [SerializeField] ShadowResolution m_LocalShadowsAtlasResolution = ShadowResolution._256;
        [SerializeField] int m_MaxPixelLights = 0;
        [SerializeField] ShadowResolution m_ShadowAtlasResolution = ShadowResolution._256;

        [SerializeField] ShaderVariantLogLevel m_ShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;
        [SerializeField] VolumeFrameworkUpdateMode m_VolumeFrameworkUpdateMode = VolumeFrameworkUpdateMode.EveryFrame;

        // Note: A lut size of 16^3 is barely usable with the HDR grading mode. 32 should be the
        // minimum, the lut being encoded in log. Lower sizes would work better with an additional
        // 1D shaper lut but for now we'll keep it simple.
        public const int k_MinLutSize = 16;
        public const int k_MaxLutSize = 65;

        internal const int k_ShadowCascadeMinCount = 1;
        internal const int k_ShadowCascadeMaxCount = 4;

        public static readonly int AdditionalLightsDefaultShadowResolutionTierLow = 256;
        public static readonly int AdditionalLightsDefaultShadowResolutionTierMedium = 512;
        public static readonly int AdditionalLightsDefaultShadowResolutionTierHigh = 1024;

#if UNITY_EDITOR
        [NonSerialized]
        internal UniversalRenderPipelineEditorResources m_EditorResourcesAsset;

        public static readonly string packagePath = "Packages/com.unity.render-pipelines.universal";
        public static readonly string editorResourcesGUID = "a3d8d823eedde654bb4c11a1cfaf1abb";

        public static UniversalRenderPipelineAsset Create(ScriptableRendererData rendererData = null)
        {
            // Create Universal RP Asset
            var instance = CreateInstance<UniversalRenderPipelineAsset>();
            if (rendererData != null)
                instance.m_RendererDataList[0] = rendererData;
            else
                instance.m_RendererDataList[0] = CreateInstance<UniversalRendererData>();

            // Initialize default Renderer
            instance.m_EditorResourcesAsset = instance.editorResources;

            // Only enable for new URP assets by default
            instance.m_ConservativeEnclosingSphere = true;

            return instance;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateUniversalPipelineAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                //Create asset
                AssetDatabase.CreateAsset(Create(CreateRendererAsset(pathName, RendererType.UniversalRenderer)), pathName);
            }
        }

        [MenuItem("Assets/Create/Rendering/URP Asset (with Universal Renderer)", priority = CoreUtils.Sections.section2 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority + 1)]
        static void CreateUniversalPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateUniversalPipelineAsset>(),
                "New Universal Render Pipeline Asset.asset", null, null);
        }

        internal static ScriptableRendererData CreateRendererAsset(string path, RendererType type, bool relativePath = true, string suffix = "Renderer")
        {
            ScriptableRendererData data = CreateRendererData(type);
            string dataPath;
            if (relativePath)
                dataPath =
                    $"{Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path))}_{suffix}{Path.GetExtension(path)}";
            else
                dataPath = path;
            AssetDatabase.CreateAsset(data, dataPath);
            ResourceReloader.ReloadAllNullIn(data, packagePath);
            return data;
        }

        static ScriptableRendererData CreateRendererData(RendererType type)
        {
            switch (type)
            {
                case RendererType.UniversalRenderer:
                default:
                {
                    var rendererData = CreateInstance<UniversalRendererData>();
                    rendererData.postProcessData = PostProcessData.GetDefaultPostProcessData();
                    return rendererData;
                }
                // 2D renderer is experimental
                case RendererType._2DRenderer:
                {
                    var rendererData = CreateInstance<Renderer2DData>();
                    rendererData.postProcessData = PostProcessData.GetDefaultPostProcessData();
                    return rendererData;
                    // Universal Renderer is the fallback renderer that works on all platforms
                }
            }
        }

        // Hide: User aren't suppose to have to create it.
        //[MenuItem("Assets/Create/Rendering/URP Editor Resources", priority = CoreUtils.Sections.section8 + CoreUtils.Priorities.assetsCreateRenderingMenuPriority)]
        static void CreateUniversalPipelineEditorResources()
        {
            var instance = CreateInstance<UniversalRenderPipelineEditorResources>();
            ResourceReloader.ReloadAllNullIn(instance, packagePath);
            AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(UniversalRenderPipelineEditorResources).Name));
        }

        UniversalRenderPipelineEditorResources editorResources
        {
            get
            {
                if (m_EditorResourcesAsset != null && !m_EditorResourcesAsset.Equals(null))
                    return m_EditorResourcesAsset;

                string resourcePath = AssetDatabase.GUIDToAssetPath(editorResourcesGUID);
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
                m_EditorResourcesAsset = objs != null && objs.Length > 0 ? objs.First() as UniversalRenderPipelineEditorResources : null;
                return m_EditorResourcesAsset;
            }
        }
#endif

        public ScriptableRendererData LoadBuiltinRendererData(RendererType type = RendererType.UniversalRenderer)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            return m_RendererDataList[0] =
                CreateRendererAsset("Assets/UniversalRenderer.asset", type, false);
#else
            m_RendererDataList[0] = null;
            return m_RendererDataList[0];
#endif
        }

        protected override RenderPipeline CreatePipeline()
        {
            if (m_RendererDataList == null)
                m_RendererDataList = new ScriptableRendererData[1];

            // If no default data we can't create pipeline instance
            if (m_RendererDataList[m_DefaultRendererIndex] == null)
            {
                // If previous version and current version are miss-matched then we are waiting for the upgrader to kick in
                if (k_AssetPreviousVersion != k_AssetVersion)
                    return null;

                if (m_RendererDataList[m_DefaultRendererIndex].GetType().ToString()
                    .Contains("Universal.ForwardRendererData"))
                    return null;

                Debug.LogError(
                    $"Default Renderer is missing, make sure there is a Renderer assigned as the default on the current Universal RP asset:{UniversalRenderPipeline.asset.name}",
                    this);
                return null;
            }

            CreateRenderers();
            return new UniversalRenderPipeline(this);
        }

        internal void DestroyRenderers()
        {
            if (m_Renderers == null)
                return;

            for (int i = 0; i < m_Renderers.Length; i++)
                DestroyRenderer(ref m_Renderers[i]);
        }

        void DestroyRenderer(ref ScriptableRenderer renderer)
        {
            if (renderer != null)
            {
                renderer.Dispose();
                renderer = null;
            }
        }

        protected override void OnValidate()
        {
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnValidate();
        }

        protected override void OnDisable()
        {
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnDisable();
        }

        void CreateRenderers()
        {
            DestroyRenderers();

            if (m_Renderers == null || m_Renderers.Length != m_RendererDataList.Length)
                m_Renderers = new ScriptableRenderer[m_RendererDataList.Length];

            for (int i = 0; i < m_RendererDataList.Length; ++i)
            {
                if (m_RendererDataList[i] != null)
                    m_Renderers[i] = m_RendererDataList[i].InternalCreateRenderer();
            }
        }

        Material GetMaterial(DefaultMaterialType materialType)
        {
#if UNITY_EDITOR
            if (scriptableRendererData == null || editorResources == null)
                return null;

            var material = scriptableRendererData.GetDefaultMaterial(materialType);
            if (material != null)
                return material;

            switch (materialType)
            {
                case DefaultMaterialType.Standard:
                    return editorResources.materials.lit;

                case DefaultMaterialType.Particle:
                    return editorResources.materials.particleLit;

                case DefaultMaterialType.Terrain:
                    return editorResources.materials.terrainLit;

                case DefaultMaterialType.Decal:
                    return editorResources.materials.decal;

                // Unity Builtin Default
                default:
                    return null;
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Returns the default renderer being used by this pipeline.
        /// </summary>
        public ScriptableRenderer scriptableRenderer
        {
            get
            {
                if (m_RendererDataList?.Length > m_DefaultRendererIndex && m_RendererDataList[m_DefaultRendererIndex] == null)
                {
                    Debug.LogError("Default renderer is missing from the current Pipeline Asset.", this);
                    return null;
                }

                if (scriptableRendererData.isInvalidated || m_Renderers[m_DefaultRendererIndex] == null)
                {
                    DestroyRenderer(ref m_Renderers[m_DefaultRendererIndex]);
                    m_Renderers[m_DefaultRendererIndex] = scriptableRendererData.InternalCreateRenderer();
                }

                return m_Renderers[m_DefaultRendererIndex];
            }
        }

        /// <summary>
        /// Returns a renderer from the current pipeline asset
        /// </summary>
        /// <param name="index">Index to the renderer. If invalid index is passed, the default renderer is returned instead.</param>
        /// <returns></returns>
        public ScriptableRenderer GetRenderer(int index)
        {
            if (index == -1)
                index = m_DefaultRendererIndex;

            if (index >= m_RendererDataList.Length || index < 0 || m_RendererDataList[index] == null)
            {
                Debug.LogWarning(
                    $"Renderer at index {index.ToString()} is missing, falling back to Default Renderer {m_RendererDataList[m_DefaultRendererIndex].name}",
                    this);
                index = m_DefaultRendererIndex;
            }

            // RendererData list differs from RendererList. Create RendererList.
            if (m_Renderers == null || m_Renderers.Length < m_RendererDataList.Length)
                CreateRenderers();

            // This renderer data is outdated or invalid, we recreate the renderer
            // so we construct all render passes with the updated data
            if (m_RendererDataList[index].isInvalidated || m_Renderers[index] == null)
            {
                DestroyRenderer(ref m_Renderers[index]);
                m_Renderers[index] = m_RendererDataList[index].InternalCreateRenderer();
            }

            return m_Renderers[index];
        }

        internal ScriptableRendererData scriptableRendererData
        {
            get
            {
                if (m_RendererDataList[m_DefaultRendererIndex] == null)
                    CreatePipeline();

                return m_RendererDataList[m_DefaultRendererIndex];
            }
        }

#if UNITY_EDITOR
        internal GUIContent[] rendererDisplayList
        {
            get
            {
                GUIContent[] list = new GUIContent[m_RendererDataList.Length + 1];
                list[0] = new GUIContent($"Default Renderer ({RendererDataDisplayName(m_RendererDataList[m_DefaultRendererIndex])})");

                for (var i = 1; i < list.Length; i++)
                {
                    list[i] = new GUIContent($"{(i - 1).ToString()}: {RendererDataDisplayName(m_RendererDataList[i - 1])}");
                }
                return list;
            }
        }

        string RendererDataDisplayName(ScriptableRendererData data)
        {
            if (data != null)
                return data.name;

            return "NULL (Missing RendererData)";
        }

#endif
        private static GraphicsFormat[][] s_LightCookieFormatList = new GraphicsFormat[][]
        {
            /* Grayscale Low */ new GraphicsFormat[] {GraphicsFormat.R8_UNorm},
            /* Grayscale High*/ new GraphicsFormat[] {GraphicsFormat.R16_UNorm},
            /* Color Low     */ new GraphicsFormat[] {GraphicsFormat.R5G6B5_UNormPack16, GraphicsFormat.B5G6R5_UNormPack16, GraphicsFormat.R5G5B5A1_UNormPack16, GraphicsFormat.B5G5R5A1_UNormPack16},
            /* Color High    */ new GraphicsFormat[] {GraphicsFormat.A2B10G10R10_UNormPack32, GraphicsFormat.R8G8B8A8_SRGB, GraphicsFormat.B8G8R8A8_SRGB},
            /* Color HDR     */ new GraphicsFormat[] {GraphicsFormat.B10G11R11_UFloatPack32},
        };

        internal GraphicsFormat additionalLightsCookieFormat
        {
            get
            {
                GraphicsFormat result = GraphicsFormat.None;
                foreach (var format in s_LightCookieFormatList[(int)m_AdditionalLightsCookieFormat])
                {
                    if (SystemInfo.IsFormatSupported(format, FormatUsage.Render))
                    {
                        result = format;
                        break;
                    }
                }

                if (QualitySettings.activeColorSpace == ColorSpace.Gamma)
                    result = GraphicsFormatUtility.GetLinearFormat(result);

                // Fallback
                if (result == GraphicsFormat.None)
                {
                    result = GraphicsFormat.R8G8B8A8_UNorm;
                    Debug.LogWarning($"Additional Lights Cookie Format ({ m_AdditionalLightsCookieFormat.ToString() }) is not supported by the platform. Falling back to {GraphicsFormatUtility.GetBlockSize(result) * 8}-bit format ({GraphicsFormatUtility.GetFormatString(result)})");
                }

                return result;
            }
        }

        internal Vector2Int additionalLightsCookieResolution => new Vector2Int((int)m_AdditionalLightsCookieResolution, (int)m_AdditionalLightsCookieResolution);

        internal int[] rendererIndexList
        {
            get
            {
                int[] list = new int[m_RendererDataList.Length + 1];
                for (int i = 0; i < list.Length; i++)
                {
                    list[i] = i - 1;
                }
                return list;
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

        public bool supportsTerrainHoles
        {
            get { return m_SupportsTerrainHoles; }
        }

        /// <summary>
        /// Returns the active store action optimization value.
        /// </summary>
        /// <returns>Returns the active store action optimization value.</returns>
        public StoreActionsOptimization storeActionsOptimization
        {
            get { return m_StoreActionsOptimization; }
            set { m_StoreActionsOptimization = value; }
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

        /// <summary>
        /// Returns the upscaling filter desired by the user
        /// Note: Filter selections differ from actual filters in that they may include "meta-filters" such as
        ///       "Automatic" which resolve to an actual filter at a later time.
        /// </summary>
        public UpscalingFilterSelection upscalingFilter
        {
            get { return m_UpscalingFilter; }
            set { m_UpscalingFilter = value; }
        }

        /// <summary>
        /// If this property is set to true, the value from the fsrSharpness property will control the intensity of the
        /// sharpening filter associated with FidelityFX Super Resolution.
        /// </summary>
        public bool fsrOverrideSharpness
        {
            get { return m_FsrOverrideSharpness; }
            set { m_FsrOverrideSharpness = value; }
        }

        /// <summary>
        /// Controls the intensity of the sharpening filter associated with FidelityFX Super Resolution.
        /// A value of 1.0 produces maximum sharpness while a value of 0.0 disables the sharpening filter entirely.
        ///
        /// Note: This value only has an effect when the fsrOverrideSharpness property is set to true.
        /// </summary>
        public float fsrSharpness
        {
            get { return m_FsrSharpness; }
            set { m_FsrSharpness = value; }
        }

        public LightRenderingMode mainLightRenderingMode
        {
            get { return m_MainLightRenderingMode; }
            internal set { m_MainLightRenderingMode = value; }
        }

        public bool supportsMainLightShadows
        {
            get { return m_MainLightShadowsSupported; }
            internal set {
                m_MainLightShadowsSupported = value;
#if UNITY_EDITOR
                m_AnyShadowsSupported = m_MainLightShadowsSupported || m_AdditionalLightShadowsSupported;
#endif
            }
        }

        public int mainLightShadowmapResolution
        {
            get { return (int)m_MainLightShadowmapResolution; }
            internal set { m_MainLightShadowmapResolution = (ShadowResolution)value; }
        }

        public LightRenderingMode additionalLightsRenderingMode
        {
            get { return m_AdditionalLightsRenderingMode; }
            internal set { m_AdditionalLightsRenderingMode = value; }
        }

        public int maxAdditionalLightsCount
        {
            get { return m_AdditionalLightsPerObjectLimit; }
            set { m_AdditionalLightsPerObjectLimit = ValidatePerObjectLights(value); }
        }

        public bool supportsAdditionalLightShadows
        {
            get { return m_AdditionalLightShadowsSupported; }
            internal set {
                m_AdditionalLightShadowsSupported = value;
#if UNITY_EDITOR
                m_AnyShadowsSupported = m_MainLightShadowsSupported || m_AdditionalLightShadowsSupported;
#endif
            }
        }

        public int additionalLightsShadowmapResolution
        {
            get { return (int)m_AdditionalLightsShadowmapResolution; }
            internal set { m_AdditionalLightsShadowmapResolution = (ShadowResolution)value; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "Low" in the UniversalRenderPipeline asset.
        /// </summary>
        public int additionalLightsShadowResolutionTierLow
        {
            get { return (int)m_AdditionalLightsShadowResolutionTierLow; }
            internal set { m_AdditionalLightsShadowResolutionTierLow = value; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "Medium" in the UniversalRenderPipeline asset.
        /// </summary>
        public int additionalLightsShadowResolutionTierMedium
        {
            get { return (int)m_AdditionalLightsShadowResolutionTierMedium; }
            internal set { m_AdditionalLightsShadowResolutionTierMedium = value; }
        }

        /// <summary>
        /// Returns the additional light shadow resolution defined for tier "High" in the UniversalRenderPipeline asset.
        /// </summary>
        public int additionalLightsShadowResolutionTierHigh
        {
            get { return (int)m_AdditionalLightsShadowResolutionTierHigh; }
            internal set { m_AdditionalLightsShadowResolutionTierHigh = value; }
        }

        internal int GetAdditionalLightsShadowResolution(int additionalLightsShadowResolutionTier)
        {
            if (additionalLightsShadowResolutionTier <= UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow /* 0 */)
                return additionalLightsShadowResolutionTierLow;

            if (additionalLightsShadowResolutionTier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium /* 1 */)
                return additionalLightsShadowResolutionTierMedium;

            if (additionalLightsShadowResolutionTier >= UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh /* 2 */)
                return additionalLightsShadowResolutionTierHigh;

            return additionalLightsShadowResolutionTierMedium;
        }

        public bool reflectionProbeBlending
        {
            get { return m_ReflectionProbeBlending; }
            internal set { m_ReflectionProbeBlending = value; }
        }

        public bool reflectionProbeBoxProjection
        {
            get { return m_ReflectionProbeBoxProjection; }
            internal set { m_ReflectionProbeBoxProjection = value; }
        }

        /// <summary>
        /// Controls the maximum distance at which shadows are visible.
        /// </summary>
        public float shadowDistance
        {
            get { return m_ShadowDistance; }
            set { m_ShadowDistance = Mathf.Max(0.0f, value); }
        }

        /// <summary>
        /// Returns the number of shadow cascades.
        /// </summary>
        public int shadowCascadeCount
        {
            get { return m_ShadowCascadeCount; }
            set
            {
                if (value < k_ShadowCascadeMinCount || value > k_ShadowCascadeMaxCount)
                {
                    throw new ArgumentException($"Value ({value}) needs to be between {k_ShadowCascadeMinCount} and {k_ShadowCascadeMaxCount}.");
                }
                m_ShadowCascadeCount = value;
            }
        }

        /// <summary>
        /// Returns the split value.
        /// </summary>
        /// <returns>Returns a Float with the split value.</returns>
        public float cascade2Split
        {
            get { return m_Cascade2Split; }
            internal set { m_Cascade2Split = value; }
        }

        /// <summary>
        /// Returns the split values.
        /// </summary>
        /// <returns>Returns a Vector2 with the split values.</returns>
        public Vector2 cascade3Split
        {
            get { return m_Cascade3Split; }
            internal set { m_Cascade3Split = value; }
        }

        /// <summary>
        /// Returns the split values.
        /// </summary>
        /// <returns>Returns a Vector3 with the split values.</returns>
        public Vector3 cascade4Split
        {
            get { return m_Cascade4Split; }
            internal set { m_Cascade4Split = value; }
        }

        /// <summary>
        /// Last cascade fade distance in percentage.
        /// </summary>
        public float cascadeBorder
        {
            get { return m_CascadeBorder; }
            set { m_CascadeBorder = value; }
        }

        /// <summary>
        /// The Shadow Depth Bias, controls the offset of the lit pixels.
        /// </summary>
        public float shadowDepthBias
        {
            get { return m_ShadowDepthBias; }
            set { m_ShadowDepthBias = ValidateShadowBias(value); }
        }

        /// <summary>
        /// Controls the distance at which the shadow casting surfaces are shrunk along the surface normal.
        /// </summary>
        public float shadowNormalBias
        {
            get { return m_ShadowNormalBias; }
            set { m_ShadowNormalBias = ValidateShadowBias(value); }
        }

        /// <summary>
        /// Supports Soft Shadows controls the Soft Shadows.
        /// </summary>
        public bool supportsSoftShadows
        {
            get { return m_SoftShadowsSupported; }
            internal set { m_SoftShadowsSupported = value; }
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

        /// <summary>
        /// Returns true if the Render Pipeline Asset supports light layers, false otherwise.
        /// </summary>
        public bool supportsLightLayers
        {
            get { return m_SupportsLightLayers; }
        }

        public ShaderVariantLogLevel shaderVariantLogLevel
        {
            get { return m_ShaderVariantLogLevel; }
            set { m_ShaderVariantLogLevel = value; }
        }

        /// <summary>
        /// Returns the selected update mode for volumes.
        /// </summary>
        public VolumeFrameworkUpdateMode volumeFrameworkUpdateMode => m_VolumeFrameworkUpdateMode;

        [Obsolete("PipelineDebugLevel is deprecated. Calling debugLevel is not necessary.", false)]
        public PipelineDebugLevel debugLevel
        {
            get => PipelineDebugLevel.Disabled;
        }

        public bool useSRPBatcher
        {
            get { return m_UseSRPBatcher; }
            set { m_UseSRPBatcher = value; }
        }

        public ColorGradingMode colorGradingMode
        {
            get { return m_ColorGradingMode; }
            set { m_ColorGradingMode = value; }
        }

        public int colorGradingLutSize
        {
            get { return m_ColorGradingLutSize; }
            set { m_ColorGradingLutSize = Mathf.Clamp(value, k_MinLutSize, k_MaxLutSize); }
        }

        /// <summary>
        /// Returns true if fast approximation functions are used when converting between the sRGB and Linear color spaces, false otherwise.
        /// </summary>
        public bool useFastSRGBLinearConversion
        {
            get { return m_UseFastSRGBLinearConversion; }
        }

        /// <summary>
        /// Set to true to allow Adaptive performance to modify graphics quality settings during runtime.
        /// Only applicable when Adaptive performance package is available.
        /// </summary>
        public bool useAdaptivePerformance
        {
            get { return m_UseAdaptivePerformance; }
            set { m_UseAdaptivePerformance = value; }
        }

        /// <summary>
        /// Set to true to enable a conservative method for calculating the size and position of the minimal enclosing sphere around the frustum cascade corner points for shadow culling.
        /// </summary>
        public bool conservativeEnclosingSphere
        {
            get { return m_ConservativeEnclosingSphere; }
            set { m_ConservativeEnclosingSphere = value; }
        }

        /// <summary>
        /// Set the number of iterations to reduce the cascade culling enlcosing sphere to be closer to the absolute minimun enclosing sphere, but will also require more CPU computation for increasing values.
        /// This parameter is used only when conservativeEnclosingSphere is set to true. Default value is 64.
        /// </summary>
        public int numIterationsEnclosingSphere
        {
            get { return m_NumIterationsEnclosingSphere; }
            set { m_NumIterationsEnclosingSphere = value; }
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

        /// <summary>
        /// Returns the default material for the 2D renderer.
        /// </summary>
        /// <returns>Returns the material containing the default lit and unlit shader passes for sprites in the 2D renderer.</returns>
        public override Material default2DMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Sprite); }
        }

        /// <summary>
        /// Returns the default sprite mask material for the 2D renderer.
        /// </summary>
        /// <returns>Returns the material containing the default shader pass for sprite mask in the 2D renderer.</returns>
        public override Material default2DMaskMaterial
        {
            get { return GetMaterial(DefaultMaterialType.SpriteMask); }
        }

        public Material decalMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Decal); }
        }

        public override Shader defaultShader
        {
            get
            {
#if UNITY_EDITOR
                // TODO: When importing project, AssetPreviewUpdater:CreatePreviewForAsset will be called multiple time
                // which in turns calls this property to get the default shader.
                // The property should never return null as, when null, it loads the data using AssetDatabase.LoadAssetAtPath.
                // However it seems there's an issue that LoadAssetAtPath will not load the asset in some cases. so adding the null check
                // here to fix template tests.
                if (scriptableRendererData != null)
                {
                    Shader defaultShader = scriptableRendererData.GetDefaultShader();
                    if (defaultShader != null)
                        return defaultShader;
                }

                if (m_DefaultShader == null)
                {
                    string path = AssetDatabase.GUIDToAssetPath(ShaderUtils.GetShaderGUID(ShaderPathID.Lit));
                    m_DefaultShader  = AssetDatabase.LoadAssetAtPath<Shader>(path);
                }
#endif

                if (m_DefaultShader == null)
                    m_DefaultShader = Shader.Find(ShaderUtils.GetShaderPath(ShaderPathID.Lit));

                return m_DefaultShader;
            }
        }

#if UNITY_EDITOR
        public override Shader autodeskInteractiveShader
        {
            get { return editorResources?.shaders.autodeskInteractivePS; }
        }

        public override Shader autodeskInteractiveTransparentShader
        {
            get { return editorResources?.shaders.autodeskInteractiveTransparentPS; }
        }

        public override Shader autodeskInteractiveMaskedShader
        {
            get { return editorResources?.shaders.autodeskInteractiveMaskedPS; }
        }

        public override Shader terrainDetailLitShader
        {
            get { return editorResources?.shaders.terrainDetailLitPS; }
        }

        public override Shader terrainDetailGrassShader
        {
            get { return editorResources?.shaders.terrainDetailGrassPS; }
        }

        public override Shader terrainDetailGrassBillboardShader
        {
            get { return editorResources?.shaders.terrainDetailGrassBillboardPS; }
        }

        public override Shader defaultSpeedTree7Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree7PS; }
        }

        public override Shader defaultSpeedTree8Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree8PS; }
        }
#endif

        /// <summary>Names used for display of rendering layer masks.</summary>
        public override string[] renderingLayerMaskNames => UniversalRenderPipelineGlobalSettings.instance.renderingLayerMaskNames;

        /// <summary>Names used for display of rendering layer masks with prefix.</summary>
        public override string[] prefixedRenderingLayerMaskNames => UniversalRenderPipelineGlobalSettings.instance.prefixedRenderingLayerMaskNames;

        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        public string[] lightLayerMaskNames => UniversalRenderPipelineGlobalSettings.instance.lightLayerNames;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (k_AssetVersion < 3)
            {
                m_SoftShadowsSupported = (m_ShadowType == ShadowQuality.SoftShadows);
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 3;
            }

            if (k_AssetVersion < 4)
            {
                m_AdditionalLightShadowsSupported = m_LocalShadowsSupported;
                m_AdditionalLightsShadowmapResolution = m_LocalShadowsAtlasResolution;
                m_AdditionalLightsPerObjectLimit = m_MaxPixelLights;
                m_MainLightShadowmapResolution = m_ShadowAtlasResolution;
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 4;
            }

            if (k_AssetVersion < 5)
            {
                if (m_RendererType == RendererType.Custom)
                {
#pragma warning disable 618 // Obsolete warning
                    m_RendererDataList[0] = m_RendererData;
#pragma warning restore 618 // Obsolete warning
                }
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 5;
            }

            if (k_AssetVersion < 6)
            {
#pragma warning disable 618 // Obsolete warning
                // Adding an upgrade here so that if it was previously set to 2 it meant 4 cascades.
                // So adding a 3rd cascade shifted this value up 1.
                int value = (int)m_ShadowCascades;
                if (value == 2)
                {
                    m_ShadowCascadeCount = 4;
                }
                else
                {
                    m_ShadowCascadeCount = value + 1;
                }
                k_AssetVersion = 6;
#pragma warning restore 618 // Obsolete warning
            }

            if (k_AssetVersion < 7)
            {
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 7;
            }

            if (k_AssetVersion < 8)
            {
                k_AssetPreviousVersion = k_AssetVersion;
                m_CascadeBorder = 0.1f; // In previous version we had this hard coded
                k_AssetVersion = 8;
            }

            if (k_AssetVersion < 9)
            {
                bool assetContainsCustomAdditionalLightShadowResolutions =
                    m_AdditionalLightsShadowResolutionTierHigh != AdditionalLightsDefaultShadowResolutionTierHigh ||
                    m_AdditionalLightsShadowResolutionTierMedium != AdditionalLightsDefaultShadowResolutionTierMedium ||
                    m_AdditionalLightsShadowResolutionTierLow != AdditionalLightsDefaultShadowResolutionTierLow;

                if (!assetContainsCustomAdditionalLightShadowResolutions)
                {
                    // if all resolutions are still the default values, we assume that they have never been customized and that it is safe to upgrade them to fit better the Additional Lights Shadow Atlas size
                    m_AdditionalLightsShadowResolutionTierHigh = (int)m_AdditionalLightsShadowmapResolution;
                    m_AdditionalLightsShadowResolutionTierMedium = Mathf.Max(m_AdditionalLightsShadowResolutionTierHigh / 2, UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution);
                    m_AdditionalLightsShadowResolutionTierLow = Mathf.Max(m_AdditionalLightsShadowResolutionTierMedium / 2, UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution);
                }

                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 9;
            }

#if UNITY_EDITOR
            if (k_AssetPreviousVersion != k_AssetVersion)
            {
                EditorApplication.delayCall += () => UpgradeAsset(this.GetInstanceID());
            }
#endif
        }

#if UNITY_EDITOR
        static void UpgradeAsset(int assetInstanceID)
        {
            UniversalRenderPipelineAsset asset = EditorUtility.InstanceIDToObject(assetInstanceID) as UniversalRenderPipelineAsset;

            if (asset.k_AssetPreviousVersion < 5)
            {
                if (asset.m_RendererType == RendererType.UniversalRenderer)
                {
                    var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/UniversalRenderer.asset");
                    if (data)
                    {
                        asset.m_RendererDataList[0] = data;
                    }
                    else
                    {
                        asset.LoadBuiltinRendererData();
                    }
#pragma warning disable 618 // Obsolete warning
                    asset.m_RendererData = null; // Clears the old renderer
#pragma warning restore 618 // Obsolete warning
                }

                asset.k_AssetPreviousVersion = 5;
            }

            if (asset.k_AssetPreviousVersion < 9)
            {
                // The added feature was reverted, we keep this version to avoid breakage in case somebody already has version 7
                asset.k_AssetPreviousVersion = 9;
            }

            EditorUtility.SetDirty(asset);
        }

#endif

        float ValidateShadowBias(float value)
        {
            return Mathf.Max(0.0f, Mathf.Min(value, UniversalRenderPipeline.maxShadowBias));
        }

        int ValidatePerObjectLights(int value)
        {
            return System.Math.Max(0, System.Math.Min(value, UniversalRenderPipeline.maxPerObjectLights));
        }

        float ValidateRenderScale(float value)
        {
            return Mathf.Max(UniversalRenderPipeline.minRenderScale, Mathf.Min(value, UniversalRenderPipeline.maxRenderScale));
        }

        /// <summary>
        /// Check to see if the RendererData list contains valid RendererData references.
        /// </summary>
        /// <param name="partial">This bool controls whether to test against all or any, if false then there has to be no invalid RendererData</param>
        /// <returns></returns>
        internal bool ValidateRendererDataList(bool partial = false)
        {
            var emptyEntries = 0;
            for (int i = 0; i < m_RendererDataList.Length; i++) emptyEntries += ValidateRendererData(i) ? 0 : 1;
            if (partial)
                return emptyEntries == 0;
            return emptyEntries != m_RendererDataList.Length;
        }

        internal bool ValidateRendererData(int index)
        {
            // Check to see if you are asking for the default renderer
            if (index == -1) index = m_DefaultRendererIndex;
            return index < m_RendererDataList.Length ? m_RendererDataList[index] != null : false;
        }
    }
}
