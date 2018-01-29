#ifndef LIGHTWEIGHT_PASS_SHADOW_INCLUDED
#define LIGHTWEIGHT_PASS_SHADOW_INCLUDED

#include "LWRP/ShaderLibrary/Core.hlsl"

// x: global clip space bias, y: normal world space bias
float4 _ShadowBias;
float3 _LightDirection;

struct VertexInput
{
    float4 position : POSITION;
    float3 normal   : NORMAL;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float4 ShadowPassVertex(VertexInput v) : SV_POSITION
{
    UNITY_SETUP_INSTANCE_ID(v);

    float3 positionWS = TransformObjectToWorld(v.position.xyz);
    float3 normalWS = TransformObjectToWorldDir(v.normal);

    float invNdotL = 1.0 - saturate(dot(_LightDirection, normalWS));
    float scale = invNdotL * _ShadowBias.y;

    // normal bias is negative since we want to apply an inset normal offset
    positionWS = normalWS * scale.xxx + positionWS;
    float4 clipPos = TransformWorldToHClip(positionWS);

    // _ShadowBias.x sign depens on if platform has reversed z buffer
    clipPos.z += _ShadowBias.x;

#if defined(UNITY_REVERSED_Z)
    clipPos.z = min(clipPos.z, 1.0);
#else
    clipPos.z = max(clipPos.z, 0.0);
#endif
    return clipPos;
}

half4 ShadowPassFragment() : SV_TARGET
{
    return 0;
}

#endif
