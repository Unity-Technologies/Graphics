namespace UnityEngine.Rendering.HighDefinition
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
        RaytracedIndirectDiffuseHF,
        RaytracedIndirectDiffuseLF,
        PathTracing,
#endif
        Count
    }
}
