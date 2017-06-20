#ifndef UNITY_HDRENDERPIPELINE_SSAO_NOISEFILTER
#define UNITY_HDRENDERPIPELINE_SSAO_NOISEFILTER

half _Downsample;

TEXTURE2D(_MainTex);
SAMPLER2D(sampler_MainTex);
float4 _MainTex_TexelSize;

half4 FragSeparableFilter(Varyings input) : SV_Target
{
    // input.positionCS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);
    float2 uv = posInput.positionSS;

#if defined(SSAO_NOISEFILTER_HORIZONTAL)

    // Horizontal pass: Always use 2 texels interval to match to
    // the dither pattern.
    float2 delta = float2(_MainTex_TexelSize.x * 2.0, 0.0);

#else // SSAO_NOISEFILTER_VERTICAL

    // Vertical pass: Apply _Downsample to match to the dither
    // pattern in the original occlusion buffer.
    float2 delta = float2(0.0, _MainTex_TexelSize.y / _Downsample * 2.0);

#endif

    // 5-tap Gaussian with linear sampling.
    half4 p0  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half4 p1a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - delta * 1.3846153846);
    half4 p1b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + delta * 1.3846153846);
    half4 p2a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - delta * 3.2307692308);
    half4 p2b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + delta * 3.2307692308);

#if defined(SSAO_NOISEFILTER_CENTERNORMAL)
    half3 n0 = SampleNormal(posInput.unPositionSS);
#else
    half3 n0 = GetPackedNormal(p0);
#endif

    // Geometry-aware weighting.
    half w0  = 0.2270270270;
    half w1a = CompareNormal(n0, GetPackedNormal(p1a)) * 0.3162162162;
    half w1b = CompareNormal(n0, GetPackedNormal(p1b)) * 0.3162162162;
    half w2a = CompareNormal(n0, GetPackedNormal(p2a)) * 0.0702702703;
    half w2b = CompareNormal(n0, GetPackedNormal(p2b)) * 0.0702702703;

    half s;
    s  = GetPackedAO(p0)  * w0;
    s += GetPackedAO(p1a) * w1a;
    s += GetPackedAO(p1b) * w1b;
    s += GetPackedAO(p2a) * w2a;
    s += GetPackedAO(p2b) * w2b;
    s /= w0 + w1a + w1b + w2a + w2b;

    return PackAONormal(s, n0);
}

half4 FragFinalFilter(Varyings input) : SV_Target0
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

#endif // UNITY_HDRENDERPIPELINE_SSAO_NOISEFILTER
