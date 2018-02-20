#ifndef LIGHTWEIGHT_PASS_SHADOW_INCLUDED
#define LIGHTWEIGHT_PASS_SHADOW_INCLUDED

#include "LWRP/ShaderLibrary/LightweightPassLit.hlsl"

// x: global clip space bias, y: normal world space bias
float4 _ShadowBias;
float3 _LightDirection;

LightweightVertexOutput ShadowPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = LitPassVertex(v);

    float invNdotL = 1.0 - saturate(dot(_LightDirection, o.normal));
    float scale = invNdotL * _ShadowBias.y;

    // normal bias is negative since we want to apply an inset normal offset
    o.posWS = o.normal * scale.xxx + o.posWS;
    float4 clipPos = TransformWorldToHClip(o.posWS);

    // _ShadowBias.x sign depens on if platform has reversed z buffer
    clipPos.z += _ShadowBias.x;

#if UNITY_REVERSED_Z
    clipPos.z = min(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
#else
    clipPos.z = max(clipPos.z, clipPos.w * UNITY_NEAR_CLIP_VALUE);
#endif

    o.clipPos = clipPos;
    return o;
}
#endif
