using System;
using UnityEngine.Experimental.Rendering;

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
            R8G8B8A8 = GraphicsFormat.R8G8B8A8_SNorm,
            /// <summary>R16G16B16A16 high quality HDR format.</summary>
            R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat,
            /// <summary>R11G11B10 medium quality HDR format.</summary>
            R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
        }

        internal static RenderPipelineSettings NewDefault() => new RenderPipelineSettings()
        {
            supportShadowMask = true,
            supportSSAO = true,
            supportSubsurfaceScattering = true,
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
            msaaSampleCount = MSAASamples.None,
            supportMotionVectors = true,
            supportRuntimeDebugDisplay = true,
            supportDitheringCrossFade = true,
            supportTerrainHole = false,

            lightLoopSettings = GlobalLightLoopSettings.NewDefault(),
            hdShadowInitParams = HDShadowInitParameters.NewDefault(),
            decalSettings = GlobalDecalSettings.NewDefault(),
            postProcessSettings = GlobalPostProcessSettings.NewDefault(),
            dynamicResolutionSettings = GlobalDynamicResolutionSettings.NewDefault(),
            lowresTransparentSettings = GlobalLowResolutionTransparencySettings.NewDefault(),
            xrSettings = GlobalXRSettings.NewDefault(),
            postProcessQualitySettings = GlobalPostProcessingQualitySettings.NewDefault(),
            lightingQualitySettings = GlobalLightingQualitySettings.NewDefault(),

            supportRayTracing = false,
            lodBias = new FloatScalableSetting(new[] { 1.0f, 1, 1 }, ScalableSettingSchemaId.With3Levels),
            maximumLODLevel = new IntScalableSetting(new[] { 0, 0, 0 }, ScalableSettingSchemaId.With3Levels),
            lightLayerName0 = "Light Layer default",
            lightLayerName1 = "Light Layer 1",
            lightLayerName2 = "Light Layer 2",
            lightLayerName3 = "Light Layer 3",
            lightLayerName4 = "Light Layer 4",
            lightLayerName5 = "Light Layer 5",
            lightLayerName6 = "Light Layer 6",
            lightLayerName7 = "Light Layer 7",
        };

        /// <summary>
        /// Light Settings.
        /// </summary>
        [Serializable]
        public struct LightSettings
        {
            /// <summary>Enable contact shadows.</summary>
            public BoolScalableSetting useContactShadow;
        }

        // Lighting
        /// <summary>Support shadow masks.</summary>
        public bool supportShadowMask;
        /// <summary>Support screen space reflections.</summary>
        public bool supportSSR;
        /// <summary>Support screen space ambient occlusion.</summary>
        public bool supportSSAO;
        /// <summary>Support subsurface scattering.</summary>
        public bool supportSubsurfaceScattering;
        /// <summary>High quality subsurface scattering.</summary>
        public bool increaseSssSampleCount;
        /// <summary>Support volumetric lighting.</summary>
        public bool supportVolumetrics;
        /// <summary>High quality volumetric lighting.</summary>
        public bool increaseResolutionOfVolumetrics;
        /// <summary>Support light layers.</summary>
        public bool supportLightLayers;
        /// <summary>Name for light layer 0.</summary>
        public string lightLayerName0;
        /// <summary>Name for light layer 1.</summary>
        public string lightLayerName1;
        /// <summary>Name for light layer 2.</summary>
        public string lightLayerName2;
        /// <summary>Name for light layer 3.</summary>
        public string lightLayerName3;
        /// <summary>Name for light layer 4.</summary>
        public string lightLayerName4;
        /// <summary>Name for light layer 5.</summary>
        public string lightLayerName5;
        /// <summary>Name for light layer 6.</summary>
        public string lightLayerName6;
        /// <summary>Name for light layer 7.</summary>
        public string lightLayerName7;
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

        // Engine
        /// <summary>Support decals.</summary>
        public bool supportDecals;

        /// <summary>Number of samples when using MSAA.</summary>
        public MSAASamples msaaSampleCount;
        /// <summary>Support MSAA.</summary>
        public bool supportMSAA => msaaSampleCount != MSAASamples.None;

        // Returns true if the output of the rendering passes support an alpha channel
        internal bool supportsAlpha => colorBufferFormat == ColorBufferFormat.R16G16B16A16;

        /// <summary>Support motion vectors.</summary>
        public bool supportMotionVectors;
        /// <summary>Support runtime debug display.</summary>
        public bool supportRuntimeDebugDisplay;
        /// <summary>Support dithered cross-fade.</summary>
        public bool supportDitheringCrossFade;
        /// <summary>Support terrain holes.</summary>
        public bool supportTerrainHole;
        /// <summary>Support ray tracing.</summary>
        public bool supportRayTracing;

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
    }
}
