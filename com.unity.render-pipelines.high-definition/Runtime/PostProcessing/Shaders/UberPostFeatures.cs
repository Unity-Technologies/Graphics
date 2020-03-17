using System;

namespace UnityEngine.Rendering.HighDefinition
{
    // Must be kept in sync with variants defined in UberPost.compute
    [GenerateHLSL, Flags]
    internal enum UberPostFeatureFlags
    {
        None                      = 0,
        ChromaticAberration       = 1 << 0,
        Vignette                  = 1 << 1,
        LensDistortion            = 1 << 2,
        EnableAlpha               = 1 << 3
    }
}
