using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.HighDefinition
{

    [GenerateHLSL]
    internal enum StencilUsage
    {
        Clear = 0,

        // Note: first bit is free and can still be used by both phases.

        // --- Following bits are used before transparent rendering ---

        RequiresDeferredLighting    = (1 << 1),
        SubsurfaceScattering        = (1 << 2),     //  SSS, Split Lighting
        TraceReflectionRay          = (1 << 3),     //  SSR or RTR
        Decals                      = (1 << 4),     //  Used for surfaces that receive decals
        ObjectMotionVector          = (1 << 5),     //  Animated object (for motion blur, SSR, SSAO, TAA)

        // --- Stencil  is cleared after opaque rendering has finished ---

        // --- Following bits are used exclusively for what happens after opaque ---
        ExcludeFromTAA              = (1 << 1),    // Disable Temporal Antialiasing for certain objects
        DistortionVectors           = (1 << 2),    // Distortion pass - reset after distortion pass, shared with SMAA
        SMAA                        = (1 << 2),    // Subpixel Morphological Antialiasing
        AfterOpaqueReservedBits     = 0x38,        // Reserved for future usage

        // --- Following are user bits, we don't touch them inside HDRP and is up to the user to handle them ---
        UserBit0 = (1 << 6),
        UserBit1 = (1 << 7),

        HDRPReservedBits = 255 & ~(UserBit0 | UserBit1),
    }
}
