#ifndef UNITY_PATH_TRACING_SKY_SAMPLING_INCLUDED
#define UNITY_PATH_TRACING_SKY_SAMPLING_INCLUDED

#ifdef COMPUTE_PATH_TRACING_SKY_SAMPLING_DATA
#define PTSKY_TEXTURE2D(name) RW_TEXTURE2D(float, name)
#else
#define PTSKY_TEXTURE2D(name) TEXTURE2D(name)
#endif

TEXTURE2D_X(_SkyCameraTexture);
PTSKY_TEXTURE2D(_PathTracingSkyCDFTexture);
PTSKY_TEXTURE2D(_PathTracingSkyMarginalTexture);

uint _PathTracingSkyTextureWidth;
uint _PathTracingSkyTextureHeight;

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

// // Standard latlon mapping (for reference)
// float2 MapSkyDirectionToUV(float3 dir)
// {
//     float theta = acos(-dir.y);
//     float phi = atan2(-dir.z, -dir.x);

//     return float2(0.5 - phi * INV_TWO_PI, theta * INV_PI);
// }

// // Standard latlon mapping (for reference)
// float3 MapUVToSkyDirection(float u, float v)
// {
//     float phi = TWO_PI * (1.0 - u);
//     float cosTheta = cos((1.0 - v) * PI);

//     return TransformGLtoDX(SphericalToCartesian(phi, cosTheta));
// }

float3 MapUVToSkyDirection(float2 uv)
{
    return MapUVToSkyDirection(uv.x, uv.y);
}

#ifndef COMPUTE_PATH_TRACING_SKY_SAMPLING_DATA

bool IsSkyEnabled()
{
    return _EnvLightSkyEnabled;
}

bool IsSkySamplingEnabled()
{
    return _PathTracingSkyTextureWidth;
}

float GetSkyCDF(PTSKY_TEXTURE2D(cdf), uint i, uint j)
{
    return cdf[uint2(i, j)].x;
}

// Dichotomic search
float SampleSkyCDF(PTSKY_TEXTURE2D(cdf), uint size, uint j, float smp)
{
    uint i = 0;
    for (uint halfsize = size >> 1, offset = halfsize; offset > 0; offset >>= 1)
    {
        if (smp < GetSkyCDF(cdf, halfsize, j))
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
    float cdfInf = i > 0 ? GetSkyCDF(cdf, i, j) : 0.0;
    float cdfSup = i < size ? GetSkyCDF(cdf, i + 1, j) : 1.0;

    return (i + (smp - cdfInf) / (cdfSup - cdfInf)) / size;
}

float GetSkyPDFNormalizationFactor()
{
    return _PathTracingSkyMarginalTexture[uint2(0, 0)].x;
}

// This PDF approximation is valid only if PDF/CDF tables are computed with equiareal mapping
float GetSkyPDFFromValue(float3 value)
{
    return Luminance(value) * GetSkyPDFNormalizationFactor();
}

float4 GetSkyBackground(uint2 pixelCoord)
{
    return _SkyCameraTexture[COORD_TEXTURE2D_X(pixelCoord)];
}

float3 GetSkyValue(float3 dir)
{
    return SampleSkyTexture(dir, 0.0, 0).rgb;
}

float2 SampleSky(float smpU, float smpV)
{
    float v = SampleSkyCDF(_PathTracingSkyMarginalTexture, _PathTracingSkyTextureHeight, 0, smpV);
    float u = SampleSkyCDF(_PathTracingSkyCDFTexture, _PathTracingSkyTextureWidth, v * _PathTracingSkyTextureHeight, smpU);

    return float2(u, v);
}

float2 SampleSky(float2 smp)
{
    return SampleSky(smp.x, smp.y);
}

#endif // COMPUTE_PATH_TRACING_SKY_SAMPLING_DATA

#endif // UNITY_PATH_TRACING_SKY_SAMPLING_INCLUDED
