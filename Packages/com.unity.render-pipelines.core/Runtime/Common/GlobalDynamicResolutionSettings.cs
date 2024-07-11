using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Types of dynamic resolution that can be requested. Note that if Hardware is selected, but not available on the platform, the system will fallback to Software.
    /// </summary>
    public enum DynamicResolutionType : byte
    {
        /// <summary>
        /// Software dynamic resolution.
        /// </summary>
        Software,
        /// <summary>
        /// Hardware dynamic resolution.
        /// </summary>
        Hardware,
    }

    /// <summary>
    /// Types of filters that can be used to upscale rendered result to native resolution.
    /// </summary>
    public enum DynamicResUpscaleFilter : byte
    {
        /// <summary>
        /// Bilinear upscaling filter. Obsolete and not supported.
        /// </summary>
        [Obsolete("Bilinear upscale filter is considered obsolete and is not supported anymore, please use CatmullRom for a very cheap, but blurry filter.", false)] Bilinear,
        /// <summary>
        /// Bicubic Catmull-Rom upscaling filter.
        /// </summary>
        CatmullRom,
        /// <summary>
        /// Lanczos upscaling filter. Obsolete and not supported.
        /// </summary>
        [Obsolete("Lanczos upscale filter is considered obsolete and is not supported anymore, please use Contrast Adaptive Sharpening for very sharp filter or FidelityFX Super Resolution 1.0.", false)] Lanczos,
        /// <summary>
        /// Contrast Adaptive Sharpening upscaling filter.
        /// </summary>
        ContrastAdaptiveSharpen,
        /// <summary>
        /// FidelityFX Super Resolution 1.0
        /// </summary>
        [InspectorName("FidelityFX Super Resolution 1.0")]
        EdgeAdaptiveScalingUpres,
        /// <summary>
        /// Temporal Upscaling.
        /// </summary>
        [InspectorName("TAA Upscale")]
        TAAU
    }

    /// <summary>User-facing settings for advanced upscalers.</summary>
    public enum AdvancedUpscalers : byte
    {
        /// <summary>
        /// NVIDIA Deep Learning Super Sampling (DLSS).
        /// </summary>
        [InspectorName("Deep Learning Super Sampling (DLSS)")]
        DLSS = 0,
        /// <summary>
        /// AMD FidelityFX Super Resolution (FSR2).
        /// </summary>
        [InspectorName("FidelityFX Super Resolution 2.0 (FSR2)")]
        FSR2 = 1,
        /// <summary>
        /// Spatial-Temporal Post-Processing
        /// </summary>
        [InspectorName("Spatial-Temporal Post-Processing (STP)")]
        STP = 2
    }

    /// <summary>User-facing settings for dynamic resolution.</summary>
    [Serializable]
    public struct GlobalDynamicResolutionSettings
    {
        /// <summary>Default GlobalDynamicResolutionSettings</summary>
        /// <returns>A GlobalDynamicResolutionSettings instance initialized with default values.</returns>
        public static GlobalDynamicResolutionSettings NewDefault() => new GlobalDynamicResolutionSettings()
        {
            useMipBias = false,
            maxPercentage = 100.0f,
            minPercentage = 100.0f,
            // It fall-backs to software when not supported, so it makes sense to have it on by default.
            dynResType = DynamicResolutionType.Hardware,
            upsampleFilter = DynamicResUpscaleFilter.CatmullRom,
            forcedPercentage = 100.0f,
            lowResTransparencyMinimumThreshold = 0.0f,
            lowResVolumetricCloudsMinimumThreshold = 50.0f,
            rayTracingHalfResThreshold = 50.0f,

            DLSSUseOptimalSettings = true,
            DLSSPerfQualitySetting = 0,
            DLSSSharpness = 0.5f,
            DLSSInjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.BeforePost,
            FSR2InjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.BeforePost,
            TAAUInjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.BeforePost,
            defaultInjectionPoint = DynamicResolutionHandler.UpsamplerScheduleType.AfterPost,
            advancedUpscalersByPriority = new List<AdvancedUpscalers>() { AdvancedUpscalers.STP },

            fsrOverrideSharpness = false,
            fsrSharpness = FSRUtils.kDefaultSharpnessLinear
        };

        /// <summary>Select whether the dynamic resolution is enabled or not.</summary>
        public bool enabled;
        /// <summary>Offsets the mip bias to recover mode detail. This only works if the camera is utilizing TAA.</summary>
        public bool useMipBias;

        /// <summary> Enables upsamplers available for certain platforms by priority. </summary>
        public List<AdvancedUpscalers> advancedUpscalersByPriority;

        /// <summary>Opaque quality setting of NVIDIA Deep Learning Super Sampling (DLSS). Use the system enum UnityEngine.NVIDIA.DLSSQuality to set the quality.</summary>
        public uint DLSSPerfQualitySetting;

        /// <summary>The injection point at which to apply DLSS upscaling.</summary>
        public DynamicResolutionHandler.UpsamplerScheduleType DLSSInjectionPoint;

        /// <summary>The injection point at which to apply TAAU upsampling.</summary>
        public DynamicResolutionHandler.UpsamplerScheduleType TAAUInjectionPoint;

        /// <summary>The injection point at which to apply STP upsampling.</summary>
        public DynamicResolutionHandler.UpsamplerScheduleType STPInjectionPoint;

        /// <summary>The injection point at which to apply the fallback upsampling.</summary>
        public DynamicResolutionHandler.UpsamplerScheduleType defaultInjectionPoint;

        /// <summary>Toggle NVIDIA Deep Learning Super Sampling (DLSS) automatic recommendation system for scaling and sharpness.
        /// If this is on, the manually established scale callback for Dynamic Resolution Scaling is ignored. The sharpness setting of DLSS is also ignored.
        /// </summary>
        public bool DLSSUseOptimalSettings;

        /// <summary>Pixel sharpness of NVIDIA Deep Leraning Super Sampling (DLSS).</summary>
        [Range(0, 1)]
        public float DLSSSharpness;

        /// <summary>Enable sharpness control for FidelityFX 2.0 Super Resolution (FSR2).</summary>
        public bool FSR2EnableSharpness;

        /// <summary>Pixel sharpness of AMD FidelityFX 2.0 Super Resolution (FSR2).</summary>
        [Range(0, 1)]
        public float FSR2Sharpness;

        /// <summary>Toggle AMD FidelityFX 2.0 Super Resolution (FSR2) automatic recommendation system for scaling.
        /// If this is on, the manually established scale callback for Dynamic Resolution Scaling is ignored.
        /// </summary>
        public bool FSR2UseOptimalSettings;

        /// <summary>Opaque quality setting of AMD FidelityFX 2.0 Super Resolution (FSR2). Use the system enum UnityEngine.AMD.FSR2Quality to set the quality.</summary>
        public uint FSR2QualitySetting;

        /// <summary>The injection point at which to apply FSR2 upscaling.</summary>
        public DynamicResolutionHandler.UpsamplerScheduleType FSR2InjectionPoint;

        /// <summary>Toggle sharpness override for AMD FidelityFX Super Resolution (FSR).
        /// If this is on, a sharpness value specified by the user will be used instead of the default.
        /// </summary>
        public bool fsrOverrideSharpness;

        /// <summary>Pixel sharpness of AMD FidelityFX Super Resolution (FSR).</summary>
        [Range(0, 1)]
        public float fsrSharpness;

        /// <summary>The maximum resolution percentage that dynamic resolution can reach.</summary>
        public float maxPercentage;
        /// <summary>The minimum resolution percentage that dynamic resolution can reach.</summary>
        public float minPercentage;

        /// <summary>The type of dynamic resolution method.</summary>
        public DynamicResolutionType dynResType;
        /// <summary>The default of upscaling filter used. It can be overridden via the API DynamicResolutionHandler.SetUpscaleFilter </summary>
        public DynamicResUpscaleFilter upsampleFilter;

        /// <summary>Select whether dynamic resolution system will force a specific resolution percentage.</summary>
        public bool forceResolution;
        /// <summary>The resolution percentage forced in case forceResolution is set to true.</summary>
        public float forcedPercentage;

        /// <summary>The minimum percentage threshold allowed to clamp low resolution transparency. When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage.</summary>
        public float lowResTransparencyMinimumThreshold;

        /// <summary>The minimum percentage threshold allowed to render ray tracing effects at half resolution. When the resolution percentage falls below this threshold, HDRP will render ray tracing effects at full resolution.</summary>
        public float rayTracingHalfResThreshold;

        /// <summary>The minimum percentage threshold allowed to clamp low resolution for SSGI (Screen Space Global Illumination). When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage.</summary>
        public float lowResSSGIMinimumThreshold;

        /// <summary>The minimum percentage threshold allowed to clamp tracing resolution for Volumetric Clouds. When the resolution percentage falls below this threshold, HDRP will trace the Volumetric Clouds in half res.</summary>
        public float lowResVolumetricCloudsMinimumThreshold;

#pragma warning disable 618 // Type or member is obsolete
        /// <summary>Obsolete, used only for data migration. Use the advancedUpscalersByPriority list instead to add the proper supported advanced upscaler by priority.</summary>
        [Obsolete("Obsolete, used only for data migration. Use the advancedUpscalersByPriority list instead to add the proper supported advanced upscaler by priority.")]
        public bool enableDLSS;
#pragma warning restore 618
    }
}
