#ifndef UNITY_DECLARE_OPAQUE_TEXTURE_INCLUDED
#define UNITY_DECLARE_OPAQUE_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);

float3 SampleSceneColor(float2 uv)
{
    return SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, UnityStereoTransformScreenSpaceTex(uv)).rgb;
}

float3 LoadSceneColor(uint2 uv)
{
    return LOAD_TEXTURE2D_X(_CameraOpaqueTexture, uv).rgb;
}
#endif
