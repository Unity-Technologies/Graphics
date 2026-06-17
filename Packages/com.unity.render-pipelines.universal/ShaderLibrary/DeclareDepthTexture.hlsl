#ifndef UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthTexture);
float4 _CameraDepthTexture_TexelSize;

// 2023.3 Deprecated. This is for backwards compatibility. Remove in the future.
#define sampler_CameraDepthTexture sampler_PointClamp

float SampleSceneDepth(float2 uv, SAMPLER(samplerParam))
{
    uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraDepthTexture_TexelSize.xy);
    return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, samplerParam, uv).r;
}

float SampleSceneDepth(float2 uv)
{
    uv = UnityStereoTransformScreenSpaceTex(uv);
    #if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
    uv = RemovePretransformRotation(uv, GetScaledScreenParams());
    #endif
    uv = ClampAndScaleUVForBilinear(uv, _CameraDepthTexture_TexelSize.xy);
    uint2 pixelCoord = uint2(uv * _ScreenSize.xy);
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, pixelCoord).r;
}

float LoadSceneDepth(uint2 pixelCoords)
{
    return LOAD_TEXTURE2D_X(_CameraDepthTexture, pixelCoords).r;
}
#endif
