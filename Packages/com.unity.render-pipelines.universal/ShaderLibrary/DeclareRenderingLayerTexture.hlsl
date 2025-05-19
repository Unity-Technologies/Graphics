#ifndef UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#define UNITY_DECLARE_RENDERING_LAYER_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TYPED_TEXTURE2D_X(uint4, _CameraRenderingLayersTexture);

uint LoadSceneRenderingLayer(uint2 uvCoord)
{
    return LOAD_TEXTURE2D_X(_CameraRenderingLayersTexture, uvCoord).r;
}
#endif
