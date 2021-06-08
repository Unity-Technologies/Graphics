using System;

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
        /// Bilinear upscaling filter.
        /// </summary>
        Bilinear,
        /// <summary>
        /// Bicubic Catmull-Rom upscaling filter.
        /// </summary>
        CatmullRom,
        /// <summary>
        /// Lanczos upscaling filter.
        /// </summary>
        Lanczos,
        /// <summary>
        /// Contrast Adaptive Sharpening upscaling filter.
        /// </summary>
        ContrastAdaptiveSharpen,
        /// <summary>
        /// Edge Adaptive Scaling Upres.
        /// </summary>
        [InspectorName("Edge Adaptive Scaling Upres")]
        EdgeAdaptiveScalingUpres
    }

    /// <summary>User-facing settings for dynamic resolution.</summary>
    [Serializable]
    public struct GlobalDynamicResolutionSettings
    {
        /// <summary>Default GlobalDynamicResolutionSettings</summary>
        /// <returns></returns>
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

            //Defaults for dlss
            enableDLSS = false,
            DLSSUseOptimalSettings = true,
            DLSSPerfQualitySetting = 0,
            DLSSSharpness = 0.5f
        };

        /// <summary>Select whether the dynamic resolution is enabled or not.</summary>
        public bool enabled;
        /// <summary>Offsets the mip bias to recover mode detail. This only works if the camera is utilizing TAA.</summary>
        public bool useMipBias;

        /// <summary>Toggle NVIDIA Deep Learning Super Sampling (DLSS).</summary>
        public bool enableDLSS;

        /// <summary>Opaque quality setting of NVIDIA Deep Learning Super Sampling (DLSS). Use the system enum UnityEngine.NVIDIA.DLSSQuality to set the quality.</summary>
        public uint DLSSPerfQualitySetting;

        /// <summary>Toggle NVIDIA Deep Learning Super Sampling (DLSS) automatic recommendation system for scaling and sharpness.
        /// If this is on, the manually established scale callback for Dynamic Resolution Scaling is ignored. The sharpness setting of DLSS is also ignored.
        /// </summary>
        public bool DLSSUseOptimalSettings;

        /// <summary>Pixel sharpness of NVIDIA Deep Leraning Super Sampling (DLSS).</summary>
        [Range(0, 1)]
        public float DLSSSharpness;

        /// <summary>The maximum resolution percentage that dynamic resolution can reach.</summary>
        public float maxPercentage;
        /// <summary>The minimum resolution percentage that dynamic resolution can reach.</summary>
        public float minPercentage;

        /// <summary>The type of dynamic resolution method.</summary>
        public DynamicResolutionType dynResType;
        /// <summary>The type of upscaling filter to use.</summary>
        public DynamicResUpscaleFilter upsampleFilter;

        /// <summary>Select whether dynamic resolution system will force a specific resolution percentage.</summary>
        public bool forceResolution;
        /// <summary>The resolution percentage forced in case forceResolution is set to true.</summary>
        public float forcedPercentage;

        /// <summary>The minimum percentage threshold allowed to clamp low resolution transparency. When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage.</summary>
        public float lowResTransparencyMinimumThreshold;
    }
}
