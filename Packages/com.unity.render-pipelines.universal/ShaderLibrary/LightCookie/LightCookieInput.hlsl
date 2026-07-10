#ifndef UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED
#define UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookieTypes.hlsl"

// Textures
TEXTURE2D(_MainLightCookieTexture);
TEXTURE2D(_AdditionalLightsCookieAtlasTexture);

// Samplers
SAMPLER(sampler_MainLightCookieTexture);

// Buffers
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef LIGHT_SHADOWS_NO_CBUFFER
CBUFFER_START(LightCookies)
#endif
    float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS];
    float4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS]; // (xy: uv size, zw: uv offset)
#ifndef LIGHT_SHADOWS_NO_CBUFFER
CBUFFER_END
#endif

float4x4 _MainLightWorldToLight;
float _MainLightCookieTextureFormat;
float _AdditionalLightsCookieAtlasTextureFormat;

// Cannot be included in the CBUFFER, float[] arrays padding differs between std140 (CBUFFER)
// and Metal MSL when packed alongside float4/float4x4 arrays.
float _AdditionalLightsCookieEnableBits[(MAX_VISIBLE_LIGHTS + 31) / 32];
float _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS];

// Data Getters

float4x4 GetLightCookieWorldToLightMatrix(int lightIndex)
{
    return _AdditionalLightsWorldToLights[lightIndex];
}

float4 GetLightCookieAtlasUVRect(int lightIndex)
{
    return _AdditionalLightsCookieAtlasUVRects[lightIndex];
}

int GetLightCookieLightType(int lightIndex)
{
    return _AdditionalLightsLightTypes[lightIndex];
}

bool IsMainLightCookieTextureRGBFormat()
{
    return _MainLightCookieTextureFormat == URP_LIGHT_COOKIE_FORMAT_RGB;
}

bool IsMainLightCookieTextureAlphaFormat()
{
    return _MainLightCookieTextureFormat == URP_LIGHT_COOKIE_FORMAT_ALPHA;
}

bool IsAdditionalLightsCookieAtlasTextureRGBFormat()
{
    return _AdditionalLightsCookieAtlasTextureFormat == URP_LIGHT_COOKIE_FORMAT_RGB;
}

bool IsAdditionalLightsCookieAtlasTextureAlphaFormat()
{
    return _AdditionalLightsCookieAtlasTextureFormat == URP_LIGHT_COOKIE_FORMAT_ALPHA;
}

// Sampling

real4 SampleMainLightCookieTexture(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv);
}

real4 SampleAdditionalLightsCookieAtlasTexture(float2 uv)
{
    // No mipmap support
    return SAMPLE_TEXTURE2D_LOD(_AdditionalLightsCookieAtlasTexture, sampler_LinearClamp, uv, 0);
}

// Helpers
bool IsMainLightCookieEnabled()
{
    return _MainLightCookieTextureFormat != URP_LIGHT_COOKIE_FORMAT_NONE;
}

bool IsLightCookieEnabled(int lightBufferIndex)
{
#if 0
    float4 uvRect = GetLightCookieAtlasUVRect(lightBufferIndex);
    return any(uvRect != 0);
#else
    // 2^5 == 32, bit mask for a float/uint.
    uint elemIndex = ((uint)lightBufferIndex) >> 5;
    uint bitOffset = (uint)lightBufferIndex & ((1 << 5) - 1);

    uint elem = asuint(_AdditionalLightsCookieEnableBits[elemIndex]);

    return (elem & (1u << bitOffset)) != 0u;
#endif
}

#endif //UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED
