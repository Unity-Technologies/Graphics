using System;

namespace UnityEngine.Rendering
{

    public enum DynamicResolutionType : byte
    {
        Software,
        Hardware,
        //Temporal    // Not yet supported
    }

    public enum DynamicResUpscaleFilter : byte
    {
        Bilinear,
        CatmullRom,
        Lanczos,
        // Difference of Gaussians? [aka unsharp]
    }

    [Serializable]
    public struct GlobalDynamicResolutionSettings
    {
        /// <summary>Default GlobalDynamicResolutionSettings</summary>
        public static readonly GlobalDynamicResolutionSettings @default = new GlobalDynamicResolutionSettings()
        {
            maxPercentage = 100.0f,
            minPercentage = 100.0f,
            // It fall-backs to software when not supported, so it makes sense to have it on by default.
            dynResType = DynamicResolutionType.Hardware,
            upsampleFilter = DynamicResUpscaleFilter.CatmullRom,
            forcedPercentage = 100.0f
        };

        public bool enabled;

        public float maxPercentage;
        public float minPercentage;

        public DynamicResolutionType dynResType;
        public DynamicResUpscaleFilter upsampleFilter;

        public bool forceResolution;
        public float forcedPercentage;
    }
}
