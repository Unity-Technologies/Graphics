#ifndef UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#define UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraDecalLayersTexture);
SAMPLER(sampler_CameraDecalLayersTexture);

SamplerState my_point_clamp_sampler;

float SampleSceneRenderingLayer(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_CameraDecalLayersTexture, sampler_CameraDecalLayersTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
}

uint LoadSceneRenderingLayer(uint2 uv)
{
    //uv.y = _ScreenSize.y - uv.y;
    float encodedValue = LOAD_TEXTURE2D_X(_CameraDecalLayersTexture, uv).r;
    return UnpackInt(encodedValue, 16);// uint(encodedValue * 65025.5);
}

/*uint LoadSceneDecalLayer(uint2 uv)
{
    //uv.y = _ScreenSize.y - uv.y;
    float encodedValue = LOAD_TEXTURE2D_X(_CameraDecalLayersTexture, uv).r;
    return uint(encodedValue * 65025.5);
}*/
#endif
