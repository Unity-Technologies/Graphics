#ifndef UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED
#define UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookieTypes.hlsl"

// Textures
TEXTURE2D(_MainLightCookieTexture);
TEXTURE2D(_AdditionalLightsCookieAtlasTexture);

// Samplers
SAMPLER(sampler_MainLightCookieTexture);

// Persistent constant buffer everywhere except GLES3, which falls back to field-wise SetGlobalXArray. WebGL2
// reports as GLES3. Must match RenderingUtils.usePersistentConstantBuffer: if the two disagree, the field-wise
// uploads never run and the shader reads whatever is in the bound buffer.
#if !defined(SHADER_API_GLES3)
    #define URP_LIGHT_COOKIE_PERSISTENT_CBUFFER
#endif

#if defined(URP_LIGHT_COOKIE_PERSISTENT_CBUFFER)
CBUFFER_START(LightCookies)
    float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS];
    float4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS];
    float4 _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS]; // yzw are padding
    float4 _AdditionalLightsCookieEnableBits[(MAX_VISIBLE_LIGHTS + 31) / 32]; // yzw are padding
CBUFFER_END
#else
    // Field-wise layout, GLES3 only: a transient CBUFFER on WebGL2, plain loose uniforms elsewhere, since
    // CBUFFER causes a performance regression on some GLES3 devices.
    #ifndef LIGHT_SHADOWS_NO_CBUFFER
    CBUFFER_START(LightCookies)
    #endif
        float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS];
        float4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS];
        float _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS];
        float _AdditionalLightsCookieEnableBits[(MAX_VISIBLE_LIGHTS + 31) / 32];
    #ifndef LIGHT_SHADOWS_NO_CBUFFER
    CBUFFER_END
    #endif
#endif


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
#if defined(URP_LIGHT_COOKIE_PERSISTENT_CBUFFER)
    return _AdditionalLightsLightTypes[lightIndex].x;
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
    // Always sample LOD 0 for light cookies
    return SAMPLE_TEXTURE2D_LOD(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv, 0);
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

#if defined(URP_LIGHT_COOKIE_PERSISTENT_CBUFFER)
    uint elem = asuint(_AdditionalLightsCookieEnableBits[elemIndex].x);
#else
    uint elem = asuint(_AdditionalLightsCookieEnableBits[elemIndex]);
#endif
    
    return (elem & (1u << bitOffset)) != 0u;
#endif
}

#endif //UNIVERSAL_LIGHT_COOKIE_INPUT_INCLUDED
