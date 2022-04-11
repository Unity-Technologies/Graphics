#ifndef UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
#define UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraNormalsTexture);
SAMPLER(sampler_CameraNormalsTexture);

float3 SampleSceneNormals(float2 uv)
{
    float3 normal = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, sampler_CameraNormalsTexture, UnityStereoTransformScreenSpaceTex(uv)).xyz;

    #if defined(_GBUFFER_NORMALS_OCT)
    half2 remappedOctNormalWS = Unpack888ToFloat2(normal); // values between [ 0,  1]
    half2 octNormalWS = remappedOctNormalWS.xy * 2.0h - 1.0h;    // values between [-1, +1]
    normal = UnpackNormalOctQuadEncode(octNormalWS);
    #endif

    return normal;
}

float3 LoadSceneNormals(uint2 uv)
{
    float3 normal = LOAD_TEXTURE2D_X(_CameraNormalsTexture, uv).xyz;

    #if defined(_GBUFFER_NORMALS_OCT)
    half2 remappedOctNormalWS = Unpack888ToFloat2(normal); // values between [ 0,  1]
    half2 octNormalWS = remappedOctNormalWS.xy * 2.0h - 1.0h;    // values between [-1, +1]
    normal = UnpackNormalOctQuadEncode(octNormalWS);
    #endif

    return normal;
}
#endif
