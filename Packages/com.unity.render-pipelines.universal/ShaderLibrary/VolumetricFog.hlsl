#ifndef UNIVERSAL_VOLUMETRIC_FOG_INCLUDED
#define UNIVERSAL_VOLUMETRIC_FOG_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"

// Globals shared by the volumetric fog computes and surface shading. Set from
// C# before opaque rendering.
//
// When no Fog volume override is active, _VolumetricFogBaseExtinction defaults to 0,
// which makes TransmittanceHeightFog return 1.0 and the helper a no-op.
float  _VolumetricFogBaseExtinction;
float  _VolumetricFogBaseHeight;
float2 _VolumetricFogExponents;     // x = 1/H, y = H

// Builds the world-space view ray for a voxel column. dirWS is the normalized ray
// direction through the column center; tStart is the distance along it to the near plane.
void ReconstructColumnRayWS(uint2 voxelCoord, float4 vbufferSize, float4x4 coordToViewDirWS,
                            float cameraNearPlane, out float3 dirWS, out float tStart)
{
    float3 F = normalize(mul(-float4(0.5 * vbufferSize.x, 0.5 * vbufferSize.y, 1, 1), coordToViewDirWS).xyz);
    float2 centerCoord = voxelCoord + float2(0.5, 0.5);
    float3 rayDirWS = mul(-float4(centerCoord, 1, 1), coordToViewDirWS).xyz;
    dirWS  = rayDirWS * rsqrt(dot(rayDirWS, rayDirWS));
    tStart = cameraNearPlane / dot(F, dirWS);
}

// Returns the world-space center of a depth slice along the column ray. Slices are
// distributed logarithmically, so this decodes the slice's near and far depths and
// takes their midpoint.
float3 ComputeVoxelCenterWS(uint slice, float rcpSliceCount, float4 depthDecodingParams,
                            float3 cameraPositionWS, float3 dirWS, float tStart)
{
    float e0 = slice * rcpSliceCount;
    float t0 = max(tStart, DecodeLogarithmicDepthGeneralized(e0,                 depthDecodingParams));
    float t1 = max(tStart, DecodeLogarithmicDepthGeneralized(e0 + rcpSliceCount, depthDecodingParams));
    return cameraPositionWS + 0.5 * (t0 + t1) * dirWS;
}


// Returns the fraction of main-light radiance (0..1) that reaches positionWS after
// passing through the height-fog slab.
half ComputeMainLightVolumetricFogAttenuation(float3 positionWS, float3 mainLightDirection)
{
#if defined(_VOLUMETRIC_FOG)
    float cosZenithAngle = max(mainLightDirection.y, 0.001f);
    return TransmittanceHeightFog(_VolumetricFogBaseExtinction, _VolumetricFogBaseHeight,
                                  _VolumetricFogExponents, cosZenithAngle, positionWS.y);
#else
    return 1;
#endif
}

#endif
