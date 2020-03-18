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
    [Serializable]
    public struct RenderPipelineSettings
    {
        public enum SupportedLitShaderMode
        {
            ForwardOnly = 1 << 0,
            DeferredOnly = 1 << 1,
            Both = ForwardOnly | DeferredOnly
        }

        public enum RaytracingTier
        {
            Tier1 = 1 << 0,
            Tier2 = 1 << 1
        }

        public enum ColorBufferFormat
        {
            R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
            R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat
        }

        public enum CustomBufferFormat
        {
            R8G8B8A8 = GraphicsFormat.R8G8B8A8_SNorm,
            R16G16B16A16 = GraphicsFormat.R16G16B16A16_SFloat,
            R11G11B10 = GraphicsFormat.B10G11R11_UFloatPack32,
        }

        /// <summary>Default RenderPipelineSettings</summary>
        [Obsolete("Since 2019.3, use RenderPipelineSettings.NewDefault() instead.")]
        public static readonly RenderPipelineSettings @default = default;
        /// <summary>Default RenderPipelineSettings</summary>
        public static RenderPipelineSettings NewDefault() => new RenderPipelineSettings()
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
            supportedRaytracingTier = RaytracingTier.Tier2,
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

        [Serializable]
        public struct LightSettings
        {
            public BoolScalableSetting useContactShadow;
        }

        // Lighting
        public bool supportShadowMask;
        public bool supportSSR;
        public bool supportSSAO;
        public bool supportSubsurfaceScattering;
        public bool increaseSssSampleCount;
        public bool supportVolumetrics;
        public bool increaseResolutionOfVolumetrics;
        public bool supportLightLayers;
        public string lightLayerName0;
        public string lightLayerName1;
        public string lightLayerName2;
        public string lightLayerName3;
        public string lightLayerName4;
        public string lightLayerName5;
        public string lightLayerName6;
        public string lightLayerName7;
        public bool supportDistortion;
        public bool supportTransparentBackface;
        public bool supportTransparentDepthPrepass;
        public bool supportTransparentDepthPostpass;
        public ColorBufferFormat colorBufferFormat;
        public bool supportCustomPass;
        public CustomBufferFormat customBufferFormat;
        public SupportedLitShaderMode supportedLitShaderMode;

        // Engine
        public bool supportDecals;

        public MSAASamples msaaSampleCount;
        public bool supportMSAA => msaaSampleCount != MSAASamples.None;

        public bool keepAlpha => colorBufferFormat == ColorBufferFormat.R16G16B16A16;

        public bool supportMotionVectors;
        public bool supportRuntimeDebugDisplay;
        public bool supportDitheringCrossFade;
        public bool supportTerrainHole;
        public bool supportRayTracing;
        public RaytracingTier supportedRaytracingTier;

        public GlobalLightLoopSettings lightLoopSettings;
        public HDShadowInitParameters hdShadowInitParams;
        public GlobalDecalSettings decalSettings;
        public GlobalPostProcessSettings postProcessSettings;
        public GlobalDynamicResolutionSettings dynamicResolutionSettings;
        public GlobalLowResolutionTransparencySettings lowresTransparentSettings;
        public GlobalXRSettings xrSettings;
        public GlobalPostProcessingQualitySettings postProcessQualitySettings;

        public LightSettings lightSettings;
        public IntScalableSetting maximumLODLevel;
        public FloatScalableSetting lodBias;

        public GlobalLightingQualitySettings lightingQualitySettings;
    }
}
