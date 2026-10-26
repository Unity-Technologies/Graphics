#ifndef UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
#define UNITY_DECLARE_NORMALS_TEXTURE_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/PackNormalsTexture.hlsl"

TEXTURE2D_X_FLOAT(_CameraNormalsTexture);
float4 _CameraNormalsTexture_TexelSize;

// 2023.3 Deprecated. This is for backwards compatibility. Remove in the future.
#define sampler_CameraNormalsTexture sampler_PointClamp

half3 LoadSceneNormalsRaw(uint2 pixelCoords)
{
    return LOAD_TEXTURE2D_X(_CameraNormalsTexture, pixelCoords).xyz;
}

half3 SampleSceneNormalsRaw(float2 uv, SAMPLER(samplerParam))
{
    uv = UnityStereoTransformScreenSpaceTex(uv);
    #if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
    uv = RemovePretransformRotation(uv, GetScaledScreenParams());
    #endif
    uv = ClampAndScaleUVForBilinear(uv, _CameraNormalsTexture_TexelSize.xy);
    // The camera normals texture has no mipmaps, so read an explicit LOD 0 to avoid an unnecessary gradient instruction.
    return SAMPLE_TEXTURE2D_X_LOD(_CameraNormalsTexture, samplerParam, uv, 0).xyz;
}

half3 SampleSceneNormalsRaw(float2 uv)
{
    return SampleSceneNormalsRaw(uv, sampler_PointClamp);
}

half3 SampleSceneNormals(float2 uv, SAMPLER(samplerParam))
{
    return UnpackNormalWSFromTexture(SampleSceneNormalsRaw(uv, samplerParam));
}

half3 SampleSceneNormals(float2 uv)
{
    return SampleSceneNormals(uv, sampler_PointClamp);
}

half3 LoadSceneNormals(uint2 pixelCoords)
{
    return UnpackNormalWSFromTexture(LoadSceneNormalsRaw(pixelCoords));
}
#endif
