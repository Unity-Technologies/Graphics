#ifndef UNIVERSAL_META_PASS_INCLUDED
#define UNIVERSAL_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/MetaPass.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

struct MetaInput
{
    half3 Albedo;
    half3 Emission;
    half3 SpecularColor;
};

float4 MetaVertexPosition(float4 positionOS, float2 uv1, float2 uv2, float4 uv1ST, float4 uv2ST)
{
    return UnityMetaVertexPosition(positionOS.xyz, uv1, uv2, uv1ST, uv2ST);
}

half4 MetaFragment(MetaInput input)
{
    UnityMetaInput umi = (UnityMetaInput)0;
    umi.Albedo = input.Albedo;
    umi.Emission = input.Emission;
    umi.SpecularColor = umi.SpecularColor;
    return UnityMetaFragment(umi);
}
#endif
