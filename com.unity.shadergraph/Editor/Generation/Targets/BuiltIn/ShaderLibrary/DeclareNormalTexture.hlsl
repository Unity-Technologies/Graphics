#ifndef UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#define UNITY_DECLARE_DEPTH_TEXTURE_INCLUDED
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"

TEXTURE2D_X_FLOAT(_CameraDepthNormalsTexture);
SAMPLER(sampler_CameraDepthNormalsTexture);

float3 DecodeViewNormalStereo( float4 enc4 )
{
    float kScale = 1.7777;
    float3 nn = enc4.xyz*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
    float g = 2.0 / dot(nn.xyz,nn.xyz);
    float3 n;
    n.xy = g*nn.xy;
    n.z = g-1;
    return n;
}

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
