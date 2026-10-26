#ifndef UNITY_EXPOSURE_FUNCTIONS_INCLUDED
#define UNITY_EXPOSURE_FUNCTIONS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

float GetPreExposureMultiplier(Texture2D<float2> exposureMultiplierTexture)
{
#ifdef _EXPOSURE
    return LOAD_TEXTURE2D(exposureMultiplierTexture, int2(0, 0)).r;
#else
    return 1.0;
#endif
}

float GetInvPreExposureMultiplier(Texture2D<float2> exposureMultiplierTexture)
{
#ifdef _EXPOSURE
    return LOAD_TEXTURE2D(exposureMultiplierTexture, int2(0, 0)).g;
#else
    return 1.0;
#endif
}

// Precision used to accumulate light that will be pre-exposed. Promoted to float only when the
// pre-exposure feature (_EXPOSURE) is enabled, so a bright HDR sum survives until the exposure
// multiply brings it back into fp16 range. With the feature off it stays half, so content that
// doesn't use pre-exposure pays no extra cost. Note: intentionally NOT the platform 'real' type -
// this precision is chosen by the feature, not the platform.
#if defined(_EXPOSURE)
    #define URP_LIGHT_ACCUM3 float3
    #define URP_LIGHT_ACCUM4 float4
#else
    #define URP_LIGHT_ACCUM3 half3
    #define URP_LIGHT_ACCUM4 half4
#endif

// Selects between a reflection probe's capture-exposure multiplier and identity. Identity when _EXPOSURE is off,
// so stale or authored probe exposure values cannot affect rendering while the feature is disabled.
float SelectProbeExposureMultiplier(float probeExposureMultiplier)
{
#ifdef _EXPOSURE
    return probeExposureMultiplier;
#else
    return 1.0;
#endif
}

// Clamps pre-exposed lighting to fp16 range at the point where the float accumulator is
// narrowed for a half-precision render target. Identity when _EXPOSURE is off, so
// exposure-off output stays identical to URP's previous behavior.
URP_LIGHT_ACCUM3 ClampExposed(URP_LIGHT_ACCUM3 color)
{
#ifdef _EXPOSURE
    return min(color, HALF_MAX);
#else
    return color;
#endif
}

#endif
