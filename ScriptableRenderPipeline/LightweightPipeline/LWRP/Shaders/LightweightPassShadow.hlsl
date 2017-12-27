#ifndef LIGHTWEIGHT_PASS_SHADOW_INCLUDED
#define LIGHTWEIGHT_PASS_SHADOW_INCLUDED

#include "LightweightShaderLibrary/Core.hlsl"

// x: global clip space bias, y: normal world space bias
float4 _ShadowBias;

struct VertexInput
{
    float4 position : POSITION;
    float3 normal   : NORMAL;
};

float4 ShadowPassVertex(VertexInput v) : SV_POSITION
{
    float3 positionWS = TransformObjectToWorld(v.position.xyz);
    float3 normalWS = TransformObjectToWorldDir(v.normal);

    // normal bias is negative since we want to apply an inset normal offset
    positionWS = normalWS * _ShadowBias.yyy + positionWS;
    float4 clipPos = TransformWorldToHClip(positionWS);

    // _ShadowBias.x sign depens on if platform has reversed z buffer
    clipPos.z += _ShadowBias.x;

#if defined(UNITY_REVERSED_Z)
    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#else
    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#endif
    return clipPos;
}

half4 ShadowPassFragment() : SV_TARGET
{
    return 0;
}

#endif
