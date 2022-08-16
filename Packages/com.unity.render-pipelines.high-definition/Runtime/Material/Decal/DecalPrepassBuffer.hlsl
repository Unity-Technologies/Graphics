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
    uint    renderingLayerMask;
};

// NormalBuffer texture declaration (written during prepass and gbuffer pass)
// If rendering layer buffer and no decal layers, buffer is still allocated but only first 16 bits
// layout: xy = rendering layer mask, zw = geometric normal
TEXTURE2D_X(_DecalPrepassTexture);

void EncodeIntoDecalPrepassBuffer(DecalPrepassData decalPrepassData, out float4 outDecalBuffer)
{
    outDecalBuffer.x = (decalPrepassData.renderingLayerMask >> 8) / 255.0;
    outDecalBuffer.y = (decalPrepassData.renderingLayerMask & 0xFF) / 255.0;
    outDecalBuffer.zw = saturate(PackNormalOctQuadEncode(decalPrepassData.geomNormalWS).xy * 0.5f + 0.5f);
}

void DecodeFromDecalPrepass(float4 decalBuffer, out DecalPrepassData decalPrepassData)
{
    decalPrepassData.geomNormalWS = UnpackNormalOctQuadEncode(decalBuffer.zw * 2.0 - 1.0);
    decalPrepassData.renderingLayerMask = UnpackMeshRenderingLayerMask(decalBuffer);
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
