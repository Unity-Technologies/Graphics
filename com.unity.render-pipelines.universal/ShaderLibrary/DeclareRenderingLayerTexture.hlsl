#ifndef UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#define UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraRenderingLayersTexture);
SAMPLER(sampler_CameraRenderingLayersTexture);

float SampleSceneRenderingLayer(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_CameraRenderingLayersTexture, sampler_CameraRenderingLayersTexture, UnityStereoTransformScreenSpaceTex(uv)).r;
}

uint LoadSceneRenderingLayer(uint2 uv)
{
    return DecodeMeshRenderingLayer(LOAD_TEXTURE2D_X(_CameraRenderingLayersTexture, uv).r);
}
#endif
