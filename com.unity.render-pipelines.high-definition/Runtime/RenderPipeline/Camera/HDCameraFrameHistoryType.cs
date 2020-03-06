namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// History buffers available in HDCamera.
    /// </summary>
    public enum HDCameraFrameHistoryType
    {
        /// <summary>Color buffer mip chain.</summary>
        ColorBufferMipChain,
        /// <summary>Volumetric lighting buffer.</summary>
        VolumetricLighting,
        /// <summary>Exposure buffer.</summary>
        Exposure,
        /// <summary>Temporal antialiasing history.</summary>
        TemporalAntialiasing,
        /// <summary>Depth of field CoC.</summary>
        DepthOfFieldCoC,
        /// <summary>Normal buffer.</summary>
        Normal,
        /// <summary>Depth buffer.</summary>
        Depth,
        /// <summary>Ambient Occlusion buffer.</summary>
        AmbientOcclusion,
        /// <summary>Ray traced ambient occlusion buffer.</summary>
        RaytracedAmbientOcclusion,
        /// <summary>Ray traced shadow history buffer.</summary>
        RaytracedShadowHistory,
        /// <summary>Ray traced shadow history validity buffer.</summary>
        RaytracedShadowHistoryValidity,
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
        /// <summary>Number of history buffers.</summary>
        Count
    }
}
