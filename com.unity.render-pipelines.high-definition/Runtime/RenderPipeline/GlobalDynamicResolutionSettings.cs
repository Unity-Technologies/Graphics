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
        [SerializeField]
        public bool enabled = false;

        [SerializeField]
        public float maxPercentage = 100.0f;

        [SerializeField]
        public float minPercentage = 100.0f;

        [SerializeField]
        public DynamicResolutionType dynResType = DynamicResolutionType.Software;

        [SerializeField]
        public DynamicResUpscaleFilter upsampleFilter = DynamicResUpscaleFilter.CatmullRom;

        [SerializeField]
        public bool forceResolution = false;

        [SerializeField]
        public float forcedPercentage = 100.0f;
    }
}
