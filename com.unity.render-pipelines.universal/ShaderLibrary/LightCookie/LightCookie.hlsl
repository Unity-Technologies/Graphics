#ifndef UNIVERSAL_LIGHT_COOKIE_INCLUDED
#define UNIVERSAL_LIGHT_COOKIE_INCLUDED

// Includes
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LightCookie/LightCookieInput.hlsl"

#if defined(_MAIN_LIGHT_COOKIE) || defined(_ADDITIONAL_LIGHT_COOKIES)
    #ifndef REQUIRES_WORLD_SPACE_POS_INTERPOLATOR
        #define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR 1
    #endif
#endif


float2 URP_LightCookie_ComputeUVDirectional(float4x4 worldToLight, float3 samplePositionWS, float4 atlasUVRect, float2 uvScale, float2 uvOffset, uint2 uvWrap)
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
    positionUV.x = (uvWrap.x == URP_TEXTURE_WRAP_MODE_REPEAT) ? frac(positionUV.x) : positionUV.x;
    positionUV.y = (uvWrap.y == URP_TEXTURE_WRAP_MODE_REPEAT) ? frac(positionUV.y) : positionUV.y;
    positionUV.x = (uvWrap.x == URP_TEXTURE_WRAP_MODE_CLAMP)  ? saturate(positionUV.x) : positionUV.x;
    positionUV.y = (uvWrap.y == URP_TEXTURE_WRAP_MODE_CLAMP)  ? saturate(positionUV.y) : positionUV.y;

    // Remap to atlas texture
    float2 positionAtlasUV = atlasUVRect.xy + float2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}

float2 URP_LightCookie_ComputeUVSpot(float4x4 worldToLightPerspective, float3 samplePositionWS, float4 atlasUVRect)
{
    // Translate, rotate and project 'positionWS' into the light clip space.
    float4 positionCS   = mul(worldToLightPerspective, float4(samplePositionWS, 1));
    float2 positionNDC  = positionCS.xy / positionCS.w;

    // Remap NDC to the texture coordinates, from NDC [-1, 1]^2 to [0, 1]^2.
    float2 positionUV = saturate(positionNDC * 0.5 + 0.5);

    // Remap into rect in the atlas texture
    float2 positionAtlasUV = atlasUVRect.xy + float2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}

float2 URP_LightCookie_ComputeUVPoint(float4x4 worldToLight, float3 samplePositionWS, float4 atlasUVRect)
{
    // Translate and rotate 'positionWS' into the light space.
    float4 positionLS  = mul(worldToLight, float4(samplePositionWS, 1));

    float3 sampleDirLS = normalize(positionLS.xyz / positionLS.w);

    // Project direction to Octahederal quad UV.
    float2 positionUV = saturate(PackNormalOctQuadEncode(sampleDirLS) * 0.5 + 0.5);

    // Remap to atlas texture
    float2 positionAtlasUV = atlasUVRect.xy + float2(positionUV) * atlasUVRect.zw;

    // We let the sampler handle clamping to border.
    return positionAtlasUV;
}



real3 URP_LightCookie_SampleMainLightCookie(float3 samplePositionWS)
{
    float2 uv = URP_LightCookie_ComputeUVDirectional(_MainLightWorldToLight, samplePositionWS, float4(0, 0, 1, 1),
                _MainLightCookieUVScale, _MainLightCookieUVOffset, URP_TEXTURE_WRAP_MODE_NONE);
    return
        URP_LightCookie_MainLightTextureIsAlphaFormat() ?
        URP_LightCookie_SampleMainLightTexture(uv).aaa :
        URP_LightCookie_SampleMainLightTexture(uv).rgb;
}

real3 URP_LightCookie_SampleAdditionalLightCookie(int perObjectLightIndex, float3 samplePositionWS)
{
    float4 uvRect = URP_LightCookie_GetAtlasUVRect(perObjectLightIndex);

    // TODO: pack into bits, read less
    if(URP_LightCookie_IsEnabled(uvRect))
        return real3(1,1,1);

    int lightType     = URP_LightCookie_GetLightType(perObjectLightIndex);
    int isSpot        = lightType == URP_LIGHT_TYPE_SPOT;
    int isDirectional = lightType == URP_LIGHT_TYPE_DIRECTIONAL;

    float4x4 worldToLight = URP_LightCookie_GetWorldToLightMatrix(perObjectLightIndex);

    float2 uv;
    if(isSpot)
    {
        uv = URP_LightCookie_ComputeUVSpot(worldToLight, samplePositionWS, uvRect);
    }
    else if(isDirectional)
    {
        float4 dirUVScaleOffset = URP_LightCookie_GetCookieUVScaleOffset(perObjectLightIndex);
        int    dirUvWrapMode    = URP_LightCookie_GetCookieUVWrapMode(perObjectLightIndex);
        uv = URP_LightCookie_ComputeUVDirectional(worldToLight, samplePositionWS, uvRect,
            dirUVScaleOffset.xy, dirUVScaleOffset.zw, dirUvWrapMode);
    }
    else
    {
        uv = URP_LightCookie_ComputeUVPoint(worldToLight, samplePositionWS, uvRect);
    }

    return URP_LightCookie_AdditionalLightsTextureIsAlphaFormat() ?
        URP_LightCookie_SampleAdditionalLightsTexture(uv).aaa :
        URP_LightCookie_SampleAdditionalLightsTexture(uv).rgb;
}

#endif //UNIVERSAL_LIGHT_COOKIE_INCLUDED
