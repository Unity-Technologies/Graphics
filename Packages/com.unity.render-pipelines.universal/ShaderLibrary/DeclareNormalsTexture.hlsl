#ifndef UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
#define UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"

TEXTURE2D_X_FLOAT(_CameraNormalsTexture);
float4 _CameraNormalsTexture_TexelSize;

// 2023.3 Deprecated. This is for backwards compatibility. Remove in the future.
#define sampler_CameraNormalsTexture sampler_PointClamp

float3 SampleSceneNormals(float2 uv, SAMPLER(samplerParam))
{
    uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraNormalsTexture_TexelSize.xy);
    float3 normal = SAMPLE_TEXTURE2D_X(_CameraNormalsTexture, samplerParam, uv).xyz;

    #if defined(_GBUFFER_NORMALS_OCT)
    float2 remappedOctNormalWS = Unpack888ToFloat2(normal); // values between [ 0,  1]
    float2 octNormalWS = remappedOctNormalWS.xy * 2.0 - 1.0;    // values between [-1, +1]
    normal = UnpackNormalOctQuadEncode(octNormalWS);
    #endif

    return normal;
}

float3 SampleSceneNormals(float2 uv)
{
    return SampleSceneNormals(uv, sampler_PointClamp);
}

float3 LoadSceneNormals(uint2 pixelCoords)
{
    float3 normal = LOAD_TEXTURE2D_X(_CameraNormalsTexture, pixelCoords).xyz;

    #if defined(_GBUFFER_NORMALS_OCT)
    float2 remappedOctNormalWS = Unpack888ToFloat2(normal); // values between [ 0,  1]
    float2 octNormalWS = remappedOctNormalWS.xy * 2.0 - 1.0;    // values between [-1, +1]
    normal = UnpackNormalOctQuadEncode(octNormalWS);
    #endif

    return normal;
}
#endif
