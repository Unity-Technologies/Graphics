#ifndef CUSTOM_PASS_SAMPLING_HLSL
#define CUSTOM_PASS_SAMPLING_HLSL

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassInjectionPoint.cs.hlsl"

float _CustomPassInjectionPoint;

// This texture is only available in after post process and contains the result of post processing effects.
// While SampleCameraColor still returns the color pyramid without post processes
TEXTURE2D_X(_AfterPostProcessColorBuffer);

float3 CustomPassSampleCameraColor(float2 uv, float lod, bool uvGuards = true)
{
    if (uvGuards)
        uv = clamp(uv, 0, 1 - _ScreenSize.zw / 2.0);

    switch ((int)_CustomPassInjectionPoint)
    {
        case CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING: return float3(0, 0, 0);
        // there is no color pyramid yet for before transparent so we can't sample with mips.
        // Also, we don't use _RTHandleScaleHistory to sample because the color pyramid bound is the actual camera color buffer which is at the resolution of the camera
        case CUSTOMPASSINJECTIONPOINT_BEFORE_TRANSPARENT: return SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, uv * _RTHandleScaleHistory.xy, lod).rgb;
        case CUSTOMPASSINJECTIONPOINT_BEFORE_POST_PROCESS: return SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, uv * _RTHandleScale.xy, lod).rgb;
        case CUSTOMPASSINJECTIONPOINT_BEFORE_PRE_REFRACTION: return SAMPLE_TEXTURE2D_X_LOD(_ColorPyramidTexture, s_trilinear_clamp_sampler, uv * _RTHandleScale.xy, 0).rgb;
        case CUSTOMPASSINJECTIONPOINT_AFTER_POST_PROCESS: return SAMPLE_TEXTURE2D_X_LOD(_AfterPostProcessColorBuffer, s_trilinear_clamp_sampler, ClampAndScaleUVForBilinearPostProcessTexture(uv), 0).rgb;
        default: return SampleCameraColor(uv, lod);
    }
}

float3 CustomPassLoadCameraColor(uint2 pixelCoords, float lod)
{
    switch ((int)_CustomPassInjectionPoint)
    {
        case CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING: return float3(0, 0, 0);
        // there is no color pyramid yet for before transparent so we can't sample with mips.
        case CUSTOMPASSINJECTIONPOINT_BEFORE_TRANSPARENT:
        case CUSTOMPASSINJECTIONPOINT_BEFORE_PRE_REFRACTION: return LOAD_TEXTURE2D_X_LOD(_ColorPyramidTexture, pixelCoords, 0).rgb;
        case CUSTOMPASSINJECTIONPOINT_AFTER_POST_PROCESS: return LOAD_TEXTURE2D_X_LOD(_AfterPostProcessColorBuffer, pixelCoords, 0).rgb;
        default: return LoadCameraColor(pixelCoords, lod);
    }
}

float4 CustomPassSampleCustomColor(float2 uv)
{
    switch ((int)_CustomPassInjectionPoint)
    {
        case CUSTOMPASSINJECTIONPOINT_AFTER_POST_PROCESS: return LOAD_TEXTURE2D_X_LOD(_CustomColorTexture, uv * _ScreenSize.xy, 0);
        default: return SampleCustomColor(uv);
    }
}

float4 CustomPassLoadCustomColor(uint2 pixelCoords)
{
    return LoadCustomColor(pixelCoords);
}

float CustomPassLoadCustomDepth(uint2 pixelCoords)
{
    return LoadCustomDepth(pixelCoords);
}

float CustomPassLoadCameraDepth(uint2 pixelCoords)
{
    switch ((int)_CustomPassInjectionPoint)
    {
        case CUSTOMPASSINJECTIONPOINT_BEFORE_RENDERING: return 0;
        case CUSTOMPASSINJECTIONPOINT_AFTER_POST_PROCESS: return LoadCameraDepth(pixelCoords * _DynamicResolutionFullscreenScale.xy);
        default: return LoadCameraDepth(pixelCoords);
    }
}

#endif
