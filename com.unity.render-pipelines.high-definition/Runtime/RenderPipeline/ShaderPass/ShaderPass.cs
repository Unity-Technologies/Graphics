//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(PackingRules.Exact)]
    enum ShaderPass
    {
        GBuffer,
        Forward,
        ForwardUnlit,
        DeferredLighting,
        DepthOnly,
        MotionVectors,
        Distortion,
        LightTransport,
        Shadows,
        SubsurfaceScattering,
        VolumeVoxelization,
        VolumetricLighting,
        DbufferProjector,
        DbufferMesh,
        ForwardEmissiveProjector,
        ForwardEmissiveMesh,
        Raytracing,
        RaytracingIndirect,
        RaytracingVisibility,
        RaytracingForward,
        RaytracingGBuffer,
        RaytracingSubSurface,
        PathTracing,
    }
}
