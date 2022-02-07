#ifndef UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#define UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraRenderingLayersTexture);
SAMPLER(sampler_CameraRenderingLayersTexture);

SamplerState my_point_clamp_sampler;

float SampleSceneRenderingLayer(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_CameraRenderingLayersTexture, sampler_CameraRenderingLayersTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
}

uint LoadSceneRenderingLayer(uint2 uv)
{
    // TODO: Investigate faster solution instead of packing
    //uv.y = _ScreenSize.y - uv.y;
    float encodedValue = LOAD_TEXTURE2D_X(_CameraRenderingLayersTexture, uv).r;
    //return uint(encodedValue * 65025.5);
    return UnpackInt(encodedValue, 16);// TODO: Expose as property the bits value
}
#endif
