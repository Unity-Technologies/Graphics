#ifndef UNITY_DECLARE_NORMAL_TEXTURE_INCLUDED
#define UNITY_DECLARE_NORMAL_TEXTURE_INCLUDED
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthNormalsTexture);
SAMPLER(sampler_CameraDepthNormalsTexture);

float3 SampleSceneNormal(float2 uv)
{
    float4 enc = SAMPLE_TEXTURE2D_X(_CameraDepthNormalsTexture, sampler_CameraDepthNormalsTexture, UnityStereoTransformScreenSpaceTex(uv)).r;

    return DecodeViewNormalStereo(enc);
}

float3 LoadSceneNormal(uint2 pixelCoords)
{
    float4 enc = LOAD_TEXTURE2D_X(_CameraDepthNormalsTexture, pixelCoords).r;

    return DecodeViewNormalStereo(enc);
}
#endif
