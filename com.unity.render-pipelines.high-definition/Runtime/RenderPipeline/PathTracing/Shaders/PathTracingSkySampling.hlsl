#ifndef UNITY_PATH_TRACING_SKY_SAMPLING_INCLUDED
#define UNITY_PATH_TRACING_SKY_SAMPLING_INCLUDED

#ifdef COMPUTE_PATH_TRACING_SKY_SAMPLING_DATA
#define PTSKY_TEXTURE2D(name) RW_TEXTURE2D(float, name)
#else
//#define PTSKY_TEXTURE2D(name) TEXTURE2D(name)
#define PTSKY_TEXTURE2D(name) Texture2D<float> name // FIXME
#endif

PTSKY_TEXTURE2D(_PathTracingSkyPDFTexture);
PTSKY_TEXTURE2D(_PathTracingSkyCDFTexture);
PTSKY_TEXTURE2D(_PathTracingSkyMarginalTexture);

uint _PathTracingSkyTextureWidth;
uint _PathTracingSkyTextureHeight;

int  _RaytracingCameraSkyEnabled;

bool IsSkyEnabled()
{
    return _EnvLightSkyEnabled && _RaytracingCameraSkyEnabled;;
}

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

float GetSkyCDF(PTSKY_TEXTURE2D(cdf), uint size, uint i, uint j)
{
    // FIXME ************************************************
    return i == 0 ? 0.0 : (i < size ? cdf[uint2(i, j)] : 1.0);
}

// Dichotomic search
float SampleSkyCDF(PTSKY_TEXTURE2D(cdf), uint size, uint j, float smp)
{
    uint i = 0, half = size / 2;
    for (uint offset = half; offset > 0; offset /= 2)
    {
        if (smp < GetSkyCDF(cdf, size, half, j))
        {
            // i is already in the right half
            half -= offset / 2;
        }
        else
        {
            // i has to move to the other half
            i += offset;
            half += offset / 2;
        }
    }

    float cdfInf = GetSkyCDF(cdf, size, i, j);
    float cdfSup = GetSkyCDF(cdf, size, i + 1, j);

    return (i + (smp - cdfInf) / (cdfSup - cdfInf)) / size;
}

// // Linear search
// float SampleSkyCDF(PTSKY_TEXTURE2D(cdf), uint size, uint j, float smp)
// {
//     float cdfSup, cdfInf = 0.0;
//     uint i;
//     for (i = 0; i < size; i++)
//     {
//         cdfSup = GetSkyCDF(cdf, size, i + 1, j);
//         if (smp < cdfSup)
//             break;
//         cdfInf = cdfSup;
//     }
//     return (i + (smp - cdfInf) / (cdfSup - cdfInf)) / size;
// }

float GetSkyPDFNormalizationFactor()
{
    return _PathTracingSkyMarginalTexture[uint2(0, 0)];
}

float3 GetSkyValue(float3 dir)
{
    return SampleSkyTexture(dir, 0.0, 0).rgb;
}

// This PDF approximation is valid only if PDF/CDF tables are computed with equiareal mapping
float GetSkyPDFFromValue(float3 value)
{
    return Luminance(value) * GetSkyPDFNormalizationFactor();
}

// float GetSkyPDF(float u, float v)
// {
//     // Unfiltered
//     // return _PathTracingSkyPDFTexture[uint2(u * _PathTracingSkyTextureWidth, v * _PathTracingSkyTextureHeight)];

//     // Filtered
//     return SAMPLE_TEXTURE2D_LOD(_PathTracingSkyPDFTexture, s_linear_clamp_sampler, float2(u, v), 0);
// }

// float GetSkyPDF(float2 uv)
// {
//     return GetSkyPDF(uv.x, uv.y);
// }

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
