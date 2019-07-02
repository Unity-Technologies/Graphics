namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum HDCameraFrameHistoryType
    {
        ColorBufferMipChain,
        VolumetricLighting,
        Exposure,
        TemporalAntialiasing,
        DepthOfFieldCoC,
        Normal,
        Depth,
        AmbientOcclusion,
#if ENABLE_RAYTRACING
        RaytracedAmbientOcclusion,
        RaytracedShadow,
        RaytracedAreaAnalytic,
        RaytracedReflection,
        RaytracedIndirectDiffuse,
#endif
        Count
    }
}
