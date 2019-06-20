using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{

    public enum DynamicResolutionType : byte
    {
        Software,
        //Hardware,   // Has lots of problems on platform. Disabling this while we investigate.
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
    public class GlobalDynamicResolutionSettings
    {
        /// <summary>Default GlobalDynamicResolutionSettings</summary>
        public static readonly GlobalDynamicResolutionSettings @default = new GlobalDynamicResolutionSettings()
        {
            maxPercentage = 100.0f,
            minPercentage = 100.0f,
            dynResType = DynamicResolutionType.Software,
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
