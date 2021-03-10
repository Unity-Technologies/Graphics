#ifndef UNIVERSAL_LIGHT_COOKIE_INCLUDED
#define UNIVERSAL_LIGHT_COOKIE_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"

// Defines
#if defined(_MAIN_LIGHT_COOKIE) || defined(_ADDITIONAL_LIGHT_COOKIES)
    #ifndef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
        #define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR 1
    #endif
#endif

// Textures
TEXTURE2D(_MainLightCookieTexture);
TEXTURE2D(_AdditionalLightsCookieAtlasTexture);

// Samplers
SAMPLER(sampler_MainLightCookieTexture);
SAMPLER(sampler_AdditionalLightsCookieAtlasTexture);

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

// TODO: pack data
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    StructuredBuffer<float4x4> _AdditionalLightsWorldToLightBuffer; // TODO: should really be property of the light! Move to Input.hlsl
    StructuredBuffer<half4>    _AdditionalLightsCookieAtlasUVRectBuffer; // UV rect into light cookie atlas (xy: uv offset, zw: uv size)
    StructuredBuffer<float>    _AdditionalLightsLightTypeBuffer; // TODO: should really be property of the light! Move to Input.hlsl
#else
    #ifndef SHADER_API_GLES3
        CBUFFER_START(AdditionalLightsCookies)
    #endif
            float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS_UBO];  // TODO: Should really be a property of the light !!!
            half4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS_UBO]; // (xy: uv offset, zw: uv size)
            float _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS_UBO]; // TODO: Should really be a property of the light !!!
            float _AdditionalLightsCookieAtlasFormat;
    #ifndef SHADER_API_GLES3
        CBUFFER_END
    #endif
#endif

// Param defines
#define LIGHT_COOKIE_FORMAT_RGBA  0.0f
#define LIGHT_COOKIE_FORMAT_ALPHA 1.0f

// TODO: make a function
#define MAIN_LIGHT_COOKIE_FORMAT_IS_ALPHA (_MainLightCookieFormat == LIGHT_COOKIE_FORMAT_ALPHA)
#define ADDITIONAL_LIGHTS_COOKIE_FORMAT_IS_ALPHA (_AdditionalLightsCookieAtlasFormat == LIGHT_COOKIE_FORMAT_ALPHA)

// Match UnityEngine.TextureWrapMode
#define LIGHT_COOKIE_WRAP_MODE_REPEAT 0.0f
#define LIGHT_COOKIE_WRAP_MODE_CLAMP  1.0f

// Special case to use sampler settings
#define LIGHT_COOKIE_WRAP_MODE_NONE  -1.0f

// Match UnityEngine.LightType
#define LIGHT_COOKIE_LIGHT_TYPE_SPOT        0.0f
#define LIGHT_COOKIE_LIGHT_TYPE_DIRECTIONAL 1.0f
#define LIGHT_COOKIE_LIGHT_TYPE_POINT       2.0f

// Function defines
// TODO: make a function
#define SAMPLE_MAIN_LIGHT_COOKIE_TEXTURE(uv)        SAMPLE_TEXTURE2D(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv)
#define SAMPLE_ADDITONAL_LIGHT_COOKIE_TEXTURE(uv)   SAMPLE_TEXTURE2D(_AdditionalLightsCookieAtlasTexture, sampler_AdditionalLightsCookieAtlasTexture, uv)

// Constants
//static const float kConst = 1.0;

// Functions

// TODO: to namespace, or not to namespace (probably should namespace buffers too)
// TODO: Could do Universal_Feature_Method or URP_Feature_Method for public API too
// TODO: URP_Feature_Internal_Method or URP_Feature_Private_Method for internal API (by convention)
half2 LightCookie_ComputeUVDirectional(float4x4 worldToLight, float3 samplePositionWS, float2 uvScale, float2 uvWrap, half4 atlasUVRect)
{
    // Translate and rotate 'positionWS' into the light space.
    float3 positionLS   = mul(worldToLight, float4(samplePositionWS, 1)).xyz;

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

half2 LightCookie_ComputeUVSpot(float4x4 worldToLightPerspective, float3 samplePositionWS, half4 atlasUVRect)
{
    // Translate, rotate and project 'positionWS' into the light clip space.
    float4 positionCS   = mul(worldToLightPerspective, float4(samplePositionWS, 1));
    float2 positionNDC  = positionCS.xy / positionCS.w;

    // Remap NDC to the texture coordinates, from NDC [-1, 1]^2 to [0, 1]^2.
    float2 positionUV = saturate(positionNDC * 0.5 + 0.5);

    // Remap into rect in the atlas texture
    half2 positionAtlasUV = atlasUVRect.xy + half2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}
/*
half2 LightCookie_ComputeUVPoint(float4x4 worldToLight, float3 samplePositionWS, half4 atlasUVRect)
{
    // Translate and rotate 'positionWS' into the light space.
    float3 positionLS   = mul(worldToLight, float4(samplePositionWS, 1)).xyz; // TODO: is multiply in correct order?


    float3 directionLS  = normalize(positionLS);

    // TODO:
    // SampleCube or OctRead
    float2 positionCS = PackNormalOctQuadEncode(directionLS);

    // Remap ortho CS to the texture coordinates, from [-1, 1]^2 to [0, 1]^2.
    float2 positionUV = saturate(positionCS * 0.5 + 0.5);

    // Remap to atlas texture
    half2 positionAtlasUV = atlasUVRect.xy + half2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}
*/

half3 LightCookie_SampleMainLightCookie(float3 samplePositionWS)
{
    half2 uv = LightCookie_ComputeUVDirectional(_MainLightWorldToLight, samplePositionWS, _MainLightCookieUVScale, LIGHT_COOKIE_WRAP_MODE_NONE, half4(0, 0, 1, 1));
    return MAIN_LIGHT_COOKIE_FORMAT_IS_ALPHA ? SAMPLE_MAIN_LIGHT_COOKIE_TEXTURE(uv).aaa : SAMPLE_MAIN_LIGHT_COOKIE_TEXTURE(uv).rgb;
}

half3 LightCookie_SampleAdditionalLightCookie(int perObjectLightIndex, float3 samplePositionWS)
{
    float4 uvRect = _AdditionalLightsCookieAtlasUVRects[perObjectLightIndex];

    // TODO: cookie enable bit, uint[8] for 256 lights
    // TODO: read less
    if(all(uvRect == 0))
        return half3(1,1,1);

#if 1
    float    lightType    = _AdditionalLightsLightTypes[perObjectLightIndex];
    float4x4 worldToLight = _AdditionalLightsWorldToLights[perObjectLightIndex];

    int isSpot = lightType == LIGHT_COOKIE_LIGHT_TYPE_SPOT;

    half2 uv;
    //if(isSpot)
        uv = LightCookie_ComputeUVSpot(worldToLight, samplePositionWS, uvRect);
    //else
        //uv = LightCookie_ComputeUVPoint(_MainLightWorldToLight, samplePositionWS, uvRect);

    // Translate and rotate 'positionWS' into the light space.
    //float3 positionLS = mul(worldToLight, float4(samplePositionWS, 1)).xyz; // TODO: is multiply in correct order?
    //return frac(positionLS);
    //return half3(uv, 0);
    return ADDITIONAL_LIGHTS_COOKIE_FORMAT_IS_ALPHA ? SAMPLE_ADDITONAL_LIGHT_COOKIE_TEXTURE(uv).aaa : SAMPLE_ADDITONAL_LIGHT_COOKIE_TEXTURE(uv).rgb;
#else
    return half3(1,1,1);
#endif
}

#endif //UNIVERSAL_LIGHT_COOKIE_INCLUDED
