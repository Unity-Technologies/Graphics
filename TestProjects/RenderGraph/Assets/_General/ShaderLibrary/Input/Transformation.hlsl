#ifndef SCRATCH_INPUT_TRANSFORMATION_HLSL
#define SCRATCH_INPUT_TRANSFORMATION_HLSL

#include "InputMacro.hlsl"
#include "UnityBuiltIn.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

// Below functions are missing in core RP...

float3 TransformObjectToViewPos(float3 positionOS)
{
    return mul(GetWorldToViewMatrix(), mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0))).xyz;
}

float4 ComputeScreenPos(float4 positionCS)
{
    float4 o = positionCS * 0.5f;
    o.xy = float2(o.x, o.y * _ProjectionParams.x) + o.w;
    o.zw = positionCS.zw;
    return o;
}

float4 ComputeGrabScreenPos (float4 pos) 
{
    #if UNITY_UV_STARTS_AT_TOP
        float scale = -1.0;
    #else
        float scale = 1.0;
    #endif

    float4 o = pos * 0.5f;
    o.xy = float2(o.x, o.y*scale) + o.w;

    #ifdef UNITY_SINGLE_PASS_STEREO
        o.xy = TransformStereoScreenSpaceTex(o.xy, pos.w);
    #endif
    
    o.zw = pos.zw;
    return o;
}

#endif // SCRATCH_INPUT_TRANSFORMATION_HLSL