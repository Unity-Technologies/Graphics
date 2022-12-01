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
using UnityEditor.Rendering;
using UnityEngine.Serialization;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// The elements in this enum define how Unity renders shadows.
    /// </summary>
    public enum ShadowQuality
    {
        /// <summary>
        /// Disables the shadows.
        /// </summary>
        Disabled,
        /// <summary>
        /// Shadows have hard edges.
        /// </summary>
        HardShadows,
        /// <summary>
        /// Filtering is applied when sampling shadows. Shadows have smooth edges.
        /// </summary>
        SoftShadows,
    }

    /// <summary>
    /// Softness quality of soft shadows. Higher means better quality, but lower performance.
    /// </summary>
    public enum SoftShadowQuality
    {
        /// <summary>
        /// Use this to choose the setting set on the pipeline asset.
        /// </summary>
        [InspectorName("Use settings from Render Pipeline Asset")]
        UsePipelineSettings,

        /// <summary>
        /// Low quality soft shadows. Recommended for mobile. 4 PCF sample filtering.
        /// </summary>
        Low,
        /// <summary>
        /// Medium quality soft shadows. The default. 5x5 tent filtering.
        /// </summary>
        Medium,
        /// <summary>
        /// High quality soft shadows. Low performance due to high sample count. 7x7 tent filtering.
        /// </summary>
        High,
    }

    /// <summary>
    /// This controls the size of the shadow map texture.
    /// </summary>
    public enum ShadowResolution
    {
        /// <summary>
        /// Use this for 256x256 shadow resolution.
        /// </summary>
        _256 = 256,

        /// <summary>
        /// Use this for 512x512 shadow resolution.
        /// </summary>
        _512 = 512,

        /// <summary>
        /// Use this for 1024x1024 shadow resolution.
        /// </summary>
        _1024 = 1024,

        /// <summary>
        /// Use this for 2048x2048 shadow resolution.
        /// </summary>
        _2048 = 2048,

        /// <summary>
        /// Use this for 4096x4096 shadow resolution.
        /// </summary>
        _4096 = 4096
    }

    /// <summary>
    /// This controls the size of the Light Cookie atlas texture for additional lights (point, spot).
    /// </summary>
    public enum LightCookieResolution
    {
        /// <summary>
        /// Use this for 256x256 Light Cookie resolution.
        /// </summary>
        _256 = 256,

        /// <summary>
        /// Use this for 512x512 Light Cookie resolution.
        /// </summary>
        _512 = 512,

        /// <summary>
        /// Use this for 1024x1024 Light Cookie resolution.
        /// </summary>
        _1024 = 1024,

        /// <summary>
        /// Use this for 2048x2048 Light Cookie resolution.
        /// </summary>
        _2048 = 2048,

        /// <summary>
        /// Use this for 4096x4096 Light Cookie resolution.
        /// </summary>
        _4096 = 4096
    }

    /// <summary>
    /// Options for selecting the format for the Light Cookie atlas texture for additional lights (point, spot).
    /// Low precision saves memory and bandwidth.
    /// </summary>
    public enum LightCookieFormat
    {
        /// <summary>
        /// Use this to select Grayscale format with low precision.
        /// </summary>
        GrayscaleLow,

        /// <summary>
        /// Use this to select Grayscale format with high precision.
        /// </summary>
        GrayscaleHigh,

        /// <summary>
        /// Use this to select Color format with low precision.
        /// </summary>
        ColorLow,

        /// <summary>
        /// Use this to select Color format with high precision.
        /// </summary>
        ColorHigh,

        /// <summary>
        /// Use this to select High Dynamic Range format.
        /// </summary>
        ColorHDR,
    }

    /// <summary>
    /// The default color buffer format in HDR (only).
    /// Affects camera rendering and postprocessing color buffers.
    /// </summary>
    public enum HDRColorBufferPrecision
    {
        /// <summary> Typically R11G11B10f for faster rendering. Recommend for mobile.
        /// R11G11B10f can cause a subtle blue/yellow banding in some rare cases due to lower precision of the blue component.</summary>
        [Tooltip("Use 32-bits per pixel for HDR rendering.")]
        _32Bits,
        /// <summary>Typically R16G16B16A16f for better quality. Can reduce banding at the cost of memory and performance.</summary>
        [Tooltip("Use 64-bits per pixel for HDR rendering.")]
        _64Bits,
    }

    /// <summary>
    /// Options for setting MSAA Quality.
    /// This defines how many samples URP computes per pixel for evaluating the effect.
    /// </summary>
    public enum MsaaQuality
    {
        /// <summary>
        /// Disables MSAA.
        /// </summary>
        Disabled = 1,

        /// <summary>
        /// Use this for 2 samples per pixel.
        /// </summary>
        _2x = 2,

        /// <summary>
        /// Use this for 4 samples per pixel.
        /// </summary>
        _4x = 4,

        /// <summary>
        /// Use this for 8 samples per pixel.
        /// </summary>
        _8x = 8
    }

    /// <summary>
    /// Options for selecting downsampling.
    /// </summary>
    public enum Downsampling
    {
        /// <summary>
        /// Use this to disable downsampling.
        /// </summary>
        None,

        /// <summary>
        /// Use this to produce a half-resolution image with bilinear filtering.
        /// </summary>
        _2xBilinear,

        /// <summary>
        /// Use this to produce a quarter-resolution image with box filtering. This produces a softly blurred copy.
        /// </summary>
        _4xBox,

        /// <summary>
        /// Use this to produce a quarter-resolution image with bi-linear filtering.
        /// </summary>
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

    /// <summary>
    /// Options for light rendering mode.
    /// </summary>
    public enum LightRenderingMode
    {
        /// <summary>
        /// Use this to disable lighting.
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// Use this to select lighting to be calculated per vertex.
        /// </summary>
        PerVertex = 2,

        /// <summary>
        /// Use this to select lighting to be calculated per pixel.
        /// </summary>
        PerPixel = 1,
    }

    /// <summary>
    /// Defines if profiling is logged or not. This enum is not longer in use, use the Profiler instead.
    /// </summary>
    [Obsolete("PipelineDebugLevel is replaced to use the profiler and has no effect.", false)]
    public enum PipelineDebugLevel
    {
        /// <summary>
        /// Disabled logging for profiling.
        /// </summary>
        Disabled,
        /// <summary>
        /// Enabled logging for profiling.
        /// </summary>
        Profiling,
    }

    /// <summary>
    /// Options to select the type of Renderer to use.
    /// </summary>
    public enum RendererType
    {
        /// <summary>
        /// Use this for Custom Renderer.
        /// </summary>
        Custom,

        /// <summary>
        /// Use this for Universal Renderer.
        /// </summary>
        UniversalRenderer,

        /// <summary>
        /// Use this for 2D Renderer.
        /// </summary>
        _2DRenderer,
        /// <summary>
        /// This name was used before the Universal Renderer was implemented.
        /// </summary>
        [Obsolete("ForwardRenderer has been renamed (UnityUpgradable) -> UniversalRenderer", true)]
        ForwardRenderer = UniversalRenderer,
    }

    /// <summary>
    /// Options for selecting Color Grading modes, Low Dynamic Range (LDR) or High Dynamic Range (HDR)
    /// </summary>
    public enum ColorGradingMode
    {
        /// <summary>
        /// This mode follows a more classic workflow. Unity applies a limited range of color grading after tonemapping.
        /// </summary>
        LowDynamicRange,

        /// <summary>
        /// This mode works best for high precision grading similar to movie production workflows. Unity applies color grading before tonemapping.
        /// </summary>
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
        /// <summary>
        /// Use this to have the Volume Framework update every frame.
        /// </summary>
        [InspectorName("Every Frame")]
        EveryFrame = 0,

        /// <summary>
        /// Use this to disable Volume Framework updates or to update it manually via scripting.
        /// </summary>
        [InspectorName("Via Scripting")]
        ViaScripting = 1,

        /// <summary>
        /// Use this to choose the setting set on the pipeline asset.
        /// </summary>
        [InspectorName("Use Pipeline Settings")]
        UsePipelineSettings = 2,
    }

    /// <summary>
    /// Defines the upscaling filter selected by the user the universal render pipeline asset.
    /// </summary>
    public enum UpscalingFilterSelection
    {
        /// <summary>
        /// Unity selects a filtering option automatically based on the Render Scale value and the current screen resolution.
        /// </summary>
        [InspectorName("Automatic"), Tooltip("Unity selects a filtering option automatically based on the Render Scale value and the current screen resolution.")]
        Auto,

        /// <summary>
        /// Unity uses Bilinear filtering to perform upscaling.
        /// </summary>
        [InspectorName("Bilinear")]
        Linear,

        /// <summary>
        /// Unity uses Nearest-Neighbour filtering to perform upscaling.
        /// </summary>
        [InspectorName("Nearest-Neighbor")]
        Point,

        /// <summary>
        /// Unity uses the AMD FSR 1.0 technique to perform upscaling.
        /// </summary>
        [InspectorName("FidelityFX Super Resolution 1.0"), Tooltip("If the target device does not support Unity shader model 4.5, Unity falls back to the Automatic option.")]
        FSR
    }

    /// <summary>
    /// Type of the LOD cross-fade.
    /// </summary>
    public enum LODCrossFadeDitheringType
    {
        /// <summary>Unity uses the Bayer matrix texture to compute the LOD cross-fade dithering.</summary>
        BayerMatrix,

        /// <summary>Unity uses the precomputed blue noise texture to compute the LOD cross-fade dithering.</summary>
        BlueNoise
    }

    /// <summary>
    /// The asset that contains the URP setting.
    /// You can use this asset as a graphics quality level.
    /// </summary>
    /// <see cref="RenderPipelineAsset"/>
    /// <see cref="UniversalRenderPipeline"/>
    [ExcludeFromPreset]
    [URPHelpURL("universalrp-asset")]
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
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DecalLayers)]
        private const bool k_RendererFeatureDefaults = true;

        // Platform specific filtering overrides
        [ShaderKeywordFilter.ApplyRulesIfGraphicsAPI(GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.WriteRenderingLayers)]
        private const bool k_CommonGLDefaults = true;
#endif
        Shader m_DefaultShader;
        ScriptableRenderer[] m_Renderers = new ScriptableRenderer[1];

        // Default values set when a new UniversalRenderPipeline asset is created
        [SerializeField] int k_AssetVersion = 11;
        [SerializeField] int k_AssetPreviousVersion = 11;

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

        // Quality settings
        [SerializeField] bool m_SupportsHDR = true;
        [SerializeField] HDRColorBufferPrecision m_HDRColorBufferPrecision = HDRColorBufferPrecision._32Bits;
        [SerializeField] MsaaQuality m_MSAA = MsaaQuality.Disabled;
        [SerializeField] float m_RenderScale = 1.0f;
        [SerializeField] UpscalingFilterSelection m_UpscalingFilter = UpscalingFilterSelection.Auto;
        [SerializeField] bool m_FsrOverrideSharpness = false;
        [SerializeField] float m_FsrSharpness = FSRUtils.kDefaultSharpnessLinear;

#if UNITY_EDITOR // multi_compile_fragment _ LOD_FADE_CROSSFADE
        // TODO: Add RenderPipelineGlobalSettings to filter data hierarchy and select both variants based on
        // stripUnusedLODCrossFadeVariants. Then we can try removing here based on this setting.
        // [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.LOD_FADE_CROSSFADE)]
#endif
        [SerializeField] bool m_EnableLODCrossFade = true;
        [SerializeField] LODCrossFadeDitheringType m_LODCrossFadeDitheringType = LODCrossFadeDitheringType.BlueNoise;

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
        [SerializeField] SoftShadowQuality m_SoftShadowQuality = SoftShadowQuality.Medium;

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
        // multi_compile_fragment _ _LIGHT_COOKIES
        [ShaderKeywordFilter.RemoveIf(false, keywordNames: ShaderKeywordStrings.LightCookies)]
#endif
        [SerializeField] bool m_SupportsLightCookies = true;
#if UNITY_EDITOR
        // multi_compile_fragment _ _LIGHT_LAYERS
        [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: ShaderKeywordStrings.LightLayers)]
        // TODO: Filtering WriteRenderingLayers requires different filter triggers for different passes (i.e. per-pass filter attributes)
#endif
        [SerializeField] bool m_SupportsLightLayers = false;
        [SerializeField] [Obsolete] PipelineDebugLevel m_DebugLevel;
        [SerializeField] StoreActionsOptimization m_StoreActionsOptimization = StoreActionsOptimization.Auto;
        [SerializeField] bool m_EnableRenderGraph = false;

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

        [SerializeField] VolumeFrameworkUpdateMode m_VolumeFrameworkUpdateMode = VolumeFrameworkUpdateMode.EveryFrame;

        [SerializeField] TextureResources m_Textures;

        // Note: A lut size of 16^3 is barely usable with the HDR grading mode. 32 should be the
        // minimum, the lut being encoded in log. Lower sizes would work better with an additional
        // 1D shaper lut but for now we'll keep it simple.

        /// <summary>
        /// The minimum color grading LUT (lookup table) size.
        /// </summary>
        public const int k_MinLutSize = 16;

        /// <summary>
        /// The maximum color grading LUT (lookup table) size.
        /// </summary>
        public const int k_MaxLutSize = 65;

        internal const int k_ShadowCascadeMinCount = 1;
        internal const int k_ShadowCascadeMaxCount = 4;

        /// <summary>
        /// The default low tier resolution for additional lights shadow texture.
        /// </summary>
        public static readonly int AdditionalLightsDefaultShadowResolutionTierLow = 256;

        /// <summary>
        /// The default medium tier resolution for additional lights shadow texture.
        /// </summary>
        public static readonly int AdditionalLightsDefaultShadowResolutionTierMedium = 512;

        /// <summary>
        /// The default high tier resolution for additional lights shadow texture.
        /// </summary>
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

            ResourceReloader.ReloadAllNullIn(instance, packagePath);

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
        /// <summary>
        /// Use this class to initialize the rendererData element that is required by the renderer.
        /// </summary>
        /// <param name="type">The <c>RendererType</c> of the new renderer that is initialized within this asset.</param>
        /// <returns></returns>
        /// <see cref="RendererType"/>
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

        /// <summary>
        /// Creates a <c>UniversalRenderPipeline</c> from the <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <returns>Returns a <c>UniversalRenderPipeline</c> created from this UniversalRenderPipelineAsset.</returns>
        /// <see cref="RenderPipeline"/>
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

            DestroyRenderers();
            var pipeline = new UniversalRenderPipeline(this);
            CreateRenderers();

            // Blitter can only be initialized after renderers have been created and ResourceReloader has been
            // called on potentially empty shader resources
            foreach (var data in m_RendererDataList)
            {
                if (data is UniversalRendererData universalData)
                {
                    Blitter.Initialize(universalData.shaders.coreBlitPS, universalData.shaders.coreBlitColorAndDepthPS);
                    break;
                }
            }

            return pipeline;
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

        /// <summary>
        /// Unity calls this function when it loads the asset or when the asset is changed with the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnValidate();
        }

        /// <summary>
        /// Unity calls this function when the asset is disabled.
        /// </summary>
        protected override void OnDisable()
        {
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnDisable();
        }

        void CreateRenderers()
        {
            if (m_Renderers != null)
            {
                for (int i = 0; i < m_Renderers.Length; ++i)
                {
                    if (m_Renderers[i] != null)
                        Debug.LogError($"Creating renderers but previous instance wasn't properly destroyed: m_Renderers[{i}]");
                }
            }

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
            {
                DestroyRenderers();
                CreateRenderers();
            }

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

        /// <summary>
        /// When true, the pipeline creates a depth texture that can be read in shaders. The depth texture can be accessed as _CameraDepthTexture. This setting can be overridden per camera.
        /// </summary>
        public bool supportsCameraDepthTexture
        {
            get { return m_RequireDepthTexture; }
            set { m_RequireDepthTexture = value; }
        }

        /// <summary>
        /// When true, the pipeline creates a texture that contains a copy of the color buffer after rendering opaque objects. This texture can be accessed in shaders as _CameraOpaqueTexture. This setting can be overridden per camera.
        /// </summary>
        public bool supportsCameraOpaqueTexture
        {
            get { return m_RequireOpaqueTexture; }
            set { m_RequireOpaqueTexture = value; }
        }

        /// <summary>
        /// Returns the downsampling method used when copying the camera color texture after rendering opaques.
        /// </summary>
        public Downsampling opaqueDownsampling
        {
            get { return m_OpaqueDownsampling; }
        }

        /// <summary>
        /// This settings controls if the asset <c>UniversalRenderPipelineAsset</c> supports terrain holes.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/terrain-PaintHoles.html"/>
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

        /// <summary>
        /// When enabled, the camera renders to HDR buffers. This setting can be overridden per camera.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/HDR.html"/>
        public bool supportsHDR
        {
            get { return m_SupportsHDR; }
            set { m_SupportsHDR = value; }
        }

        /// <summary>
        /// Graphics format requested for HDR color buffers.
        /// </summary>
        public HDRColorBufferPrecision hdrColorBufferPrecision
        {
            get { return m_HDRColorBufferPrecision; }
            set { m_HDRColorBufferPrecision = value; }
        }

        /// <summary>
        /// Specifies the msaa sample count used by this <c>UniversalRenderPipelineAsset</c>
        /// </summary>
        /// <see cref="MsaaQuality"/>
        public int msaaSampleCount
        {
            get { return (int)m_MSAA; }
            set { m_MSAA = (MsaaQuality)value; }
        }

        /// <summary>
        /// Specifies the render scale which scales the render target resolution used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        public float renderScale
        {
            get { return m_RenderScale; }
            set { m_RenderScale = ValidateRenderScale(value); }
        }

        /// <summary>
        /// Returns true if the cross-fade style blending between the current LOD and the next LOD is enabled.
        /// </summary>
        public bool enableLODCrossFade
        {
            get { return m_EnableLODCrossFade; }
        }

        /// <summary>
        /// Returns the type of active LOD cross-fade.
        /// </summary>
        public LODCrossFadeDitheringType lodCrossFadeDitheringType
        {
            get { return m_LODCrossFadeDitheringType; }
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

        /// <summary>
        /// Specifies the <c>LightRenderingMode</c> for the main light used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <see cref="LightRenderingMode"/>
        public LightRenderingMode mainLightRenderingMode
        {
            get { return m_MainLightRenderingMode; }
            internal set { m_MainLightRenderingMode = value; }
        }

        /// <summary>
        /// Specifies if objects lit by main light cast shadows.
        /// </summary>
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

        /// <summary>
        /// Returns the main light shadowmap resolution used for this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        public int mainLightShadowmapResolution
        {
            get { return (int)m_MainLightShadowmapResolution; }
            internal set { m_MainLightShadowmapResolution = (ShadowResolution)value; }
        }

        /// <summary>
        /// Specifies the <c>LightRenderingMode</c> for the additional lights used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <see cref="LightRenderingMode"/>
        public LightRenderingMode additionalLightsRenderingMode
        {
            get { return m_AdditionalLightsRenderingMode; }
            internal set { m_AdditionalLightsRenderingMode = value; }
        }

        /// <summary>
        /// Specifies the maximum amount of per-object additional lights which can be used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        public int maxAdditionalLightsCount
        {
            get { return m_AdditionalLightsPerObjectLimit; }
            set { m_AdditionalLightsPerObjectLimit = ValidatePerObjectLights(value); }
        }

        /// <summary>
        /// Specifies if objects lit by additional lights cast shadows.
        /// </summary>
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

        /// <summary>
        /// Additional light shadows are rendered into a single shadow map atlas texture. This setting controls the resolution of the shadow map atlas texture.
        /// </summary>
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

        /// <summary>
        /// Specifies if this <c>UniversalRenderPipelineAsset</c> should use Probe blending for the reflection probes in the scene.
        /// </summary>
        public bool reflectionProbeBlending
        {
            get { return m_ReflectionProbeBlending; }
            internal set { m_ReflectionProbeBlending = value; }
        }

        /// <summary>
        /// Specifies if this <c>UniversalRenderPipelineAsset</c> should allow box projection for the reflection probes in the scene.
        /// </summary>
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

        /// <summary>
        /// Light default Soft Shadow Quality.
        /// </summary>
        internal SoftShadowQuality softShadowQuality
        {
            get { return m_SoftShadowQuality; }
            set { m_SoftShadowQuality = value; }
        }

        /// <summary>
        /// Specifies if this <c>UniversalRenderPipelineAsset</c> should use dynamic batching.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/DrawCallBatching.html"/>
        public bool supportsDynamicBatching
        {
            get { return m_SupportsDynamicBatching; }
            set { m_SupportsDynamicBatching = value; }
        }

        /// <summary>
        /// Returns true if the Render Pipeline Asset supports mixed lighting, false otherwise.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/LightMode-Mixed.html"/>
        public bool supportsMixedLighting
        {
            get { return m_MixedLightingSupported; }
        }

        /// <summary>
        /// Returns true if the Render Pipeline Asset supports light cookies, false otherwise.
        /// </summary>
        public bool supportsLightCookies
        {
            get { return m_SupportsLightCookies; }
        }

        /// <summary>
        /// Returns true if the Render Pipeline Asset supports light layers, false otherwise.
        /// </summary>
        [Obsolete("This is obsolete, UnityEngine.Rendering.ShaderVariantLogLevel instead.", false)]
        public bool supportsLightLayers
        {
            get { return m_SupportsLightLayers; }
        }

        /// <summary>
        /// Returns true if the Render Pipeline Asset supports rendering layers for lights, false otherwise.
        /// </summary>
        public bool useRenderingLayers
        {
            get { return m_SupportsLightLayers; }
        }

        /// <summary>
        /// Returns the selected update mode for volumes.
        /// </summary>
        public VolumeFrameworkUpdateMode volumeFrameworkUpdateMode => m_VolumeFrameworkUpdateMode;

        /// <summary>
        /// Previously returned the debug level for this Render Pipeline Asset but is now deprecated. Replaced to use the profiler and is no longer used.
        /// </summary>
        [Obsolete("PipelineDebugLevel is deprecated and replaced to use the profiler. Calling debugLevel is not necessary.", false)]
        public PipelineDebugLevel debugLevel
        {
            get => PipelineDebugLevel.Disabled;
        }

        /// <summary>
        /// Specifies if SRPBacher is used by this <c>UniversalRenderPipelineAsset</c>.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Manual/SRPBatcher.html"/>
        public bool useSRPBatcher
        {
            get { return m_UseSRPBatcher; }
            set { m_UseSRPBatcher = value; }
        }

        /// <summary>
        /// Controls whether the RenderGraph render path is enabled.
        /// </summary>
        public bool enableRenderGraph
        {
            get { return m_EnableRenderGraph; }
            set { m_EnableRenderGraph = value; }
        }

        /// <summary>
        /// Returns the selected ColorGradingMode in the URP Asset.
        /// <see cref="ColorGradingMode"/>
        /// </summary>
        public ColorGradingMode colorGradingMode
        {
            get { return m_ColorGradingMode; }
            set { m_ColorGradingMode = value; }
        }

        /// <summary>
        /// Specifies the color grading LUT (lookup table) size in the URP Asset.
        /// </summary>
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

        /// <summary>
        /// Returns the default Material.
        /// </summary>
        /// <returns>Returns the default Material.</returns>
        public override Material defaultMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Standard); }
        }

        /// <summary>
        /// Returns the default particle Material.
        /// </summary>
        /// <returns>Returns the default particle Material.</returns>
        public override Material defaultParticleMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        /// <summary>
        /// Returns the default line Material.
        /// </summary>
        /// <returns>Returns the default line Material.</returns>
        public override Material defaultLineMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        /// <summary>
        /// Returns the default terrain Material.
        /// </summary>
        /// <returns>Returns the default terrain Material.</returns>
        public override Material defaultTerrainMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Terrain); }
        }

        /// <summary>
        /// Returns the default UI Material.
        /// </summary>
        /// <returns>Returns the default UI Material.</returns>
        public override Material defaultUIMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        /// <summary>
        /// Returns the default UI overdraw Material.
        /// </summary>
        /// <returns>Returns the default UI overdraw Material.</returns>
        public override Material defaultUIOverdrawMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        /// <summary>
        /// Returns the default UIETC1 supported Material for this asset.
        /// </summary>
        /// <returns>Returns the default UIETC1 supported Material.</returns>
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

        /// <summary>
        /// Returns the Material that Unity uses to render decals.
        /// </summary>
        /// <returns>Returns the Material containing the Unity decal shader.</returns>
        public Material decalMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Decal); }
        }

        /// <summary>
        /// Returns the default shader for the specified renderer. When creating new objects in the editor, the materials of those objects will use the selected default shader.
        /// </summary>
        /// <returns>Returns the default shader for the specified renderer.</returns>
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
        /// <summary>
        /// Returns the Autodesk Interactive shader that this asset uses.
        /// </summary>
        /// <returns>Returns the Autodesk Interactive shader that this asset uses.</returns>
        public override Shader autodeskInteractiveShader
        {
            get { return editorResources?.shaders.autodeskInteractivePS; }
        }

        /// <summary>
        /// Returns the Autodesk Interactive transparent shader that this asset uses.
        /// </summary>
        /// <returns>Returns the Autodesk Interactive transparent shader that this asset uses.</returns>
        public override Shader autodeskInteractiveTransparentShader
        {
            get { return editorResources?.shaders.autodeskInteractiveTransparentPS; }
        }

        /// <summary>
        /// Returns the Autodesk Interactive mask shader that this asset uses.
        /// </summary>
        /// <returns>Returns the Autodesk Interactive mask shader that this asset uses</returns>
        public override Shader autodeskInteractiveMaskedShader
        {
            get { return editorResources?.shaders.autodeskInteractiveMaskedPS; }
        }

        /// <summary>
        /// Returns the terrain detail lit shader that this asset uses.
        /// </summary>
        /// <returns>Returns the terrain detail lit shader that this asset uses.</returns>
        public override Shader terrainDetailLitShader
        {
            get { return editorResources?.shaders.terrainDetailLitPS; }
        }

        /// <summary>
        /// Returns the terrain detail grass shader that this asset uses.
        /// </summary>
        /// <returns>Returns the terrain detail grass shader that this asset uses.</returns>
        public override Shader terrainDetailGrassShader
        {
            get { return editorResources?.shaders.terrainDetailGrassPS; }
        }

        /// <summary>
        /// Returns the terrain detail grass billboard shader that this asset uses.
        /// </summary>
        /// <returns>Returns the terrain detail grass billboard shader that this asset uses.</returns>
        public override Shader terrainDetailGrassBillboardShader
        {
            get { return editorResources?.shaders.terrainDetailGrassBillboardPS; }
        }

        /// <summary>
        /// Returns the default SpeedTree7 shader that this asset uses.
        /// </summary>
        /// <returns>Returns the default SpeedTree7 shader that this asset uses.</returns>
        public override Shader defaultSpeedTree7Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree7PS; }
        }

        /// <summary>
        /// Returns the default SpeedTree8 shader that this asset uses.
        /// </summary>
        /// <returns>Returns the default SpeedTree8 shader that this asset uses.</returns>
        public override Shader defaultSpeedTree8Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree8PS; }
        }

        /// <inheritdoc/>
        public override string renderPipelineShaderTag => UniversalRenderPipeline.k_ShaderTagName;
#endif

        /// <summary>Names used for display of rendering layer masks.</summary>
        public override string[] renderingLayerMaskNames => UniversalRenderPipelineGlobalSettings.instance.renderingLayerMaskNames;

        /// <summary>Names used for display of rendering layer masks with prefix.</summary>
        public override string[] prefixedRenderingLayerMaskNames => UniversalRenderPipelineGlobalSettings.instance.prefixedRenderingLayerMaskNames;

        /// <summary>
        /// Names used for display of light layers.
        /// </summary>
        [Obsolete("This is obsolete, please use renderingLayerMaskNames instead.", false)]
        public string[] lightLayerMaskNames => new string[0];

        /// <summary>
        /// Returns asset texture resources
        /// </summary>
        public TextureResources textures
        {
            get
            {
                if (m_Textures == null)
                    m_Textures = new TextureResources();

#if UNITY_EDITOR
                if (m_Textures.NeedsReload())
                    ResourceReloader.ReloadAllNullIn(this, packagePath);
#endif

                return m_Textures;
            }
        }

        /// <summary>
        /// Unity raises a callback to this method before it serializes the asset.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// Unity raises a callback to this method after it deserializes the asset.
        /// </summary>
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

            if (k_AssetVersion < 10)
            {
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 10;
            }

            if (k_AssetVersion < 11)
            {
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 11;
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

            if (asset.k_AssetPreviousVersion < 10)
            {
                UniversalRenderPipelineGlobalSettings.Ensure().shaderVariantLogLevel = (Rendering.ShaderVariantLogLevel) asset.m_ShaderVariantLogLevel;
                asset.k_AssetPreviousVersion = 10;
            }

            if(asset.k_AssetPreviousVersion < 11)
            {
                ResourceReloader.ReloadAllNullIn(asset, packagePath);
                asset.k_AssetPreviousVersion = 11;
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

        /// <summary>
        /// Class containing texture resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class TextureResources
        {
            /// <summary>
            /// Pre-baked blue noise textures.
            /// </summary>
            [Reload("Textures/BlueNoise64/L/LDR_LLL1_0.png")]
            public Texture2D blueNoise64LTex;

            /// <summary>
            /// Bayer matrix texture.
            /// </summary>
            [Reload("Textures/BayerMatrix.png")]
            public Texture2D bayerMatrixTex;

            /// <summary>
            /// Check if the textures need reloading.
            /// </summary>
            /// <returns>True if any of the textures need reloading.</returns>
            public bool NeedsReload()
            {
                return blueNoise64LTex == null || bayerMatrixTex == null;
            }
        }
    }
}
