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

float GetCDFValue(PTSKY_TEXTURE2D(cdf), uint size, uint i, uint j)
{
    return i < size ? cdf[uint2(i, j)] : 1.0;
}

float SampleCDF(PTSKY_TEXTURE2D(cdf), uint size, uint j, float smp)
{
    float valSup, valInf = 0.0;
    uint i;
    for (i = 0; i < size; i++)
    {
        valSup = GetCDFValue(cdf, size, i+1, j);
        if (smp < valSup)
            break;
        valInf = valSup;
    }

    return (i + (smp - valInf) / (valSup - valInf)) / size;
}

float GetPDFValue(float u, float v)
{
    return _PathTracingSkyPDFTexture[uint2(u * _PathTracingSkyTextureWidth, v * _PathTracingSkyTextureHeight)];
}

float GetPDFValue(float2 uv)
{
    return GetPDFValue(uv.x, uv.y);
}

float2 SampleSky(float smpU, float smpV)
{
    float v = SampleCDF(_PathTracingSkyMarginalTexture, _PathTracingSkyTextureHeight, 0, smpV);
    float u = SampleCDF(_PathTracingSkyCDFTexture, _PathTracingSkyTextureWidth, v * _PathTracingSkyTextureHeight, smpU);

    return float2(u, v);
}

float2 SampleSky(float2 smp)
{
    return SampleSky(smp.x, smp.y);
}

// float2 MapSkyDirectionToUV(float3 dir)
// {
//     float theta = acos(-dir.y);
//     float phi = atan2(-dir.z, -dir.x);

//     return float2(0.5 - phi * INV_TWO_PI, theta * INV_PI);
// }

// float3 MapUVToSkyDirection(float u, float v)
// {
//     float phi = TWO_PI * (1.0 - u);
//     float cosTheta = cos((1.0 - v) * PI);

//     return TransformGLtoDX(SphericalToCartesian(phi, cosTheta));
// }

// Equiareal variant
float2 MapSkyDirectionToUV(float3 dir)
{
    float cosTheta = dir.y;
    float phi = atan2(-dir.z, -dir.x);

    return float2(0.5 - phi * INV_TWO_PI, (cosTheta + 1.0) * 0.5);
}

// Equiareal variant
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


#endif // UNITY_PATH_TRACING_SKY_SAMPLING_INCLUDED
