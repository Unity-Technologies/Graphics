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
        /// <summary>Ray traced reflections buffer.</summary>
        RaytracedReflection,
        /// <summary>Ray traced indirect diffuse HF buffer.</summary>
        RaytracedIndirectDiffuseHF,
        /// <summary>Ray traced indirect diffuse LF buffer.</summary>
        RaytracedIndirectDiffuseLF,
        /// <summary>Ray traced subsurface buffer.</summary>
        RayTracedSubSurface,
        /// <summary>Path tracing buffer.</summary>
        PathTracing,
        /// <summary>Temporal antialiasing history after DoF.</summary>
        TemporalAntialiasingPostDoF,
        /// <summary>Volumetric clouds buffer 0.</summary>
        VolumetricClouds0,
        /// <summary>Volumetric clouds buffer 1.</summary>
        VolumetricClouds1,
        /// <summary>Screen Space Reflection Accumulation.</summary>
        ScreenSpaceReflectionAccumulation,
        /// <summary>Number of history buffers.</summary>
        Count // TODO: Obsolete
    }
}
