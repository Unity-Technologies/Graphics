#ifndef UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
#define UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_CameraNormalsTexture);
SAMPLER(sampler_CameraNormalsTexture);

float3 SampleSceneNormals(float2 uv)
{
    return UnpackNormalOctRectEncode(SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, UnityStereoTransformScreenSpaceTex(uv)).xy) * float3(1.0, 1.0, -1.0);
}

float3 SampleSceneNormalsLod(float2 uv, int lod)
{
    return UnpackNormalOctRectEncode(SAMPLE_TEXTURE2D_LOD(_CameraNormalsTexture, sampler_CameraNormalsTexture, UnityStereoTransformScreenSpaceTex(uv), lod).xy) * float3(1.0, 1.0, -1.0);
}

float3 LoadSceneNormals(uint2 uv)
{
    return UnpackNormalOctRectEncode(LOAD_TEXTURE2D(_CameraNormalsTexture, uv).xy) * float3(1.0, 1.0, -1.0);
}
#endif
