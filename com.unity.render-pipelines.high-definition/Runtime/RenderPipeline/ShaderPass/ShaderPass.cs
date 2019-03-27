using UnityEngine;
using System;

//-----------------------------------------------------------------------------
// structure definition
//-----------------------------------------------------------------------------
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL(PackingRules.Exact)]
    public enum ShaderPass
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
    }
}
