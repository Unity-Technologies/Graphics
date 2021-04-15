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

// TODO: Should move shader input parameters/data into a input header

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
    float2   _MainLightCookieUVOffset;
    float    _MainLightCookieFormat;  // 0 RGBA, 1 Alpha only
//  float3   _MainLightCookie_Unused;
#ifndef SHADER_API_GLES3
CBUFFER_END
#endif

// TODO: pack data
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
    StructuredBuffer<float4x4> _AdditionalLightsWorldToLightBuffer; // TODO: should really be property of the light! Move to Input.hlsl
    StructuredBuffer<float4>   _AdditionalLightsCookieAtlasUVRectBuffer; // UV rect into light cookie atlas (xy: uv offset, zw: uv size)
    StructuredBuffer<float4>   _AdditionalLightsCookieUVScaleOffsetBuffer; // UV scale and offset for repeated textures (xy: uv offset, zw: uv size)
    StructuredBuffer<float>    _AdditionalLightsCookieUVWrapModeBuffer; // UV wrap mode for repeated textures (xy: uv offset, zw: uv size)
    StructuredBuffer<float>    _AdditionalLightsLightTypeBuffer; // TODO: should really be property of the light! Move to Input.hlsl
#else
    #ifndef SHADER_API_GLES3
        CBUFFER_START(AdditionalLightsCookies)
    #endif
            float4x4 _AdditionalLightsWorldToLights[MAX_VISIBLE_LIGHTS_UBO];  // TODO: Should really be a property of the light !!!
            float4 _AdditionalLightsCookieAtlasUVRects[MAX_VISIBLE_LIGHTS_UBO]; // (xy: uv offset, zw: uv size)
            float4 _AdditionalLightsCookieUVScaleOffsets[MAX_VISIBLE_LIGHTS_UBO]; // UV scale and offset for repeated textures (xy: uv offset, zw: uv size) // !!! TODO !!!: waste for non-directional
            float _AdditionalLightsCookieUVWrapModes[MAX_VISIBLE_LIGHTS_UBO]; // !!! TODO !!!: waste for non-directional
            float _AdditionalLightsLightTypes[MAX_VISIBLE_LIGHTS_UBO]; // TODO: Should really be a property of the light !!!
            float _AdditionalLightsCookieAtlasFormat;
    #ifndef SHADER_API_GLES3
        CBUFFER_END
    #endif
#endif

// TODO: Should move shader type/data definitions into a data/type header

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
// TODO: prefer functions and types
#define SAMPLE_MAIN_LIGHT_COOKIE_TEXTURE(uv)         SAMPLE_TEXTURE2D(_MainLightCookieTexture, sampler_MainLightCookieTexture, uv)
#define SAMPLE_ADDITIONAL_LIGHT_COOKIE_TEXTURE(uv)   SAMPLE_TEXTURE2D(_AdditionalLightsCookieAtlasTexture, sampler_AdditionalLightsCookieAtlasTexture, uv)

// Constants
//static const float kConst = 1.0;

// Functions

// TODO: Shader data functions vs. objects prototype
// TODO: Rename / namespace
#if 0
float4x4 LightCookie_GetWorldToLight(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsWorldToLightBuffer[lightIndex];
    #else
        return _AdditionalLightsWorldToLights[lightIndex];
    #endif
}

half4 LightCookie_GetAtlasUVRect(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsCookieAtlasUVRectBuffer[lightIndex];
    #else
        return _AdditionalLightsCookieAtlasUVRects[lightIndex];
    #endif
}

float LightCookie_GetLightType(int lightIndex)
{
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return _AdditionalLightsLightTypeBuffer[lightIndex];
    #else
        return _AdditionalLightsLightTypes[lightIndex];
    #endif
}

bool LightCookie_IsEnabled(float4 uvRect)
{
    return all(uvRect == 0);
}

#else
#define PROTO_OBJ_INTERFACE 1
struct LightCookie
{
    int m_lightIndex;

    float4x4 WorldToLight()
    {
        #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
            return _AdditionalLightsWorldToLightBuffer[m_lightIndex];
        #else
            return _AdditionalLightsWorldToLights[m_lightIndex];
        #endif
    }

    half4 AtlasUVRect()
    {
        #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
            return _AdditionalLightsCookieAtlasUVRectBuffer[m_lightIndex];
        #else
            return _AdditionalLightsCookieAtlasUVRects[m_lightIndex];
        #endif
    }

    float LightType()
    {
        #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
            return _AdditionalLightsLightTypeBuffer[m_lightIndex];
        #else
            return _AdditionalLightsLightTypes[m_lightIndex];
        #endif
    }

    bool IsEnabled(float4 uvRect)
    {
        return all(uvRect == 0);
    }
};
LightCookie LightCookie_Create(int lightIndex)
{
    LightCookie l = (LightCookie)0;
    l.m_lightIndex = lightIndex;
    return l;
}
#endif

// TODO: To namespace, or not to namespace (probably should namespace buffers too)
// TODO:
// TODO: URP names are like keywords. Users extending URP can't use them.
// TODO: We can also accidentally hijack user names and break existing code by adding a symbol that conflicts with a user symbol.
// TODO: All shader code is public. Sometimes we have implementation functions that aren't really for direct usage.
// TODO: We can't protect against that, but we could mitigate that by having C style namespacing conventions.
// TODO: A consistent names space for Universal would be great.
// TODO: We could do Universal_Feature_Method or URP_Feature_Method for public API
// TODO: URP_Feature_Internal_Method or URP_Feature_Private_Method for internal API (by convention)
// TODO: Even if only 20% would avoid using internal methods, we would still reduce some breakage.
// TODO: A 20% win in reduced code breakage is still a win!
// TODO: Maximum benefit if all Universal shader code would follow standard organization and naming.
// TODO: We have quite a lot of existing code.
// TODO: Perhaps we could do a compatibility/deprecated header where we #define existing symbols to new name spaced symbols.
// TODO: Amount of future URP code will be larger that amount of existing code!

// TODO: We should organize .hlsl files per feature (when possible). Or by area "lighting".
// TODO: Build shader "modules"
// TODO: A module would be a folder/"solution filter" with module header .hlsl of the same name
// TODO: For example, LightCookie shader files would be in LightCookie/ and to include the feature would be LightCookie/LightCookie.hlsl.
// TODO: LightCookie.hlsl would include all the sub-headers.
// TODO: 1 file to include to "register" module/feature usage
// TODO: Example:
// TODO: LightCookie/
// TODO: LightCookie/LightCookie.hlsl                // Include light cookie feature to the shader. Could also implement public API (main feature functions). Includes all the sub headers.
// TODO: LightCookie/LightCookiePrivate.hlsl         // Include light cookie shader implementation detail functions.
// TODO: LightCookie/LightCookieInput.hlsl           // Include light cookie shader parameters/data. CPU -> GPU
// TODO: LightCookie/LightCookieData.hlsl            // Include light cookie shader types, data structs and enums etc. Useful, if you only need to pass them forward.
// TODO: LightCookie/LightCookieData.deprecated.hlsl // Include light cookie deprecated shader types. We should deprecate per file. We've already had cyclic issues when putting everything into one file.

// Building blocks layer: projection from world to uv

float2 LightCookie_ComputeUVDirectional(float4x4 worldToLight, float3 samplePositionWS, float4 atlasUVRect, float2 uvScale, float2 uvOffset, float2 uvWrap)
{
    // Translate and rotate 'positionWS' into the light space.
    float3 positionLS   = mul(worldToLight, float4(samplePositionWS, 1)).xyz;

    // Project
    float2 positionCS  = positionLS.xy;

    // Remap ortho CS to the texture coordinates, from [-1, 1]^2 to [0, 1]^2.
    float2 positionUV = positionCS * 0.5 + 0.5;

    // Scale UVs
    positionUV *= uvScale;
    positionUV += uvOffset;

    // Tile texture for cookie in repeat mode
    positionUV.x = (uvWrap.x == LIGHT_COOKIE_WRAP_MODE_REPEAT) ? frac(positionUV.x) : positionUV.x;
    positionUV.y = (uvWrap.y == LIGHT_COOKIE_WRAP_MODE_REPEAT) ? frac(positionUV.y) : positionUV.y;
    positionUV.x = (uvWrap.x == LIGHT_COOKIE_WRAP_MODE_CLAMP)  ? saturate(positionUV.x) : positionUV.x;
    positionUV.y = (uvWrap.y == LIGHT_COOKIE_WRAP_MODE_CLAMP)  ? saturate(positionUV.y) : positionUV.y;

    // Remap to atlas texture
    float2 positionAtlasUV = atlasUVRect.xy + float2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}

float2 LightCookie_ComputeUVSpot(float4x4 worldToLightPerspective, float3 samplePositionWS, float4 atlasUVRect)
{
    // Translate, rotate and project 'positionWS' into the light clip space.
    float4 positionCS   = mul(worldToLightPerspective, float4(samplePositionWS, 1));
    float2 positionNDC  = positionCS.xy / positionCS.w;

    // Remap NDC to the texture coordinates, from NDC [-1, 1]^2 to [0, 1]^2.
    float2 positionUV = saturate(positionNDC * 0.5 + 0.5);

    // Remap into rect in the atlas texture
    float2 positionAtlasUV = atlasUVRect.xy + half2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}

float2 LightCookie_ComputeUVPoint(float4x4 worldToLight, float3 samplePositionWS, half4 atlasUVRect)
{
    // Translate and rotate 'positionWS' into the light space.
    float4 positionLS  = mul(worldToLight, float4(samplePositionWS, 1));

    float3 sampleDirLS = normalize(positionLS.xyz / positionLS.w);

    // Project direction to Octahederal quad UV.
    float2 positionUV = saturate(PackNormalOctQuadEncode(sampleDirLS) * 0.5 + 0.5);

    // Remap to atlas texture
    float2 positionAtlasUV = atlasUVRect.xy + half2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}

// Main Layer: Cookie sampling per light

half3 LightCookie_SampleMainLightCookie(float3 samplePositionWS)
{
    float2 uv = LightCookie_ComputeUVDirectional(_MainLightWorldToLight, samplePositionWS, half4(0, 0, 1, 1), _MainLightCookieUVScale, _MainLightCookieUVOffset, LIGHT_COOKIE_WRAP_MODE_NONE);
    return MAIN_LIGHT_COOKIE_FORMAT_IS_ALPHA ? SAMPLE_MAIN_LIGHT_COOKIE_TEXTURE(uv).aaa : SAMPLE_MAIN_LIGHT_COOKIE_TEXTURE(uv).rgb;
}

half3 LightCookie_SampleAdditionalLightCookie(int perObjectLightIndex, float3 samplePositionWS)
{
    #if 0
        #ifdef PROTO_OBJ_INTERFACE
            LightCookie c = LightCookie_Create(perObjectLightIndex);
            float4 uvRect = c.AtlasUVRect();

            if(c.IsEnabled(uvRect))
                return half3(1,1,1);

            float    lightType    = c.LightType();
            float4x4 worldToLight = c.WorldToLight();
        #else
            float4 uvRect = LightCookie_GetAtlasUVRect(perObjectLightIndex);

            if(LightCookie_IsEnabled(uvRect))
                return half3(1,1,1);

            float    lightType    = LightCookie_GetLightType(perObjectLightIndex);
            float4x4 worldToLight = LightCookie_GetWorldToLightMatrix(perObjectLightIndex);
        #endif
    #else
        float4 uvRect = _AdditionalLightsCookieAtlasUVRects[perObjectLightIndex];

        // TODO: cookie enable bit, uint[8] for 256 lights
        // TODO: read less
        if(all(uvRect == 0))
            return half3(1,1,1);

        float    lightType    = _AdditionalLightsLightTypes[perObjectLightIndex];
        float4x4 worldToLight = _AdditionalLightsWorldToLights[perObjectLightIndex];
    #endif

    int isSpot        = lightType == LIGHT_COOKIE_LIGHT_TYPE_SPOT;
    int isDirectional = lightType == LIGHT_COOKIE_LIGHT_TYPE_DIRECTIONAL;

    float2 uv;
    if(isSpot)
        uv = LightCookie_ComputeUVSpot(worldToLight, samplePositionWS, uvRect);
    else if(isDirectional)
    {
        float4 dirUVScaleOffset = _AdditionalLightsCookieUVScaleOffsets[perObjectLightIndex];
        float dirUvWrapMode = _AdditionalLightsCookieUVWrapModes[perObjectLightIndex];
        uv = LightCookie_ComputeUVDirectional(worldToLight, samplePositionWS, uvRect,dirUVScaleOffset.xy, dirUVScaleOffset.zw, dirUvWrapMode);
    }
    else
        uv = LightCookie_ComputeUVPoint(worldToLight, samplePositionWS, uvRect);

    return ADDITIONAL_LIGHTS_COOKIE_FORMAT_IS_ALPHA ? SAMPLE_ADDITIONAL_LIGHT_COOKIE_TEXTURE(uv).aaa : SAMPLE_ADDITIONAL_LIGHT_COOKIE_TEXTURE(uv).rgb;
}

#endif //UNIVERSAL_LIGHT_COOKIE_INCLUDED
