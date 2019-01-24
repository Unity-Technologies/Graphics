using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Must be kept in sync with variants defined in UberPost.compute
    [GenerateHLSL, Flags]
    public enum UberPostFeatureFlags
    {
        None                      = 0,
        ChromaticAberration       = 1 << 0,
        Vignette                  = 1 << 1,
        LensDistortion            = 1 << 2
    }
}
