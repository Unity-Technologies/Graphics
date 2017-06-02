#ifndef UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_DENOISING
#define UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_DENOISING

#include "CommonAmbientOcclusion.hlsl"

half _Downsample;

TEXTURE2D(_MainTex);
SAMPLER2D(sampler_MainTex);
float4 _MainTex_TexelSize;

half4 Frag(Varyings input) : SV_Target
{
    // input.positionCS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenSize.zw);
    float2 uv = posInput.positionSS;

#if defined(AO_DENOISE_HORIZONTAL)

    // Horizontal pass: Always use 2 texels interval to match to
    // the dither pattern.
    float2 delta = float2(_MainTex_TexelSize.x * 2.0, 0.0);

#else // AO_DENOISE_VERTICAL

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

#if defined(AO_DENOISE_CENTER_NORMAL)

    half3 unused;
    BSDFData bsdfData;
    FETCH_GBUFFER(gbuffer, _GBufferTexture, posInput.unPositionSS);
    DECODE_FROM_GBUFFER(gbuffer, 0xFFFFFFFF, bsdfData, unused);

    half3 n0 = SampleNormal(bsdfData);
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

#endif // UNITY_HDRENDERPIPELINE_AMBIENTOCCLUSION_DENOISING
