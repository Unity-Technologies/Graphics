#ifndef UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMPOSITION
#define UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMPOSITION

#include "CommonAmbientOcclusion.hlsl"

half _Downsample;

TEXTURE2D(_MainTex);
SAMPLER2D(sampler_MainTex);
float4 _MainTex_TexelSize;

half4 Frag(Varyings input) : SV_Target0
{
    // input.positionCS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);
    float2 uv = posInput.positionSS;

    float2 delta = _MainTex_TexelSize.xy / _Downsample; // TODO: is it correct, we have already bilateral upsample here ?

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

    // Note: When we ImageLoad outside of texture size, the value returned by Load is 0.
    // We use this property to have a neutral value for AO that doesn't consume a sampler and work also with compute shader (i.e use ImageLoad)
    // We store inverse AO so neutral is black. So either we sample inside or outside the texture it return 0 in case of neutral
    return half4(ao, 0, 0, 0); // <= we don't invert ao here but when we sample the texture for reasons explain above
}

#endif // UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_COMPOSITION
