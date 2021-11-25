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

        RequiresDeferredLighting = (1 << 1),
        SubsurfaceScattering = (1 << 2),     //  SSS, Split Lighting
        TraceReflectionRay = (1 << 3),     //  SSR or RTR - slot is reuse in transparent
        Decals = (1 << 4),     //  Use to tag when an Opaque Decal is render into DBuffer
        ObjectMotionVector = (1 << 5),     //  Animated object (for motion blur, SSR, SSAO, TAA)

        // --- Stencil  is cleared after opaque rendering has finished ---

        // --- Following bits are used exclusively for what happens after opaque ---
        ExcludeFromTAA = (1 << 1),    // Disable Temporal Antialiasing for certain objects
        DistortionVectors = (1 << 2),    // Distortion pass - reset after distortion pass, shared with SMAA
        SMAA = (1 << 2),    // Subpixel Morphological Antialiasing
        // Reserved TraceReflectionRay = (1 << 3) for transparent SSR or RTR
        AfterOpaqueReservedBits = 0x38,        // Reserved for future usage

        // --- Following are user bits, we don't touch them inside HDRP and is up to the user to handle them ---
        UserBit0 = (1 << 6),
        UserBit1 = (1 << 7),

        HDRPReservedBits = 255 & ~(UserBit0 | UserBit1),
    }

    /// <summary>
    /// Stencil bit exposed to user and not reserved by HDRP.
    /// Note that it is important that the Write Mask used in conjunction with these bits includes only this bits.
    /// For example if you want to tag UserBit0, the shaderlab code for the stencil state setup would look like:
    ///
    ///         WriteMask 64 // Value of UserBit0
    ///         Ref  64 // Value of UserBit0
    ///         Comp Always
    ///         Pass Replace
    ///
    /// Or if for example you want to write UserBit0 and zero out the UserBit1,  the shaderlab code for the stencil state setup would look like:
    ///
    ///         WriteMask MyWriteMask // with MyWriteMask define in C# as MyWriteMask = (UserStencilUsage.UserBit0 | UserStencilUsage.UserBit1)
    ///         Ref MyRef // with MyRef define in C# as MyRef = UserStencilUsage.UserBit0
    ///         Comp Always
    ///         Pass Replace
    /// </summary>
    public enum UserStencilUsage
    {
        /// <summary>User stencil bit 0.</summary>
        UserBit0 = StencilUsage.UserBit0,
        /// <summary>User stencil bit 1.</summary>
        UserBit1 = StencilUsage.UserBit1
    }
}
