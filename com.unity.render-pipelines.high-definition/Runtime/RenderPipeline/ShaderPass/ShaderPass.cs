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
        TransparentDepthPrepass,
        TransparentDepthPostpass,
        MotionVectors,
        Distortion,
        LightTransport,
        Shadows,
        SubsurfaceScattering,
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
        RayTracingDebug,
        Constant,
        FullScreenDebug,
    }
}
