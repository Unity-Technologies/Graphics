#ifndef UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED
#define UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookieTypes.hlsl"

// TODO : _URP_LightCookie_MainLightTexture???
// TODO : _URP_LightCookie_AdditionalLightsAtlasTexture???
// Textures
TEXTURE2D(_MainLightCookieTexture);
TEXTURE2D(_AdditionalLightsCookieAtlasTexture);

// Samplers
SAMPLER(sampler_MainLightCookieTexture);
SAMPLER(sampler_AdditionalLightsCookieAtlasTexture);

// Buffers
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef SHADER_API_GLES3
CBUFFER_START(MainLightCookie)
#endif
    float4x4 _MainLightWorldToLight;  // TODO: Simple solution. Should property of the light! Orthographic! 3x4 would be enough or even just light.up (cross(spotDir, light.up), light.up, spotDir, lightPos)
    float2   _MainLightCookieUVScale;
    float2   _MainLightCookieUVOffset;
    float    _MainLightCookieFormat;  // 0 RGBA, 1 Alpha only
//  float3   _MainLightCookie_Unused;
#ifndef SHADER_API_GLES3
CBUFFER_END
#endif

// TODO : _URP_LightCookie_AdditionalLightsWorldToLightBuffer???
// TODO : _URP_LightCookie_AdditionalLightsAtlasUVRectBuffer???
// TODO: pack data
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    StructuredBuffer<float4x4> _AdditionalLightsWorldToLightBuffer; // TODO: should really be property of the light! Move to Input.hlsl
    StructuredBuffer<float4>   _AdditionalLightsCookieAtlasUVRectBuffer; // UV rect into light cookie atlas (xy: uv offset, zw: uv size)
    StructuredBuffer<float>    _AdditionalLightsLightTypeBuffer; // TODO: should really be property of the light! Move to Input.hlsl
#else
    #ifndef SHADER_API_GLES3
        CBUFFER_START(AdditionalLightsCookies)
    #endif
            float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS];  // TODO: Should really be a property of the light !!!
            float4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS]; // (xy: uv size, zw: uv offset)
            float _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS]; // TODO: Should really be a property of the light !!!
            float _AdditionalLightsCookieAtlasFormat;
    #ifndef SHADER_API_GLES3
        CBUFFER_END
    #endif
#endif

// Data Getters

float4x4 URP_LightCookie_GetWorldToLightMatrix(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsWorldToLightBuffer[lightIndex];
    #else
        return _AdditionalLightsWorldToLights[lightIndex];
    #endif
}

float4 URP_LightCookie_GetAtlasUVRect(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsCookieAtlasUVRectBuffer[lightIndex];
    #else
        return _AdditionalLightsCookieAtlasUVRects[lightIndex];
    #endif
}

int URP_LightCookie_GetLightType(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsLightTypeBuffer[lightIndex];
    #else
        return _AdditionalLightsLightTypes[lightIndex];
    #endif
}

bool URP_LightCookie_MainLightTextureIsRGBFormat()
{
    return _MainLightCookieFormat == URP_LIGHT_COOKIE_FORMAT_RGB;
}

bool URP_LightCookie_MainLightTextureIsAlphaFormat()
{
    return _MainLightCookieFormat == URP_LIGHT_COOKIE_FORMAT_ALPHA;
}

bool URP_LightCookie_AdditionalLightsTextureIsRGBFormat()
{
    return _AdditionalLightsCookieAtlasFormat == URP_LIGHT_COOKIE_FORMAT_RGB;
}

bool URP_LightCookie_AdditionalLightsTextureIsAlphaFormat()
{
    return _AdditionalLightsCookieAtlasFormat == URP_LIGHT_COOKIE_FORMAT_ALPHA;
}

// Sampling

real4 URP_LightCookie_SampleMainLightTexture(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv);
}

real4 URP_LightCookie_SampleAdditionalLightsTexture(float2 uv)
{
    return SAMPLE_TEXTURE2D(_AdditionalLightsCookieAtlasTexture, sampler_AdditionalLightsCookieAtlasTexture, uv);
}

// Helpers

bool URP_LightCookie_IsEnabled(float4 uvRect)
{
    return all(uvRect == 0);
}

#endif //UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED
