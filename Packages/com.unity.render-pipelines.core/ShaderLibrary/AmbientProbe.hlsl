#ifndef __AMBIENTPROBE_HLSL__
#define __AMBIENTPROBE_HLSL__

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl"

// Ambient Probe is preconvolved with clamped cosinus
// In case we use a diffuse power, we have to edit the coefficients to change the convolution
// This is currently not used because visual difference is really minor
void ReconvolveAmbientProbeWithPower(float diffusePower, inout float4 SHCoefficients[7])
{
    if (diffusePower == 0.0f)
        return;

    // convolution coefs
    float w = diffusePower + 1;
    float kModifiedLambertian0 = 1.0f;
    float kModifiedLambertian1 = (w + 1.0f) / (w + 2.0f);
    float kModifiedLambertian2 = w / (w + 3.0f);

    // ambient probe is pre-convolved by clamped cosine - we have to undo pre-convolution and
    // convolve again with coefs for modified lambertian
    float wrapScaling0 = kModifiedLambertian0 / kClampedCosine0;
    float wrapScaling1 = kModifiedLambertian1 / kClampedCosine1;
    float wrapScaling2 = kModifiedLambertian2 / kClampedCosine2;

    // handle coeficient packing - see AmbientProbeConvolution.compute : PackSHFromScratchBuffer
    float3 ambient6 = float3(SHCoefficients[3].z, SHCoefficients[4].z, SHCoefficients[5].z) / 3.0f;
    float3 ambient0 = float3(SHCoefficients[0].a, SHCoefficients[1].a, SHCoefficients[2].a) + ambient6;

    SHCoefficients[0].xyz *= wrapScaling1;
    SHCoefficients[1].xyz *= wrapScaling1;
    SHCoefficients[2].xyz *= wrapScaling1;
    SHCoefficients[3]     *= wrapScaling2;
    SHCoefficients[4]     *= wrapScaling2;
    SHCoefficients[5]     *= wrapScaling2;
    SHCoefficients[6]     *= wrapScaling2;

    SHCoefficients[0].a = ambient0.r * wrapScaling0 - ambient6.r * wrapScaling2;
    SHCoefficients[1].a = ambient0.g * wrapScaling0 - ambient6.g * wrapScaling2;
    SHCoefficients[2].a = ambient0.b * wrapScaling0 - ambient6.b * wrapScaling2;
}

// We need to define this before including ProbeVolume.hlsl as that file expects this function to be defined.
// AmbientProbe Data is fetch directly from a compute buffer to remain on GPU and is preconvolved with clamped cosinus
real3 EvaluateAmbientProbe(real3 normalWS)
{
#if AMBIENT_PROBE_BUFFER
    return SampleSH9(_AmbientProbeData, normalWS);
#else
    // Linear + constant polynomial terms
    real3 res = SHEvalLinearL0L1(normalWS, unity_SHAr, unity_SHAg, unity_SHAb);

    // Quadratic polynomials
    res += SHEvalLinearL2(normalWS, unity_SHBr, unity_SHBg, unity_SHBb, unity_SHC);

    return res;
#endif
}

real3 EvaluateAmbientProbeSRGB(real3 normalWS)
{
    real3 res = EvaluateAmbientProbe(normalWS);
#ifdef UNITY_COLORSPACE_GAMMA
    res = LinearToSRGB(res);
#endif
    return res;
}

real3 SampleSH(real3 normalWS)
{
    return EvaluateAmbientProbeSRGB(normalWS);
}

real3 EvaluateAmbientProbeL1(real3 normalWS)
{
#if AMBIENT_PROBE_BUFFER
    real4 SHCoefficients[3];
    SHCoefficients[0] = _AmbientProbeData[0];
    SHCoefficients[1] = _AmbientProbeData[1];
    SHCoefficients[2] = _AmbientProbeData[2];
    return SampleSH4_L1(SHCoefficients, normalWS);
#else
    return real3(0.0, 0.0, 0.0);
#endif
}

real3 EvaluateAmbientProbeL0()
{
#if AMBIENT_PROBE_BUFFER
    return real3(_AmbientProbeData[0].w, _AmbientProbeData[1].w, _AmbientProbeData[2].w);
#else
    return real3(0.0, 0.0, 0.0);
#endif
}

#endif
