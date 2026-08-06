#ifndef UNIVERSAL_PACK_NORMALS_TEXTURE_INCLUDED
#define UNIVERSAL_PACK_NORMALS_TEXTURE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// Converts a world-space normal to a packed output format suited for a URP normals texture.
// Note that this function does not normalize normalWS, manually normalizing before packing is required.
half3 PackNormalWSToTexture(half3 normalWS)
{
    half3 result = normalWS;                                                // values between [-1, +1]

#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);                 // values between [-1, +1], must use fp32 on some platforms.
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);         // values between [ 0, +1]
    result = half3(PackFloat2To888(remappedOctNormalWS));                   // values between [ 0, +1]
#endif

    return result;
}

// Converts a packed normal sampled from a URP normals texture back to a world-space normal.
half3 UnpackNormalWSFromTexture(half3 packedNormal)
{
    half3 result = packedNormal;                                            // values between [-1, +1]

#if defined(_GBUFFER_NORMALS_OCT)
    float2 remappedOctNormalWS = Unpack888ToFloat2(packedNormal);           // values between [ 0, +1], must use fp32 to preserve the 12-bit payload.
    float2 octNormalWS = remappedOctNormalWS * 2.0 - 1.0;                   // values between [-1, +1]
    result = half3(UnpackNormalOctQuadEncode(octNormalWS));                 // values between [-1, +1]
#endif

    return result;                                                          // values between [-1, +1]
}

#endif
