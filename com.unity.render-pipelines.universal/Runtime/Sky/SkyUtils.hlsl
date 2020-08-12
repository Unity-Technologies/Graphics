#ifndef __SKYUTILS_H__
#define __SKYUTILS_H__

float4x4 _PixelCoordToViewDirWS;
// TODO Stereo

// Generates a world-space view direction for sky and atmospheric effects
float3 GetSkyViewDirWS(float2 positionCS)
{
    float4 viewDirWS = mul(float4(positionCS.xy, 1.0f, 1.0f), _PixelCoordToViewDirWS); // TODO TAA jitter adjustment
    return normalize(viewDirWS.xyz);
}

#endif // __SKYUTILS_H__
