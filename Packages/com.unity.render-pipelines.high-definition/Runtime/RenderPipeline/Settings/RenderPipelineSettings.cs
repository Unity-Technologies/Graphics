using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
#if UNITY_EDITOR
// TODO @ SHADERS: Enable as many of the rules (currently commented out) as make sense
//                 once the setting asset aggregation behavior is finalized.  More fine tuning
//                 of these rules is also desirable (current rules have been interpreted from
//                 the variant stripping logic)
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    // RenderPipelineSettings define settings that can't be change during runtime. It is equivalent to the GraphicsSettings of Unity (Tiers + shader variant removal).
    // This allow to allocate resource or not for a given feature.
    // FrameSettings control within a frame what is enable or not(enableShadow, enableDistortion...).
    // HDRenderPipelineAsset reference the current RenderPipelineSettings used, there is one per supported platform(Currently this feature is not implemented and only one GlobalFrameSettings is available).
    // A Camera with HDAdditionalData has one FrameSettings that configures how it will render. For example a camera used for reflection will disable distortion and post-process.
    // Additionally, on a Camera there is another FrameSettings called ActiveFrameSettings that is created on the fly based on FrameSettings and allows modifications for debugging purpose at runtime without being serialized on disk.
    // The ActiveFrameSettings is registered in the debug windows at the creation of the camera.
    // A Camera with HDAdditionalData has a RenderPath that defines if it uses a "Default" FrameSettings, a preset of FrameSettings or a custom one.
    // HDRenderPipelineAsset contains a "Default" FrameSettings that can be referenced by any camera with RenderPath.Defaut or when the camera doesn't have HDAdditionalData like the camera of the Editor.
    // It also contains a DefaultActiveFrameSettings

    // RenderPipelineSettings represents settings that are immutable at runtime.
    // There is a dedicated RenderPipelineSettings for each platform
    /// <summary>
    /// HDRP Render Pipeline Settings.
    /// </summary>
    [Serializable]
    public struct RenderPipelineSettings
    {
        /// <summary>
        /// Supported Lit Shader Mode.
        /// </summary>
        public enum SupportedLitShaderMode
        {
            /// <summary>Forward shading only.</summary>
            ForwardOnly = 1 << 0,
            /// <summary>Deferred shading only.</summary>
            DeferredOnly = 1 << 1,
            /// <summary>Both Forward and Deferred shading.</summary>
            Both = ForwardOnly | DeferredOnly
        }

        /// <summary>
        /// The available Probe system used.
        /// </summary>
        public enum LightProbeSystem
        {
            /// <summary>The legacy light probe system.</summary>
            [InspectorName("Light Probe Groups")]
            LegacyLightProbes = 0,
            /// <summary>Adaptive Probe Volumes system.</summary>
            AdaptiveProbeVolumes = 1,
        }


        /// <summary>
        /// Color Buffer format.
        /// </summary>
        public enum ColorBufferFormat
        {
            /// <summary>R11G11B10 for faster rendering.</summary>
            R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
            /// <summary>R16G16B16A16 for better quality.</summary>
            R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat
        }

        /// <summary>
        /// Custom Buffers format.
        /// </summary>
        public enum CustomBufferFormat
        {
            /// <summary>Regular R8G8B8A8 format.</summary>
            [InspectorName("Signed R8G8B8A8")]
            SignedR8G8B8A8 = GraphicsFormat.R8G8B8A8_SNorm,
            /// <summary>Regular R8G8B8A8 format.</summary>
            R8G8B8A8 = GraphicsFormat.R8G8B8A8_UNorm,
            /// <summary>R16G16B16A16 high quality HDR format.</summary>
            R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat,
            /// <summary>R11G11B10 medium quality HDR format.</summary>
            R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
        }

        /// <summary>
        /// Supported Ray Tracing Mode.
        /// </summary>
        public enum SupportedRayTracingMode
        {
            /// <summary>Performance mode only.</summary>
            Performance = 1 << 0,
            /// <summary>Quality mode only.</summary>
            Quality = 1 << 1,
            /// <summary>Both Performance and Quality modes.</summary>
            Both = Performance | Quality
        }

        internal static RenderPipelineSettings NewDefault()
        {
            RenderPipelineSettings settings = new RenderPipelineSettings()
            {
                supportShadowMask = true,
                supportSSAO = true,
                supportSubsurfaceScattering = true,
                subsurfaceScatteringAttenuation = true,
                sssSampleBudget = new IntScalableSetting(new[] { (int)DefaultSssSampleBudgetForQualityLevel.Low,
                                                                 (int)DefaultSssSampleBudgetForQualityLevel.Medium,
                                                                 (int)DefaultSssSampleBudgetForQualityLevel.High }, ScalableSettingSchemaId.With3Levels),
                sssDownsampleSteps = new IntScalableSetting(new[] { (int)DefaultSssDownsampleSteps.Low,
                                                                    (int)DefaultSssDownsampleSteps.Medium,
                                                                    (int)DefaultSssDownsampleSteps.High }, ScalableSettingSchemaId.With3Levels),
                supportVolumetrics = true,
                supportDistortion = true,
                supportTransparentBackface = true,
                supportTransparentDepthPrepass = true,
                supportTransparentDepthPostpass = true,
                colorBufferFormat = ColorBufferFormat.R11G11B10,
                supportCustomPass = true,
                supportVariableRateShading = true,
                customBufferFormat = CustomBufferFormat.R8G8B8A8,
                supportedLitShaderMode = SupportedLitShaderMode.DeferredOnly,
                supportDecals = true,
                supportDecalLayers = false,
                supportSurfaceGradient = true,
                decalNormalBufferHP = false,
                msaaSampleCount = MSAASamples.None,
                supportMotionVectors = true,
                supportRuntimeAOVAPI = false,
                supportTerrainHole = false,

                supportComputeThickness = false,
                computeThicknessResolution = ComputeThicknessResolution.Half,
                computeThicknessLayerMask = 0,

                planarReflectionResolution = new PlanarReflectionAtlasResolutionScalableSetting(new[] { PlanarReflectionAtlasResolution.Resolution256,
                                                                                                        PlanarReflectionAtlasResolution.Resolution1024,
                                                                                                        PlanarReflectionAtlasResolution.Resolution2048 }, ScalableSettingSchemaId.With3Levels),
                cubeReflectionResolution = new ReflectionProbeResolutionScalableSetting(new[] { CubeReflectionResolution.CubeReflectionResolution128,
                                                                                                CubeReflectionResolution.CubeReflectionResolution256,
                                                                                                CubeReflectionResolution.CubeReflectionResolution512 }, ScalableSettingSchemaId.With3Levels),
                lightLoopSettings = GlobalLightLoopSettings.NewDefault(),
                hdShadowInitParams = HDShadowInitParameters.NewDefault(),
                decalSettings = GlobalDecalSettings.NewDefault(),
                postProcessSettings = GlobalPostProcessSettings.NewDefault(),
                dynamicResolutionSettings = GlobalDynamicResolutionSettings.NewDefault(),
                lowresTransparentSettings = GlobalLowResolutionTransparencySettings.NewDefault(),
                xrSettings = GlobalXRSettings.NewDefault(),
                postProcessQualitySettings = GlobalPostProcessingQualitySettings.NewDefault(),
                lightingQualitySettings = GlobalLightingQualitySettings.NewDefault(),
                lightSettings = LightSettings.NewDefault(),

                // Water Properties
                supportWater = false,
                waterSimulationResolution = WaterSimulationResolution.Medium128,
                supportWaterExclusion = true,
                supportWaterHorizontalDeformation = false,

                supportWaterDecals = true,
                waterDecalAtlasSize = WaterAtlasSize.AtlasSize1024,
                maximumWaterDecalCount = 48,

                waterScriptInteractionsMode = WaterScriptInteractionsMode.GPUReadback,
                waterFullCPUSimulation = false,

                supportScreenSpaceLensFlare = true,
                supportDataDrivenLensFlare = true,
                supportRayTracing = false,
                supportVFXRayTracing = false,
                supportedRayTracingMode = SupportedRayTracingMode.Both,
                lodBias = new FloatScalableSetting(new[] { 1.0f, 1, 1 }, ScalableSettingSchemaId.With3Levels),
                maximumLODLevel = new IntScalableSetting(new[] { 0, 0, 0 }, ScalableSettingSchemaId.With3Levels),
                lightProbeSystem = LightProbeSystem.AdaptiveProbeVolumes,
                probeVolumeMemoryBudget = ProbeVolumeTextureMemoryBudget.MemoryBudgetMedium,
                probeVolumeBlendingMemoryBudget = ProbeVolumeBlendingTextureMemoryBudget.MemoryBudgetLow,
                supportProbeVolumeScenarios = false,
                supportProbeVolumeScenarioBlending = true,
                supportHighQualityLineRendering = false,
                supportProbeVolumeGPUStreaming = false,
                supportProbeVolumeDiskStreaming = false,
                highQualityLineRenderingMemoryBudget = LineRendering.MemoryBudget.MemoryBudgetLow,
                probeVolumeSHBands = ProbeVolumeSHBands.SphericalHarmonicsL1,
                gpuResidentDrawerSettings = GlobalGPUResidentDrawerSettings.NewDefault()
            };
            return settings;
        }

        /// <summary>
        /// Light Settings.
        /// </summary>
        [Serializable]
        public struct LightSettings
        {
            /// <summary>Enable contact shadows.</summary>
            public BoolScalableSetting useContactShadow;

            internal static LightSettings NewDefault() => new LightSettings()
            {
                useContactShadow = new BoolScalableSetting(new[] { false, false, true }, ScalableSettingSchemaId.With3Levels)
            };
        }


        /// <summary>
        /// Represents resolution settings for planar reflections.
        /// </summary>
        [Serializable]
        public class PlanarReflectionAtlasResolutionScalableSetting : ScalableSetting<PlanarReflectionAtlasResolution>
        {
            /// <summary>
            /// Instantiate a new PlanarReflectionAtlasResolution scalable setting.
            /// </summary>
            /// <param name="values">The values of the settings</param>
            /// <param name="schemaId">The schema of the setting.</param>
            public PlanarReflectionAtlasResolutionScalableSetting(PlanarReflectionAtlasResolution[] values, ScalableSettingSchemaId schemaId)
                : base(values, schemaId)
            {
            }
        }

        /// <summary>
        /// Represents resolution settings for cube reflections.
        /// </summary>
        [Serializable]
        public class ReflectionProbeResolutionScalableSetting : ScalableSetting<CubeReflectionResolution>
        {
            /// <summary>
            /// Instantiate a new CubeReflectionResolution scalable setting.
            /// </summary>
            /// <param name="values">The values of the settings</param>
            /// <param name="schemaId">The schema of the setting.</param>
            public ReflectionProbeResolutionScalableSetting(CubeReflectionResolution[] values, ScalableSettingSchemaId schemaId)
                : base(values, schemaId)
            {
            }
        }

        // Lighting
        /// <summary>Support shadow masks.</summary>
        public bool supportShadowMask;
        /// <summary>Support screen space reflections.</summary>
        public bool supportSSR;
        /// <summary>Support transparent screen space reflections.</summary>
        public bool supportSSRTransparent;
        /// <summary>Support screen space ambient occlusion.</summary>
        public bool supportSSAO;
        /// <summary>Support screen space global illumination.</summary>
        public bool supportSSGI;
        /// <summary>Support subsurface scattering.</summary>
#if UNITY_EDITOR // multi_compile_fragment _ OUTPUT_SPLIT_LIGHTING
        // [ShaderKeywordFilter.RemoveIf(true, keywordNames: "OUTPUT_SPLIT_LIGHTING")]
#endif
        public bool supportSubsurfaceScattering;
        /// <summary>Enable SubSurface-Scattering occlusion computation. Enabling this makes the SSS slightly more expensive but add great details to occluded zones with SSS materials.</summary>
        public bool subsurfaceScatteringAttenuation;
        /// <summary>Sample budget for the Subsurface Scattering algorithm.</summary>
        public IntScalableSetting sssSampleBudget;
        /// <summary>Downsample input texture for the Subsurface Scattering algorithm.</summary>
        public IntScalableSetting sssDownsampleSteps;
        /// <summary>Support volumetric lighting.</summary>
        public bool supportVolumetrics;
        /// <summary>Support volumetric clouds.</summary>
        public bool supportVolumetricClouds;
        /// <summary>Support light layers.</summary>
        public bool supportLightLayers;
        /// <summary>Enable rendering layer mask buffer.</summary>
        public bool renderingLayerMaskBuffer;

        // Water
        /// <summary>Support Water Surfaces.</summary>
        public bool supportWater;
        /// <summary>Water simulation resolution</summary>
        public WaterSimulationResolution waterSimulationResolution;
        /// <summary>Support Water Surfaces exclusion.</summary>
        public bool supportWaterExclusion;
        /// <summary>Support Water Surfaces Horizontal Deformation.</summary>
        public bool supportWaterHorizontalDeformation;

        /// <summary>Support Water Surfaces deformation.</summary>
        public bool supportWaterDecals;
        /// <summary>Defines the resolution of the decal atlas.</summary>
        public WaterAtlasSize waterDecalAtlasSize;
        /// <summary>Maximum amount of visible water decals.</summary>
        public int maximumWaterDecalCount;

        /// <summary>Defines if the script interactions should simulate water on CPU or fetch simulation from the GPU.</summary>
        [Tooltip("Defines if the script interactions should simulate water on CPU or fetch simulation from the GPU.")]
        public WaterScriptInteractionsMode waterScriptInteractionsMode;
        /// <summary>Defines if the CPU simulation should be evaluated at full resolution or half resolution.</summary>
        [Tooltip("Defines if the CPU simulation should be evaluated at full resolution or half resolution.")]
        public bool waterFullCPUSimulation;

        // Compute Thickness
        /// <summary>Sample Compute Thickness algorithm.</summary>
        public bool supportComputeThickness;
        /// <summary>Scale for compute thickness texture array.</summary>
        public ComputeThicknessResolution computeThicknessResolution;
        /// <summary>LayerMask used to render thickness.</summary>
        public LayerMask computeThicknessLayerMask;

        /// <summary>Names for rendering layers.</summary>
        [Obsolete("This property is obsolete. Use RenderingLayerMask API and Tags & Layers project settings instead. #from(23.3)", false)]
        public string[] renderingLayerNames
        {
            get { return (string[])HDRenderPipelineGlobalSettings.instance.renderingLayerNames.Clone(); }
            set { HDRenderPipelineGlobalSettings.instance.renderingLayerNames = value; }
        }
        /// <summary>Support distortion.</summary>
        public bool supportDistortion;
        /// <summary>Support transparent backface pass.</summary>
        public bool supportTransparentBackface;
        /// <summary>Support transparent depth pre-pass.</summary>
        public bool supportTransparentDepthPrepass;
        /// <summary>Support transparent depth post-pass.</summary>
        public bool supportTransparentDepthPostpass;
        /// <summary>Color buffer format.</summary>
        public ColorBufferFormat colorBufferFormat;
        /// <summary>Support custom passes.</summary>
        public bool supportCustomPass;
        /// <summary>Support variable rate shading.</summary>
        public bool supportVariableRateShading;
        /// <summary>Custom passes buffer format.</summary>
        public CustomBufferFormat customBufferFormat;
        /// <summary>Supported Lit shader modes.</summary>
#if UNITY_EDITOR // multi_compile_fragment _ WRITE_MSAA_DEPTH
        // [ShaderKeywordFilter.RemoveIf(SupportedLitShaderMode.DeferredOnly, keywordNames: "WRITE_MSAA_DEPTH")]
#endif
        public SupportedLitShaderMode supportedLitShaderMode;
        /// <summary></summary>
        public PlanarReflectionAtlasResolutionScalableSetting planarReflectionResolution;
        /// <summary></summary>
        public ReflectionProbeResolutionScalableSetting cubeReflectionResolution;
        // Engine
        /// <summary>Support decals.</summary>
#if UNITY_EDITOR // multi_compile_fragment DECALS_OFF DECALS_3RT DECALS_4RT
        // If decals are not supported, remove the multiple render target variants
        // [ShaderKeywordFilter.RemoveIf(false, keywordNames: new string[] {"DECALS_3RT", "DECALS_4RT"})]
        // Similar but separate rule due to the separate multi_compile_fragment _ DECAL_SURFACE_GRADIENT
        // [ShaderKeywordFilter.RemoveIf(false, keywordNames: "DECAL_SURFACE_GRADIENT")]
        // If decals are supported, remove the no decal variant
        // [ShaderKeywordFilter.RemoveIf(true, keywordNames: "DECALS_OFF")]
        // If decals are not supported, remove WRITE_DECAL_BUFFER
        // [ShaderKeywordFilter.RemoveIf(false, keywordNames: "WRITE_DECAL_BUFFER")]
#endif
        public bool supportDecals;
        /// <summary>Support decal Layers.</summary>
#if UNITY_EDITOR // multi_compile _ WRITE_DECAL_BUFFER
        // [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: "WRITE_DECAL_BUFFER")]
#endif
        public bool supportDecalLayers;
        /// <summary>Support surface gradient for decal normal blending.</summary>
#if UNITY_EDITOR // multi_compile_fragment _ DECAL_SURFACE_GRADIENT
        // Remove if surface gradient is not supported
        // [ShaderKeywordFilter.RemoveIf(true, keywordNames: "DECAL_SURFACE_GRADIENT")]
#endif
        public bool supportSurfaceGradient;
        /// <summary>High precision normal buffer.</summary>
        public bool decalNormalBufferHP;
        /// <summary>Support High Quality Line Rendering.</summary>
        public bool supportHighQualityLineRendering;
        /// <summary>High Quality Line Rendering Memory Budget.</summary>
        public LineRendering.MemoryBudget highQualityLineRenderingMemoryBudget;

        /// <summary>Default Number of samples when using MSAA.</summary>
        public MSAASamples msaaSampleCount;
        /// <summary>Support MSAA.</summary>
        [Obsolete]
        public bool supportMSAA => msaaSampleCount != MSAASamples.None;

        // Returns true if the output of the rendering passes support an alpha channel
        internal bool SupportsAlpha()
        {
            return CoreUtils.IsSceneFilteringEnabled() || (colorBufferFormat == ColorBufferFormat.R16G16B16A16);
        }

        /// <summary>Support motion vectors.</summary>
        public bool supportMotionVectors;

        // Post Processing
        /// <summary>Support Screen Space Lens Flare.</summary>
        public bool supportScreenSpaceLensFlare;
        /// <summary>Support Data Driven Lens Flare.</summary>
        public bool supportDataDrivenLensFlare;

        /// <summary>Support runtime debug display.</summary>
        [Obsolete("Use HDRenderPipelineGlobalSettings.instance.stripDebugVariants) instead. #from(23.1)", false)]
        public bool supportRuntimeDebugDisplay
        {
            get => !HDRenderPipelineGlobalSettings.instance.m_StripDebugVariants;
            set => HDRenderPipelineGlobalSettings.instance.m_StripDebugVariants = !value;
        }

        internal bool supportProbeVolume => (lightProbeSystem == LightProbeSystem.AdaptiveProbeVolumes);
        [FormerlySerializedAs("supportProbeVolume")]
        [Obsolete("Use lightProbeSystem instead", false)]
        internal bool oldSupportProbeVolume;

        /// <summary> Support LOD Dithering Cross-Fade/// </summary>
        [Obsolete("This setting has no effect, use LOD Quality Setting instead", false)]
        public bool supportDitheringCrossFade;

        /// <summary>Support runtime AOV API.</summary>
        public bool supportRuntimeAOVAPI;

        /// <summary>Support terrain holes.</summary>
        public bool supportTerrainHole;
        /// <summary>Determines what system to use.</summary>
#if UNITY_EDITOR // multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
        // [ShaderKeywordFilter.SelectIf(LightProbeSystem.ProbeVolumes, new string[] {"PROBE_VOLUMES_L1", "PROBE_VOLUMES_L2"})]
        // [ShaderKeywordFilter.RemoveIf(LightProbeSystem.LegacyLightProbes, keywordNames: new string[] {"PROBE_VOLUMES_L1", "PROBE_VOLUMES_L2"})]
#endif
        public LightProbeSystem lightProbeSystem;
        [SerializeField]
        [FormerlySerializedAs("lightProbeSystem")]
        internal LightProbeSystem oldLightProbeSystem;

        /// <summary>Probe Volume Memory Budget.</summary>
        public ProbeVolumeTextureMemoryBudget probeVolumeMemoryBudget;
        /// <summary>Support GPU Streaming for Probe Volumes.</summary>
        [FormerlySerializedAs("supportProbeVolumeStreaming")]
        public bool supportProbeVolumeGPUStreaming;
        /// <summary>Support Disk Streaming for Probe Volumes.</summary>
        public bool supportProbeVolumeDiskStreaming;
        /// <summary>Probe Volumes SH Bands.</summary>
#if UNITY_EDITOR // multi_compile _ PROBE_VOLUMES_L1 PROBE_VOLUMES_L2
        // [ShaderKeywordFilter.RemoveIf(ProbeVolumeSHBands.SphericalHarmonicsL1, keywordNames: "PROBE_VOLUMES_L2")]
        // [ShaderKeywordFilter.RemoveIf(ProbeVolumeSHBands.SphericalHarmonicsL2, keywordNames: "PROBE_VOLUMES_L1")]
#endif
        public ProbeVolumeSHBands probeVolumeSHBands;
        /// <summary>Support Scenarios for Probe Volumes.</summary>
        public bool supportProbeVolumeScenarios;
        /// <summary>Support Scenarios for Probe Volumes.</summary>
        public bool supportProbeVolumeScenarioBlending;
        /// <summary>Probe Volume Memory Budget for scenario blending.</summary>
        public ProbeVolumeBlendingTextureMemoryBudget probeVolumeBlendingMemoryBudget;

        /// <summary>Support ray tracing.</summary>
        public bool supportRayTracing;
        /// <summary> Support ray tracing of VFXs.</summary>
        public bool supportVFXRayTracing;

        /// <summary>Support ray tracing mode.</summary>
        public SupportedRayTracingMode supportedRayTracingMode;

        /// <summary>Global light loop settings.</summary>
        public GlobalLightLoopSettings lightLoopSettings;
        /// <summary>Global shadows settings.</summary>
        public HDShadowInitParameters hdShadowInitParams;
        /// <summary>Global decal settings</summary>
        public GlobalDecalSettings decalSettings;
        /// <summary>Global post process settings.</summary>
        public GlobalPostProcessSettings postProcessSettings;
        /// <summary>Global dynamic resolution settings.</summary>
        public GlobalDynamicResolutionSettings dynamicResolutionSettings;
        /// <summary>Global low resolution transparency settings.</summary>
        public GlobalLowResolutionTransparencySettings lowresTransparentSettings;
        /// <summary>Global XR settings.</summary>
        public GlobalXRSettings xrSettings;
        /// <summary>Global post processing quality settings.</summary>
        public GlobalPostProcessingQualitySettings postProcessQualitySettings;

        /// <summary>Light Settings.</summary>
        public LightSettings lightSettings;
        /// <summary>Maximum LoD Level.</summary>
        public IntScalableSetting maximumLODLevel;
        /// <summary>LoD bias.</summary>
        public FloatScalableSetting lodBias;

        /// <summary>Global lighting quality settings.</summary>
        public GlobalLightingQualitySettings lightingQualitySettings;

        /// <summary>Global macro batcher settings.</summary>
        [FormerlySerializedAs("macroBatcherSettings")] public GlobalGPUResidentDrawerSettings gpuResidentDrawerSettings;

#pragma warning disable 618 // Type or member is obsolete
        [Obsolete("For data migration")]
        internal bool m_ObsoleteincreaseSssSampleCount;

        [SerializeField]
        [FormerlySerializedAs("lightLayerName0"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteLightLayerName0;
        [SerializeField]
        [FormerlySerializedAs("lightLayerName1"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteLightLayerName1;
        [SerializeField]
        [FormerlySerializedAs("lightLayerName2"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteLightLayerName2;
        [SerializeField]
        [FormerlySerializedAs("lightLayerName3"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteLightLayerName3;
        [SerializeField]
        [FormerlySerializedAs("lightLayerName4"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteLightLayerName4;
        [SerializeField]
        [FormerlySerializedAs("lightLayerName5"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteLightLayerName5;
        [SerializeField]
        [FormerlySerializedAs("lightLayerName6"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteLightLayerName6;
        [SerializeField]
        [FormerlySerializedAs("lightLayerName7"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteLightLayerName7;

        [SerializeField]
        [FormerlySerializedAs("decalLayerName0"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteDecalLayerName0;
        [SerializeField]
        [FormerlySerializedAs("decalLayerName1"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteDecalLayerName1;
        [SerializeField]
        [FormerlySerializedAs("decalLayerName2"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteDecalLayerName2;
        [SerializeField]
        [FormerlySerializedAs("decalLayerName3"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteDecalLayerName3;
        [SerializeField]
        [FormerlySerializedAs("decalLayerName4"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteDecalLayerName4;
        [SerializeField]
        [FormerlySerializedAs("decalLayerName5"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteDecalLayerName5;
        [SerializeField]
        [FormerlySerializedAs("decalLayerName6"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteDecalLayerName6;
        [SerializeField]
        [FormerlySerializedAs("decalLayerName7"), Obsolete("Moved to HDGlobal Settings")]
        internal string m_ObsoleteDecalLayerName7;

        [SerializeField]
        [FormerlySerializedAs("supportRuntimeDebugDisplay"), Obsolete("Moved to HDGlobal Settings")]
        internal bool m_ObsoleteSupportRuntimeDebugDisplay;
#pragma warning restore 618
    }
}
