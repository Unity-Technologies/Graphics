using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    [Flags]
    public enum CapsuleOcclusionFlags
    {
        /// <summary>Fade out the occlusion effect if the surface is likely to be approximated by this capsule.</summary>
        FadeSelfShadow = (1 << 0),
        /// <summary>Fade out the occlusion effect as the occluder goes under the horizon of the surface.</summary>
        FadeAtHorizon = (1 << 1),
        /// <summary>Clip the capsule extents to the cone from the surface to the (spherical) light source.</summary>
        ClipToCone = (1 << 2),
        /// <summary>Clip the capsule to the plane from the surface to the light source center.</summary>
        ClipToPlane = (1 << 3),
        /// <summary>Ray traced reference (for comparison purposes only, too slow to ship with).</summary>
        RayTracedReference = (1 << 4),
        /// <summary>Consider the (clipped) capsule as an ellipsoid, scale down everything along the capsule axis to form a sphere for occlusion.</summary>
        CapsuleAxisScale = (1 << 5),
        /// <summary>Scale down everything along the light axis as the capsule axis and light axis align, to smooth the penumbra.</summary>
        LightAxisScale = (1 << 6),
    }

    [GenerateHLSL]
    [Flags]
    public enum CapsuleAmbientOcclusionFlags
    {
        /// <summary>Include the effect of ambient occlusion along the capsule axis.</summary>
        IncludeAxis = (1 << 0),
    }
}
