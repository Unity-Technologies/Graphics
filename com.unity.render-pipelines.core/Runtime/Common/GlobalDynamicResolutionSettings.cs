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
    }

    /// <summary>User-facing settings for dynamic resolution.</summary>
    [Serializable]
    public struct GlobalDynamicResolutionSettings
    {
        /// <summary>Default GlobalDynamicResolutionSettings</summary>
        /// <returns></returns>
        public static GlobalDynamicResolutionSettings NewDefault() => new GlobalDynamicResolutionSettings()
        {
            maxPercentage = 100.0f,
            minPercentage = 100.0f,
            // It fall-backs to software when not supported, so it makes sense to have it on by default.
            dynResType = DynamicResolutionType.Hardware,
            upsampleFilter = DynamicResUpscaleFilter.CatmullRom,
            forcedPercentage = 100.0f
        };

        /// <summary>Select whether the dynamic resolution is enabled or not.</summary>
        public bool enabled;

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
    }
}
