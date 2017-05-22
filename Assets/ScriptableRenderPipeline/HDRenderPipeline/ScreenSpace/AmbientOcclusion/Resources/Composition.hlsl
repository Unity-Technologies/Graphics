#ifndef UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMPOSITION
#define UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMPOSITION

#include "Common.hlsl"

half _Downsample;

TEXTURE2D(_MainTex);
SAMPLER2D(sampler_MainTex);
float4 _MainTex_TexelSize;

struct CompositionOutput
{
    half4 gbuffer0 : SV_Target0;
    half4 gbuffer3 : SV_Target1;
};

CompositionOutput Frag(Varyings input)
{
    float2 delta = _MainTex_TexelSize.xy / _Downsample;

    float2 uv = input.texcoord;

    // 5-tap box blur filter.
    half4 p0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half4 p1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-delta.x, -delta.y));
    half4 p2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(+delta.x, -delta.y));
    half4 p3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(-delta.x, +delta.y));
    half4 p4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(+delta.x, +delta.y));

    half3 n0 = GetPackedNormal(p0);

    // Geometry-aware weighting.
    half w0 = 1.0;
    half w1 = CompareNormal(n0, GetPackedNormal(p1));
    half w2 = CompareNormal(n0, GetPackedNormal(p2));
    half w3 = CompareNormal(n0, GetPackedNormal(p3));
    half w4 = CompareNormal(n0, GetPackedNormal(p4));

    half ao;
    ao  = GetPackedAO(p0) * w0;
    ao += GetPackedAO(p1) * w1;
    ao += GetPackedAO(p2) * w2;
    ao += GetPackedAO(p3) * w3;
    ao += GetPackedAO(p4) * w4;
    ao /= w0 + w1 + w2 + w3 + w4;

    CompositionOutput output;
    output.gbuffer0 = half4(0.0, 0.0, 0.0, ao);
    output.gbuffer3 = half4((half3)ao, 0.0);
    return output;
}

#endif // UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMPOSITION
