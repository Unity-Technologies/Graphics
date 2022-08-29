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
// layout: xy = normals, z = decal layer mask, w = light layer mask (written during gbuffer pass)
TEXTURE2D_X(_DecalPrepassTexture);

void EncodeIntoDecalPrepassBuffer(DecalPrepassData decalPrepassData, out float4 outDecalBuffer)
{
    float2 packNormalWS = saturate(PackNormalOctQuadEncode(decalPrepassData.geomNormalWS).xy * 0.5f + 0.5f);
    outDecalBuffer = float4(packNormalWS, decalPrepassData.decalLayerMask / 255.0, 0);
}

void DecodeFromDecalPrepass(float4 decalBuffer, out DecalPrepassData decalPrepassData)
{
    decalPrepassData.geomNormalWS   = UnpackNormalOctQuadEncode(decalBuffer.xy * 2.0 - 1.0);
    decalPrepassData.decalLayerMask = uint(decalBuffer.z * 255.5);
}

void DecodeFromDecalPrepass(uint2 positionSS, out DecalPrepassData decalPrepassData)
{
    float4 decalBuffer = LOAD_TEXTURE2D_X(_DecalPrepassTexture, positionSS);
    DecodeFromDecalPrepass(decalBuffer, decalPrepassData);
}

float DecodeAngleFade(float cosAngle, float2 angleFade)
{
    // See equation in DecalSystem.cs - simplified to a madd mul madd here
    return saturate((cosAngle*cosAngle + 1.25) * cosAngle * angleFade.x + angleFade.y);
}

#endif // UNITY_DECAL_PREPASS_BUFFER_INCLUDED
