using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

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
            /// <summary>Probe Volume system.</summary>
            ProbeVolumes = 1,
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
                sssSampleBudget = new IntScalableSetting(new[] { (int)DefaultSssSampleBudgetForQualityLevel.Low,
                                                                 (int)DefaultSssSampleBudgetForQualityLevel.Medium,
                                                                 (int)DefaultSssSampleBudgetForQualityLevel.High }, ScalableSettingSchemaId.With3Levels),
                supportVolumetrics = true,
                supportDistortion = true,
                supportTransparentBackface = true,
                supportTransparentDepthPrepass = true,
                supportTransparentDepthPostpass = true,
                colorBufferFormat = ColorBufferFormat.R11G11B10,
                supportCustomPass = true,
                customBufferFormat = CustomBufferFormat.R8G8B8A8,
                supportedLitShaderMode = SupportedLitShaderMode.DeferredOnly,
                supportDecals = true,
                supportDecalLayers = false,
                supportSurfaceGradient = true,
                decalNormalBufferHP = false,
                msaaSampleCount = MSAASamples.None,
                supportMotionVectors = true,
                supportRuntimeAOVAPI = false,
                supportDitheringCrossFade = true,
                supportTerrainHole = false,
                supportWater = false,
                waterSimulationResolution = WaterSimulationResolution.Medium128,
                waterCPUSimulation = false,
                supportDataDrivenLensFlare = true,
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

                supportRayTracing = false,
                supportedRayTracingMode = SupportedRayTracingMode.Both,
                lodBias = new FloatScalableSetting(new[] { 1.0f, 1, 1 }, ScalableSettingSchemaId.With3Levels),
                maximumLODLevel = new IntScalableSetting(new[] { 0, 0, 0 }, ScalableSettingSchemaId.With3Levels),
                lightProbeSystem = LightProbeSystem.LegacyLightProbes,
                probeVolumeMemoryBudget = ProbeVolumeTextureMemoryBudget.MemoryBudgetMedium,
                probeVolumeBlendingMemoryBudget = ProbeVolumeBlendingTextureMemoryBudget.MemoryBudgetLow,
                supportProbeVolumeStreaming = false,
                probeVolumeSHBands = ProbeVolumeSHBands.SphericalHarmonicsL1,
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
        public bool supportSubsurfaceScattering;
        /// <summary>Sample budget for the Subsurface Scattering algorithm.</summary>
        public IntScalableSetting sssSampleBudget;
        /// <summary>Support volumetric lighting.</summary>
        public bool supportVolumetrics;
        /// <summary>Support volumetric clouds.</summary>
        public bool supportVolumetricClouds;
        /// <summary>Support light layers.</summary>
        public bool supportLightLayers;
        /// <summary>Support Water Surfaces.</summary>
        public bool supportWater;
        /// <summary>Water simulation resolution</summary>
        public WaterSimulationResolution waterSimulationResolution;
        /// <summary>Enable water CPU simulation.</summary>
        public bool waterCPUSimulation;

        /// <summary>Name for light layer 0.</summary>
        public string lightLayerName0
        {
            get { return HDRenderPipelineGlobalSettings.instance.lightLayerName0; }
            set { HDRenderPipelineGlobalSettings.instance.lightLayerName0 = value; }
        }
        /// <summary>Name for light layer 1.</summary>
        public string lightLayerName1
        {
            get { return HDRenderPipelineGlobalSettings.instance.lightLayerName1; }
            set { HDRenderPipelineGlobalSettings.instance.lightLayerName1 = value; }
        }
        /// <summary>Name for light layer 2.</summary>
        public string lightLayerName2
        {
            get { return HDRenderPipelineGlobalSettings.instance.lightLayerName2; }
            set { HDRenderPipelineGlobalSettings.instance.lightLayerName2 = value; }
        }
        /// <summary>Name for light layer 3.</summary>
        public string lightLayerName3
        {
            get { return HDRenderPipelineGlobalSettings.instance.lightLayerName3; }
            set { HDRenderPipelineGlobalSettings.instance.lightLayerName3 = value; }
        }
        /// <summary>Name for light layer 4.</summary>
        public string lightLayerName4
        {
            get { return HDRenderPipelineGlobalSettings.instance.lightLayerName4; }
            set { HDRenderPipelineGlobalSettings.instance.lightLayerName4 = value; }
        }
        /// <summary>Name for light layer 5.</summary>
        public string lightLayerName5
        {
            get { return HDRenderPipelineGlobalSettings.instance.lightLayerName5; }
            set { HDRenderPipelineGlobalSettings.instance.lightLayerName5 = value; }
        }
        /// <summary>Name for light layer 6.</summary>
        public string lightLayerName6
        {
            get { return HDRenderPipelineGlobalSettings.instance.lightLayerName6; }
            set { HDRenderPipelineGlobalSettings.instance.lightLayerName6 = value; }
        }
        /// <summary>Name for light layer 7.</summary>
        public string lightLayerName7
        {
            get { return HDRenderPipelineGlobalSettings.instance.lightLayerName7; }
            set { HDRenderPipelineGlobalSettings.instance.lightLayerName7 = value; }
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
        /// <summary>Custom passes buffer format.</summary>
        public CustomBufferFormat customBufferFormat;
        /// <summary>Supported Lit shader modes.</summary>
        public SupportedLitShaderMode supportedLitShaderMode;
        /// <summary></summary>
        public PlanarReflectionAtlasResolutionScalableSetting planarReflectionResolution;
        /// <summary></summary>
        public ReflectionProbeResolutionScalableSetting cubeReflectionResolution;
        // Engine
        /// <summary>Support decals.</summary>
        public bool supportDecals;
        /// <summary>Support decal Layers.</summary>
        public bool supportDecalLayers;
        /// <summary>Support surface gradient for decal normal blending.</summary>
        public bool supportSurfaceGradient;
        /// <summary>High precision normal buffer.</summary>
        public bool decalNormalBufferHP;
        /// <summary>Name for decal layer 0.</summary>
        public string decalLayerName0
        {
            get { return HDRenderPipelineGlobalSettings.instance.decalLayerName0; }
            set { HDRenderPipelineGlobalSettings.instance.decalLayerName0 = value; }
        }
        /// <summary>Name for decal layer 1.</summary>
        public string decalLayerName1
        {
            get { return HDRenderPipelineGlobalSettings.instance.decalLayerName1; }
            set { HDRenderPipelineGlobalSettings.instance.decalLayerName1 = value; }
        }
        /// <summary>Name for decal layer 2.</summary>
        public string decalLayerName2
        {
            get { return HDRenderPipelineGlobalSettings.instance.decalLayerName2; }
            set { HDRenderPipelineGlobalSettings.instance.decalLayerName2 = value; }
        }
        /// <summary>Name for decal layer 3.</summary>
        public string decalLayerName3
        {
            get { return HDRenderPipelineGlobalSettings.instance.decalLayerName3; }
            set { HDRenderPipelineGlobalSettings.instance.decalLayerName3 = value; }
        }
        /// <summary>Name for decal layer 4.</summary>
        public string decalLayerName4
        {
            get { return HDRenderPipelineGlobalSettings.instance.decalLayerName4; }
            set { HDRenderPipelineGlobalSettings.instance.decalLayerName4 = value; }
        }
        /// <summary>Name for decal layer 5.</summary>
        public string decalLayerName5
        {
            get { return HDRenderPipelineGlobalSettings.instance.decalLayerName5; }
            set { HDRenderPipelineGlobalSettings.instance.decalLayerName5 = value; }
        }
        /// <summary>Name for decal layer 6.</summary>
        public string decalLayerName6
        {
            get { return HDRenderPipelineGlobalSettings.instance.decalLayerName6; }
            set { HDRenderPipelineGlobalSettings.instance.decalLayerName6 = value; }
        }
        /// <summary>Name for decal layer 7.</summary>
        public string decalLayerName7
        {
            get { return HDRenderPipelineGlobalSettings.instance.decalLayerName7; }
            set { HDRenderPipelineGlobalSettings.instance.decalLayerName7 = value; }
        }

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
        /// <summary>Support Data Driven Lens Flare.</summary>
        public bool supportDataDrivenLensFlare;

        /// <summary>Support runtime debug display.</summary>
        public bool supportRuntimeDebugDisplay
        {
            get => HDRenderPipelineGlobalSettings.instance.supportRuntimeDebugDisplay;
            set => HDRenderPipelineGlobalSettings.instance.supportRuntimeDebugDisplay = value;
        }

        internal bool supportProbeVolume => (lightProbeSystem == LightProbeSystem.ProbeVolumes);

        /// <summary>Support runtime AOV API.</summary>
        public bool supportRuntimeAOVAPI;
        /// <summary>Support dithered cross-fade.</summary>
        public bool supportDitheringCrossFade;
        /// <summary>Support terrain holes.</summary>
        public bool supportTerrainHole;
        /// <summary>Determines what system to use.</summary>
        public LightProbeSystem lightProbeSystem;
        /// <summary>Probe Volume Memory Budget.</summary>
        public ProbeVolumeTextureMemoryBudget probeVolumeMemoryBudget;
        /// <summary>Probe Volume Memory Budget for scenario blending.</summary>
        public ProbeVolumeBlendingTextureMemoryBudget probeVolumeBlendingMemoryBudget;
        /// <summary>Support Streaming for Probe Volumes.</summary>
        public bool supportProbeVolumeStreaming;
        /// <summary>Probe Volumes SH Bands.</summary>
        public ProbeVolumeSHBands probeVolumeSHBands;

        /// <summary>Support ray tracing.</summary>
        public bool supportRayTracing;
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
