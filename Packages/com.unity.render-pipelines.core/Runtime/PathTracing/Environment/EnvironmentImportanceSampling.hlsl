#ifndef _ENVIRONMENT_IMPORTANCE_SAMPLING_HLSL_
#define _ENVIRONMENT_IMPORTANCE_SAMPLING_HLSL_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

// Equiareal mapping
float2 MapSkyDirectionToUV(float3 dir)
{
    float cosTheta = dir.y;
    float phi = atan2(-dir.z, -dir.x);
    return float2(0.5 - phi * INV_TWO_PI, (cosTheta + 1.0) * 0.5);
}

// Equiareal mapping
float3 MapUVToSkyDirection(float2 uv)
{
    float phi = TWO_PI * (1.0 - uv.x);
    float cosTheta = 2.0 * uv.y - 1.0;
    return TransformGLtoDX(SphericalToCartesian(phi, cosTheta));
}

// Dichotomic search
float SampleSkyCDF(StructuredBuffer<float> cdf, uint size, uint bufferOffset, float smp)
{
    uint i = 0;
    for (uint halfsize = size >> 1, offset = halfsize; offset > 0; offset >>= 1)
    {
        if (smp < cdf[bufferOffset + halfsize])
        {
            // i is already in the right half (lower one)
            halfsize -= offset >> 1;
        }
        else
        {
            // i has to move to the other half (upper one)
            i += offset;
            halfsize += offset >> 1;
        }
    }

    // MarginalTexture[0] stores the PDF normalization factor, so we need a test on i == 0
    float cdfInf = i == 0 ? 0.0 : cdf[bufferOffset + i];
    float cdfSup = i + 1 == size ? 1.0f : cdf[bufferOffset + i + 1];

    return (i + (smp - cdfInf) / (cdfSup - cdfInf)) / size;
}

float GetSkyPDFNormalizationFactor(StructuredBuffer<float> marginalBuffer)
{
    return marginalBuffer[0];
}

// This PDF approximation is valid only if PDF/CDF tables are computed with equiareal mapping
float GetSkyPDFFromValue(float3 value, float pdfNormalization)
{
    return Luminance(value) * pdfNormalization;
}

float GetSkyPDFFromValue(float3 value, StructuredBuffer<float> marginalBuffer)
{
    return GetSkyPDFFromValue(value, GetSkyPDFNormalizationFactor(marginalBuffer));
}

float2 SampleSky(float2 rand, uint marginalResolution, StructuredBuffer<float> marginalBuffer, uint conditionalResolution, StructuredBuffer<float> conditionalBuffer)
{
    float v = SampleSkyCDF(marginalBuffer, marginalResolution, 0, rand.x);
    float u = SampleSkyCDF(conditionalBuffer, conditionalResolution, conditionalResolution * uint(v * marginalResolution), rand.y);
    return float2(u, v);
}

#endif
