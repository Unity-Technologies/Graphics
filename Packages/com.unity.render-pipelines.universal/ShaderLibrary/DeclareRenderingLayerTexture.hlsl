#ifndef UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#define UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraRenderingLayersTexture);

// 2023.3 Deprecated. This is for backwards compatibility. Remove in the future.
#define sampler_CameraRenderingLayersTexture sampler_PointClamp

uint SampleSceneRenderingLayer(float2 uv)
{
    float renderingLayer = SAMPLE_TEXTURE2D_X(_CameraRenderingLayersTexture, sampler_PointClamp, UnityStereoTransformScreenSpaceTex(uv)).r;
    return DecodeMeshRenderingLayer(renderingLayer);
}

uint LoadSceneRenderingLayer(uint2 uv)
{
    float renderingLayer = LOAD_TEXTURE2D_X(_CameraRenderingLayersTexture, uv).r;
    return DecodeMeshRenderingLayer(renderingLayer);
}
#endif
