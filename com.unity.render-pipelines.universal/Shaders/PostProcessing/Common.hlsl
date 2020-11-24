#ifndef UNIVERSAL_POSTPROCESSING_COMMON_INCLUDED
#define UNIVERSAL_POSTPROCESSING_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"

// ----------------------------------------------------------------------------------
// Render fullscreen mesh by using a matrix set directly by the pipeline instead of
// relying on the matrix set by the C++ engine to avoid issues with XR

float4x4 _FullscreenProjMat;

float4 TransformFullscreenMesh(half3 positionOS)
{
    return mul(_FullscreenProjMat, half4(positionOS, 1));
}

Varyings VertFullscreenMesh(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if _USE_DRAW_PROCEDURAL
    GetProceduralQuad(input.vertexID, output.positionCS, output.uv);
#else
    output.positionCS = TransformFullscreenMesh(input.positionOS.xyz);
    output.uv = input.uv;
#endif

    return output;
}

// ----------------------------------------------------------------------------------
// Samplers

SAMPLER(sampler_LinearClamp);
SAMPLER(sampler_LinearRepeat);
SAMPLER(sampler_PointClamp);
SAMPLER(sampler_PointRepeat);

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

#if defined(UNITY_SINGLE_PASS_STEREO)
    dist.x /= unity_StereoScaleOffset[unity_StereoEyeIndex].x;
#endif

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

half3 ApplyGrain(half3 input, float2 uv, TEXTURE2D_PARAM(GrainTexture, GrainSampler), float intensity, float response, float2 scale, float2 offset)
{
    // Grain in range [0;1] with neutral at 0.5
    half grain = SAMPLE_TEXTURE2D(GrainTexture, GrainSampler, uv * scale + offset).w;

    // Remap [-1;1]
    grain = (grain - 0.5) * 2.0;

    // Noisiness response curve based on scene luminance
    float lum = 1.0 - sqrt(Luminance(input));
    lum = lerp(1.0, lum, response);

    return input + input * grain * intensity * lum;
}

half3 ApplyDithering(half3 input, float2 uv, TEXTURE2D_PARAM(BlueNoiseTexture, BlueNoiseSampler), float2 scale, float2 offset)
{
    // Symmetric triangular distribution on [-1,1] with maximal density at 0
    float noise = SAMPLE_TEXTURE2D(BlueNoiseTexture, BlueNoiseSampler, uv * scale + offset).a * 2.0 - 1.0;
    noise = FastSign(noise) * (1.0 - sqrt(1.0 - abs(noise)));

#if UNITY_COLORSPACE_GAMMA
    input += noise / 255.0;
#else
    input = GetSRGBToLinear(GetLinearToSRGB(input) + noise / 255.0);
#endif

    return input;
}

#endif // UNIVERSAL_POSTPROCESSING_COMMON_INCLUDED
