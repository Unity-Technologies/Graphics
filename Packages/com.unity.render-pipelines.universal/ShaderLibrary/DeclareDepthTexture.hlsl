#ifndef UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthTexture);
float4 _CameraDepthTexture_TexelSize;

// 2023.3 Deprecated. This is for backwards compatibility. Remove in the future.
#define sampler_CameraDepthTexture sampler_PointClamp

// Framebuffer fetch API for depth input attachment
#if defined(_DEPTH_AS_INPUT_ATTACHMENT)
    FRAMEBUFFER_INPUT_X_FLOAT(0);

    float FetchSceneDepth(float2 fragCoord)
    {
        float depth = LOAD_FRAMEBUFFER_INPUT_X(0, fragCoord).r;
        return depth; 
    }
#elif defined(_DEPTH_AS_INPUT_ATTACHMENT_MSAA)
    FRAMEBUFFER_INPUT_X_FLOAT_MS(0);
    
    float FetchSceneDepth(float2 fragCoord, int sampleIndx) 
    {
        float depth = LOAD_FRAMEBUFFER_INPUT_X_MS(0, sampleIndx, fragCoord).r;
        return depth;
    }
#endif

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
