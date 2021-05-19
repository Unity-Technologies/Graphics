#ifndef UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED
#define UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookieTypes.hlsl"

// Textures
TEXTURE2D(_MainLightCookieTexture);
TEXTURE2D(_AdditionalLightsCookieAtlasTexture);

// Samplers
SAMPLER(sampler_MainLightCookieTexture);
SAMPLER(sampler_AdditionalLightsCookieAtlasTexture);

// Buffers
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef SHADER_API_GLES3
CBUFFER_START(LightCookies)
#endif
    float4x4 _MainLightWorldToLight;  // TODO: Should property of the light! Orthographic. 3x4 would be enough.
    float    _MainLightCookieTextureFormat;
    float    _AdditionalLightsCookieAtlasTextureFormat;
//  float2   _Unused;
#ifndef SHADER_API_GLES3
CBUFFER_END
#endif

// TODO: pack data
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    StructuredBuffer<float4x4> _AdditionalLightsWorldToLightBuffer; // TODO: should really be property of the light!
    StructuredBuffer<float4>   _AdditionalLightsCookieAtlasUVRectBuffer; // UV rect into light cookie atlas (xy: uv offset, zw: uv size)
    StructuredBuffer<float>    _AdditionalLightsLightTypeBuffer; // TODO: should really be property of the light!
#else
    #ifndef SHADER_API_GLES3
        CBUFFER_START(AdditionalLightsCookies)
    #endif
            float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS];  // TODO: Should really be a property of the light !!!
            float4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS]; // (xy: uv size, zw: uv offset)
            float _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS]; // TODO: Should really be a property of the light !!!
    #ifndef SHADER_API_GLES3
        CBUFFER_END
    #endif
#endif

// Data Getters

float4x4 GetLightCookieWorldToLightMatrix(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsWorldToLightBuffer[lightIndex];
    #else
        return _AdditionalLightsWorldToLights[lightIndex];
    #endif
}

float4 GetLightCookieAtlasUVRect(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsCookieAtlasUVRectBuffer[lightIndex];
    #else
        return _AdditionalLightsCookieAtlasUVRects[lightIndex];
    #endif
}

int GetLightCookieLightType(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsLightTypeBuffer[lightIndex];
    #else
        return _AdditionalLightsLightTypes[lightIndex];
    #endif
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

real4 SampleAdditionalLightsCookieTexture(float2 uv)
{
    return SAMPLE_TEXTURE2D(_AdditionalLightsCookieAtlasTexture, sampler_AdditionalLightsCookieAtlasTexture, uv);
}

// Helpers

bool IsLightCookieEnabled(int lightBufferIndex)
{
    float4 uvRect = GetLightCookieAtlasUVRect(lightBufferIndex);
    return all(uvRect == 0);
}

#endif //UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED
