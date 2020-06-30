#ifndef UNITY_DECAL_PREPASS_BUFFER_INCLUDED
#define UNITY_DECAL_PREPASS_BUFFER_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

// ----------------------------------------------------------------------------
// Encoding/decoding normal buffer functions
// ----------------------------------------------------------------------------

struct DecalPrepassData
{
    float3  geomNormalWS;
    uint    decalLayerMask;
};

// NormalBuffer texture declaration
TEXTURE2D_X(_DecalPrepassTexture);

void EncodeIntoDecalPrepassBuffer(DecalPrepassData decalPrepassData, out float4 outDecalBuffer)
{
    float2 octNormalWS = PackNormalOctQuadEncode(decalPrepassData.geomNormalWS);
    float3 packNormalWS = PackFloat2To888(saturate(octNormalWS * 0.5 + 0.5));
    outDecalBuffer = float4(packNormalWS, decalPrepassData.decalLayerMask / 255.0);
}

void DecodeFromDecalPrepass(float4 decalBuffer, out DecalPrepassData decalPrepassData)
{
    float3 packNormalWS = decalBuffer0.xyz;
    float2 octNormalWS = Unpack888ToFloat2(packNormalWS);
    decalPrepassData.geomNormalWS = UnpackNormalOctQuadEncode(octNormalWS * 2.0 - 1.0);

    decalPrepassData.decalLayerMask = uint(decalBuffer0.w * 255.5);
}

void DecodeFromDecalPrepass(uint2 positionSS, out DecalPrepassData normalData)
{
    float4 decalBuffer = LOAD_TEXTURE2D_X(_DecalPrepassTexture, positionSS);
    DecodeFromNormalBuffer(decalBuffer, decalPrepassData);
}

#endif // UNITY_DECAL_PREPASS_BUFFER_INCLUDED
