#ifndef UNIVERSAL_LIGHT_COOKIE_INCLUDED
#define UNIVERSAL_LIGHT_COOKIE_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

// Defines
#if defined(_MAIN_LIGHT_COOKIE) || defined(_ADDITIONAL_LIGHT_COOKIES)
    #ifndef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
        #define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
    #endif
#endif

// Textures
TEXTURE2D(_MainLightCookieTexture);
TEXTURE2D(_AdditionalLightCookieAtlasTexture);

// Samplers
SAMPLER(sampler_MainLightCookieTexture);
SAMPLER(sampler_AdditionalLightCookieAtlasTexture);

// Params
//
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef SHADER_API_GLES3
CBUFFER_START(MainLightCookie)
#endif
    float4x4 _MainLightWorldToLight;  // TODO: Simple solution. Should property of the light! Orthographic! 3x4 would be enough or even just light.up (cross(spotDir, light.up), light.up, spotDir, lightPos)
    float2   _MainLightCookieUVScale;
    float    _MainLightCookieFormat;  // 0 RGBA, 1 Alpha only
//  float   _MainLightCookie_Unused;
#ifndef SHADER_API_GLES3
CBUFFER_END
#endif

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    StructuredBuffer<float4x4> _AdditionalLightsWorldToLightBuffer; // TODO: should really be property of the light! Move to Input.hlsl
    StructuredBuffer<half4>   _AdditionalLightCookieAtlasUVRectBuffer; // UV rect into light cookie atlas (xy: uv offset, zw: uv size)
#else
    #ifndef SHADER_API_GLES3
        CBUFFER_START(AdditionalLightCookies)
    #endif
            float4x4 _AdditionalLightWorldToLight[MAX_VISIBLE_LIGHTS_UBO];  // TODO: Should really be a property of the light!
            half4 _AdditionalLightCookieAtlasUVRect[MAX_VISIBLE_LIGHTS_UBO]; // (xy: uv offset, zw: uv size)
    #ifndef SHADER_API_GLES3
        CBUFFER_END
    #endif
#endif

// Param defines
#define LIGHT_COOKIE_FORMAT_RGBA  0.0f
#define LIGHT_COOKIE_FORMAT_ALPHA 1.0f

#define MAIN_LIGHT_COOKIE_FORMAT_IS_ALPHA (_MainLightCookieFormat == LIGHT_COOKIE_FORMAT_ALPHA)

// Match UnityEngine.TextureWrapMode
#define LIGHT_COOKIE_WRAP_MODE_NONE  -1.0f /* Use sampler settings */
#define LIGHT_COOKIE_WRAP_MODE_REPEAT 0.0f
#define LIGHT_COOKIE_WRAP_MODE_CLAMP  1.0f

// Function defines
#define SAMPLE_MAINLIGHT_COOKIE_TEXTURE(uv)   SAMPLE_TEXTURE2D(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv)

// Constants
//static const float kConst = 1.0;

// Functions

// TODO: to namespace, or not to namespace (probably should names buffers too)
half2 LightCookie_ComputeUVDirectional(float4x4 worldToLight, float3 samplePositionWS, float2 uvScale, float2 uvWrap, half4 atlasUVRect)
{
    // Translate and rotate 'positionWS' into the light space.
    float3 positionLS   = mul(worldToLight, float4(samplePositionWS, 1)).xyz; // TODO: is multiply in correct order?

    // Project
    float2 positionCS  = positionLS.xy;

    // Remap ortho CS to the texture coordinates, from [-1, 1]^2 to [0, 1]^2.
    float2 positionUV = positionCS * 0.5 + 0.5;

    // Scale UVs
    positionUV *= uvScale;

    // Tile texture for cookie in repeat mode
    positionUV.x = (uvWrap.x == LIGHT_COOKIE_WRAP_MODE_REPEAT) ? frac(positionUV.x) : positionUV.x;
    positionUV.y = (uvWrap.y == LIGHT_COOKIE_WRAP_MODE_REPEAT) ? frac(positionUV.y) : positionUV.y;
    positionUV.x = (uvWrap.x == LIGHT_COOKIE_WRAP_MODE_CLAMP)  ? saturate(positionUV.x) : positionUV.x;
    positionUV.y = (uvWrap.y == LIGHT_COOKIE_WRAP_MODE_CLAMP)  ? saturate(positionUV.y) : positionUV.y;

    // Remap to atlas texture
    half2 positionAtlasUV = atlasUVRect.xy + half2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}


half3 LightCookie_SampleMainLightCookie(float3 samplePositionWS)
{
    half2 uv = LightCookie_ComputeUVDirectional(_MainLightWorldToLight, samplePositionWS, _MainLightCookieUVScale, LIGHT_COOKIE_WRAP_MODE_NONE, half4(0, 0, 1, 1));
    return MAIN_LIGHT_COOKIE_FORMAT_IS_ALPHA ? SAMPLE_MAINLIGHT_COOKIE_TEXTURE(uv).aaa : SAMPLE_MAINLIGHT_COOKIE_TEXTURE(uv).rgb;
}

#endif //UNIVERSAL_LIGHT_COOKIE_INCLUDED
