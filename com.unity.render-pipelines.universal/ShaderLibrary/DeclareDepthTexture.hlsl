#ifndef UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

#define USE_PASS_VP_MATRIX 1

float SampleSceneDepth(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
}

float LoadSceneDepth(uint2 uv)
{
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, uv).r;
}

float3 GetWorldPositionFromDepth(float2 uv)
{
#if USE_PASS_VP_MATRIX
    float4x4 invVP = UNITY_MATRIX_I_VP;
#else
    float4x4 invVP = mul(unity_CameraToWorld, unity_CameraInvProjection);
#endif
    
    float depth = SampleSceneDepth(uv);
    float4 positionWS = mul(invVP, float4(uv * 2 - 1, depth, 1));
    return positionWS.xyz / positionWS.w;
}
#endif
