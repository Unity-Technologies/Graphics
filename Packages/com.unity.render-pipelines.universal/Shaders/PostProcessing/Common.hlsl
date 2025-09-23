#ifndef UNIVERSAL_POSTPROCESSING_COMMON_INCLUDED
#define UNIVERSAL_POSTPROCESSING_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

#if _FXAA
// Notes on FXAA:
// * We now rely on the official FXAA implementation (authored by Timothy Lottes while at NVIDIA)
//   with minimal changes made by Unity to integrate with URP.
// * The following 'Tweakable' defines are used by the FXAA implementation and can be changed if desired:
//   * FXAA_PC set to 1 is the highest quality implementation ("PC" here is a misnomer, it will run on all platforms).
//   * FXAA_PC set to 0 is the cheaper 'FXAA_PC_CONSOLE' variant
//     (it's equivalent to URP's old implementation but less noisy and should run faster than before)
//   * FXAA_GREEN_AS_LUMA can be set to 0 for an extra performance increase but will only antialias edges that have
//     some green in them (will be visually equivalent on the vast majority of scenes).
//   * FXAA_QUALITY__PRESET is used when FXAA_PC is set ot 1. We chose preset 12 as it runs almost as fast on Switch as
//     our old noisy implementation did.
//     On all other platforms we could basically get away with preset 15 which has slightly better edge quality.

// Tweakable params (can be changed to get different performance and quality tradeoffs)
#if (SHADER_API_PS5 || SHADER_API_SWITCH2) && defined(HDR_INPUT)
// The console implementation does not generate artefacts when the input pixels are in nits (monitor HDR range).
#define FXAA_PC 0
#else
#define FXAA_PC 1
#endif
#define FXAA_GREEN_AS_LUMA 0
#define FXAA_QUALITY__PRESET 12

// Fixed params (should not be changed)
#define FXAA_HLSL_5 1
#define FXAA_GATHER4_ALPHA 0
#define FXAA_PC_CONSOLE !FXAA_PC

#include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/FXAA3_11.hlsl"
#endif

// ----------------------------------------------------------------------------------
// Utility functions

half GetLuminance(half3 colorLinear)
{
#if _TONEMAP_ACES
    return AcesLuminance(colorLinear);
#else
    return Luminance(colorLinear);
#endif
}

real3 GetSRGBToLinear(real3 c)
{
#if _USE_FAST_SRGB_LINEAR_CONVERSION
    return FastSRGBToLinear(c);
#else
    return SRGBToLinear(c);
#endif
}

real4 GetSRGBToLinear(real4 c)
{
#if _USE_FAST_SRGB_LINEAR_CONVERSION
    return FastSRGBToLinear(c);
#else
    return SRGBToLinear(c);
#endif
}

real3 GetLinearToSRGB(real3 c)
{
#if _USE_FAST_SRGB_LINEAR_CONVERSION
    return FastLinearToSRGB(c);
#else
    return LinearToSRGB(c);
#endif
}

real4 GetLinearToSRGB(real4 c)
{
#if _USE_FAST_SRGB_LINEAR_CONVERSION
    return FastLinearToSRGB(c);
#else
    return LinearToSRGB(c);
#endif
}

// ----------------------------------------------------------------------------------
// Shared functions for uber & fast path (on-tile)
// These should only process an input color, don't sample in neighbor pixels!

half3 ApplyVignette(half3 input, float2 uv, float2 center, float intensity, float roundness, float smoothness, half3 color)
{
    center = UnityStereoTransformScreenSpaceTex(center);
    float2 dist = abs(uv - center) * intensity;

    dist.x *= roundness;
    float vfactor = pow(saturate(1.0 - dot(dist, dist)), smoothness);
    return input * lerp(color, (1.0).xxx, vfactor);
}

half3 ApplyTonemap(half3 input)
{
#if _TONEMAP_ACES
    float3 aces = unity_to_ACES(input);
    input = AcesTonemap(aces);
#elif _TONEMAP_NEUTRAL
    input = NeutralTonemap(input);
#endif

    return saturate(input);
}

half3 ApplyColorGrading(half3 input, float postExposure, TEXTURE2D_PARAM(lutTex, lutSampler), float3 lutParams, TEXTURE2D_PARAM(userLutTex, userLutSampler), float3 userLutParams, float userLutContrib)
{
    // Artist request to fine tune exposure in post without affecting bloom, dof etc
    input *= postExposure;

    // HDR Grading:
    //   - Apply internal LogC LUT
    //   - (optional) Clamp result & apply user LUT
    #if _HDR_GRADING
    {
        float3 inputLutSpace = saturate(LinearToLogC(input)); // LUT space is in LogC
        input = ApplyLut2D(TEXTURE2D_ARGS(lutTex, lutSampler), inputLutSpace, lutParams);

        UNITY_BRANCH
        if (userLutContrib > 0.0)
        {
            input = saturate(input);
            input.rgb = GetLinearToSRGB(input.rgb); // In LDR do the lookup in sRGB for the user LUT
            half3 outLut = ApplyLut2D(TEXTURE2D_ARGS(userLutTex, userLutSampler), input, userLutParams);
            input = lerp(input, outLut, userLutContrib);
            input.rgb = GetSRGBToLinear(input.rgb);
        }
    }

    // LDR Grading:
    //   - Apply tonemapping (result is clamped)
    //   - (optional) Apply user LUT
    //   - Apply internal linear LUT
    #else
    {
        input = ApplyTonemap(input);

        UNITY_BRANCH
        if (userLutContrib > 0.0)
        {
            input.rgb = GetLinearToSRGB(input.rgb); // In LDR do the lookup in sRGB for the user LUT
            half3 outLut = ApplyLut2D(TEXTURE2D_ARGS(userLutTex, userLutSampler), input, userLutParams);
            input = lerp(input, outLut, userLutContrib);
            input.rgb = GetSRGBToLinear(input.rgb);
        }

        input = ApplyLut2D(TEXTURE2D_ARGS(lutTex, lutSampler), input, lutParams);
    }
    #endif

    return input;
}

half3 ApplyGrain(half3 input, float2 uv, TEXTURE2D_PARAM(GrainTexture, GrainSampler), float intensity, float response, float2 scale, float2 offset, float oneOverPaperWhite)
{
    // Grain in range [0;1] with neutral at 0.5
    half grain = SAMPLE_TEXTURE2D(GrainTexture, GrainSampler, uv * scale + offset).w;

    // Remap [-1;1]
    grain = (grain - 0.5) * 2.0;

    // Noisiness response curve based on scene luminance
    float lum = Luminance(input);
    #ifdef HDR_INPUT
    lum *= oneOverPaperWhite;
    #endif
    lum = 1.0 - sqrt(lum);
    lum = lerp(1.0, lum, response);

    return input + input * grain * intensity * lum;
}

half3 ApplyDithering(half3 input, float2 uv, TEXTURE2D_PARAM(BlueNoiseTexture, BlueNoiseSampler), float2 scale, float2 offset, float paperWhite, float oneOverPaperWhite)
{
    // Symmetric triangular distribution on [-1,1] with maximal density at 0
    float noise = SAMPLE_TEXTURE2D(BlueNoiseTexture, BlueNoiseSampler, uv * scale + offset).a * 2.0 - 1.0;
    noise = FastSign(noise) * (1.0 - sqrt(1.0 - abs(noise)));

#if UNITY_COLORSPACE_GAMMA
    input += noise / 255.0;
#elif defined(HDR_INPUT)
    input = input * oneOverPaperWhite;
    // Do not call GetSRGBToLinear/GetLinearToSRGB because the "fast" version will clamp values!
    input = SRGBToLinear(LinearToSRGB(input) + noise / 255.0);
    input = input * paperWhite;
#else
    input = GetSRGBToLinear(GetLinearToSRGB(input) + noise /255.0);
#endif

    return input;
}

#if _FXAA
static const FxaaFloat kSubpixelBlendAmount = 0.65;
static const FxaaFloat kRelativeContrastThreshold = 0.15;
static const FxaaFloat kAbsoluteContrastThreshold = 0.03;
#endif

half3 ApplyFXAA(half3 color, float2 positionNDC, int2 positionSS, float4 sourceSize, TEXTURE2D_X(inputTexture), float paperWhite, float oneOverPaperWhite)
{
#if _FXAA
    FxaaTex tex = {sampler_LinearClamp, _BlitTexture};
    FxaaFloat4 kUnusedFloat4 = FxaaFloat4(0, 0, 0, 0);

    FxaaFloat4 fxaaConsolePos = 0;
    FxaaFloat4 kFxaaConsoleRcpFrameOpt = 0;
    FxaaFloat4 kFxaaConsoleRcpFrameOpt2 = 0;
    FxaaFloat kFxaaConsoleEdgeSharpness = 0;
    FxaaFloat kFxaaConsoleEdgeThreshold = 0;
    FxaaFloat kFxaaConsoleEdgeThresholdMin = 0;
    FxaaFloat2 fxaaHDROutputPaperWhiteNits = 0;

#if FXAA_PC_CONSOLE == 1
    fxaaConsolePos = FxaaFloat4(positionNDC.xy - 0.5*sourceSize.zw, positionNDC.xy + 0.5*sourceSize.zw);
    kFxaaConsoleRcpFrameOpt = 0.5*FxaaFloat4(sourceSize.zw, -sourceSize.zw);
    kFxaaConsoleRcpFrameOpt2 = 2.0*FxaaFloat4(-sourceSize.zw, sourceSize.zw);
    kFxaaConsoleEdgeSharpness = 8.0;
    kFxaaConsoleEdgeThreshold = 0.125;
    kFxaaConsoleEdgeThresholdMin = 0.05;
#endif
    fxaaHDROutputPaperWhiteNits = FxaaFloat2(paperWhite, oneOverPaperWhite);

    return FxaaPixelShader(
        positionNDC,
        FxaaFloat4(color, 0),
        fxaaConsolePos,
        tex,
        tex,
        tex,
        sourceSize.zw,
        kFxaaConsoleRcpFrameOpt,
        kFxaaConsoleRcpFrameOpt2,
        kUnusedFloat4,
        kSubpixelBlendAmount,
        kRelativeContrastThreshold,
        kAbsoluteContrastThreshold,
        kFxaaConsoleEdgeSharpness,
        kFxaaConsoleEdgeThreshold,
        kFxaaConsoleEdgeThresholdMin,
        kUnusedFloat4,
        fxaaHDROutputPaperWhiteNits
    ).rgb;
#else
    return color;
#endif
}

#endif // UNIVERSAL_POSTPROCESSING_COMMON_INCLUDED
