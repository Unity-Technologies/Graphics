using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// History buffers available in HDCamera.
    /// </summary>
    public enum HDCameraFrameHistoryType
    {
        /// <summary>Color buffer mip chain.</summary>
        ColorBufferMipChain,
        /// <summary>Exposure buffer.</summary>
        Exposure,
        /// <summary>Temporal antialiasing history.</summary>
        TemporalAntialiasing,
        /// <summary>Velocity magnitude history used for TAA velocity weighting.</summary>
        TAAMotionVectorMagnitude,
        /// <summary>Depth of field CoC.</summary>
        DepthOfFieldCoC,
        /// <summary>Normal buffer.</summary>
        Normal,
        /// <summary>Depth buffer.</summary>
        Depth,
        /// <summary>Mip one of the depth buffer .</summary>
        Depth1,
        /// <summary>Ambient Occlusion buffer.</summary>
        AmbientOcclusion,
        /// <summary>Ray traced ambient occlusion buffer.</summary>
        RaytracedAmbientOcclusion,
        /// <summary>Ray traced shadow history buffer.</summary>
        RaytracedShadowHistory,
        /// <summary>Ray traced shadow history validity buffer.</summary>
        RaytracedShadowHistoryValidity,
        /// <summary>Ray traced shadow history distance buffer.</summary>
        RaytracedShadowDistanceValidity,
        /// <summary>Ray traced reflections distance buffer.</summary>
        RaytracedReflectionDistance,
        /// <summary>Ray traced reflections distance buffer.</summary>
        RaytracedReflectionAccumulation,
        /// <summary>Ray traced reflections stabilization buffer.</summary>
        RaytracedReflectionStabilization,
        /// <summary>Ray traced indirect diffuse HF buffer.</summary>
        RaytracedIndirectDiffuseHF,
        /// <summary>Ray traced indirect diffuse LF buffer.</summary>
        RaytracedIndirectDiffuseLF,
        /// <summary>Ray traced subsurface buffer.</summary>
        RayTracedSubSurface,
        /// <summary>Main path tracing output buffer.</summary>
        PathTracingOutput,
        /// <summary>Temporal antialiasing history after DoF.</summary>
        TemporalAntialiasingPostDoF,
        /// <summary>Volumetric clouds buffer 0.</summary>
        VolumetricClouds0,
        /// <summary>Volumetric clouds buffer 1.</summary>
        VolumetricClouds1,
        /// <summary>Screen Space Reflection Accumulation.</summary>
        ScreenSpaceReflectionAccumulation,
        /// <summary>Path-traced Albedo AOV.</summary>
        PathTracingAlbedo,
        /// <summary>Path-traced Normal AOV.</summary>
        PathTracingNormal,
        /// <summary>Path-traced motion vector AOV.</summary>
        PathTracingMotionVector,
        /// <summary>Path-traced volumetrics scattering AOV.</summary>
        PathTracingVolumetricFog,
        /// <summary>Denoised path-traced frame history.</summary>
        PathTracingDenoised,
        /// <summary>Denoised vpath-traced volumetrics scattering frame history.</summary>
        PathTracingVolumetricFogDenoised,
        /// <summary>Variable rate shading.</summary>
        Vrs,

        // For retro compatibility
        /// <summary>Main path tracing output buffer. It is recommended to use the PathTracingOutput enum value instead.</summary>
        [Obsolete]
        PathTracing = PathTracingOutput,
        /// <summary>Path-traced Albedo AOV. It is recommended to use the PathTracingAlbedo enum value instead.</summary>
        [Obsolete]
        AlbedoAOV = PathTracingAlbedo,
        /// <summary>Path-traced Normal AOV. It is recommended to use the PathTracingNormal enum value instead.</summary>
        [Obsolete]
        NormalAOV = PathTracingNormal,
        /// <summary>Path-traced motion vector AOV. It is recommended to use the PathTracingMotionVector enum value instead.</summary>
        [Obsolete]
        MotionVectorAOV = PathTracingMotionVector,
        /// <summary>Denoised path-traced frame history. It is recommended to use the PathTracingDenoised enum value instead.</summary>
        [Obsolete]
        DenoiseHistory = PathTracingDenoised
    }
}
