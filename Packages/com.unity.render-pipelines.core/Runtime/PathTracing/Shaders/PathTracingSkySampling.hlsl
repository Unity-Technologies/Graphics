#ifndef _PATHTRACING_PATHTRACINGSKYSAMPLING_HLSL_
#define _PATHTRACING_PATHTRACINGSKYSAMPLING_HLSL_

#ifdef COMPUTE_PATH_TRACING_SKY_SAMPLING_DATA
RWStructuredBuffer<float> _PathTracingSkyConditionalBuffer;
RWStructuredBuffer<float> _PathTracingSkyMarginalBuffer;
#else
StructuredBuffer<float> _PathTracingSkyConditionalBuffer;
StructuredBuffer<float> _PathTracingSkyMarginalBuffer;
#endif

uint _PathTracingSkyConditionalResolution;
uint _PathTracingSkyMarginalResolution;

// Equiareal mapping
float2 MapSkyDirectionToUV(float3 dir)
{
    float cosTheta = dir.y;
    float phi = atan2(-dir.z, -dir.x);

    return float2(0.5 - phi * INV_TWO_PI, (cosTheta + 1.0) * 0.5);
}

// Equiareal mapping
float3 MapUVToSkyDirection(float u, float v)
{
    float phi = TWO_PI * (1.0 - u);
    float cosTheta = 2.0 * v - 1.0;

    return TransformGLtoDX(SphericalToCartesian(phi, cosTheta));
}

float3 MapUVToSkyDirection(float2 uv)
{
    return MapUVToSkyDirection(uv.x, uv.y);
}

#ifndef COMPUTE_PATH_TRACING_SKY_SAMPLING_DATA


bool IsSkySamplingEnabled()
{
    return _PathTracingSkyConditionalResolution;
}

float GetSkyCDF(StructuredBuffer<float> cdf, uint i, uint offset)
{
    return cdf[offset + i];
}

// Dichotomic search
float SampleSkyCDF(StructuredBuffer<float> cdf, uint size, uint bufferOffset, float smp)
{
    uint i = 0;
    for (uint halfsize = size >> 1, offset = halfsize; offset > 0; offset >>= 1)
    {
        if (smp < GetSkyCDF(cdf, halfsize, bufferOffset))
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
    float cdfInf = i > 0 ? GetSkyCDF(cdf, i, bufferOffset) : 0.0;
    float cdfSup = i < size ? GetSkyCDF(cdf, i + 1, bufferOffset) : 1.0;

    return (i + (smp - cdfInf) / (cdfSup - cdfInf)) / size;
}

float GetSkyPDFNormalizationFactor()
{
    return _PathTracingSkyMarginalBuffer[0];
}

// This PDF approximation is valid only if PDF/CDF tables are computed with equiareal mapping
float GetSkyPDFFromValue(float3 value, float pdfNormalization)
{
    return Luminance(value) * pdfNormalization;
}

float GetSkyPDFFromValue(float3 value)
{
    return GetSkyPDFFromValue(value, GetSkyPDFNormalizationFactor());
}

float2 SampleSky(float smpU, float smpV)
{
    float v = SampleSkyCDF(_PathTracingSkyMarginalBuffer, _PathTracingSkyMarginalResolution, 0, smpV);
    float u = SampleSkyCDF(_PathTracingSkyConditionalBuffer, _PathTracingSkyConditionalResolution, _PathTracingSkyConditionalResolution * uint(v * _PathTracingSkyMarginalResolution), smpU);
    return float2(u, v);
}

float2 SampleSky(float2 smp)
{
    return SampleSky(smp.x, smp.y);
}

#endif // COMPUTE_PATH_TRACING_SKY_SAMPLING_DATA

#endif // UNITY_PATH_TRACING_SKY_SAMPLING_INCLUDED
