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
        //Shaderpass_DBuffer_Projector, //check idx order isnt messed up by injecting this in the middle and not the end, shader has Pass Name: DBufferMesh, could be that
        ForwardEmissiveProjector,
        ForwardEmissiveMesh,
        Raytracing,
        RaytracingIndirect,
        RaytracingVisibility,
        RaytracingForward,
        RaytracingGBuffer,
        RaytracingSubSurface,
        PathTracing,
        Constant,
        FullScreenDebug,
    }
}
