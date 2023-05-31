#ifndef UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
float4 _CameraDepthTexture_TexelSize;

float SampleSceneDepth(float2 uv)
{
    uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraDepthTexture_TexelSize.xy);
    return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
}

float LoadSceneDepth(uint2 uv)
{
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, uv).r;
}
#endif
