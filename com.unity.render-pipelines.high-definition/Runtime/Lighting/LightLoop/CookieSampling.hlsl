//-----------------------------------------------------------------------------
// Cookie sampling functions
// ----------------------------------------------------------------------------

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

#define COOKIE_ATLAS_SIZE _CookieAtlasData.x
#define COOKIE_ATLAS_RCP_PADDING _CookieAtlasData.y
#define COOKIE_ATLAS_LAST_VALID_MIP _CookieAtlasData.z

// Adjust UVs using the LOD padding size
float2 RemapUVWithPadding(float2 coord, float2 size, float rcpPaddingWidth, float lod)
{
    float2 scale = rcp(size + rcpPaddingWidth) * size;
    float2 offset = 0.5 * (1.0 - scale);

    // Avoid edge bleeding for texture when sampling with lod by clamping uvs:
    float2 mipClamp = pow(2, lod) / (size * COOKIE_ATLAS_SIZE * 2);
    return clamp(coord * scale + offset, mipClamp, 1 - mipClamp);
}

// Used by directional, spots and area lights.
float3 SampleCookie2D(float2 coord, float4 scaleOffset, float lod = 0) // TODO: mip maps for cookies
{
    float2 scale        = scaleOffset.xy;
    float2 offset       = scaleOffset.zw;

    // Remap the uv to take in account the padding
    coord = RemapUVWithPadding(coord, scale, COOKIE_ATLAS_RCP_PADDING, max(0, lod - COOKIE_ATLAS_LAST_VALID_MIP));

    // Apply atlas scale and offset
    float2 atlasCoords = coord * scale + offset;

    float3 color = SAMPLE_TEXTURE2D_LOD(_CookieAtlas, s_trilinear_clamp_sampler, atlasCoords, lod).rgb;

    // Mip visualization (0 -> red, 10 -> blue)
    // color = saturate(1 - abs(3 * lod / 10 - float4(0, 1, 2, 3))).rgb;

    return color;
}

// Used by point lights.
float3 SamplePointCookie(float3 lightToSample, float3 worldPos, float3 forward, PositionInputs posInput, float4 scaleOffset, float lod = 0)
{
    float2 params = PackNormalOctQuadEncode(lightToSample);
    float2 uv     = saturate(params*0.5f + 0.5f);

    float scale = 1.0f;

    //float d = abs(dot(worldPos - _WorldSpaceCameraPos, lightToSample));
        //abs(dot(lightToSample, worldPos - _WorldSpaceCameraPos));

    //float dd = posInput.positionWS - _WorldSpaceCameraPos;
    //float d = dot(dd, dd);

    //float2 ddx_ = scale * uvdx;
    //float2 ddy_ = scale * uvdy;
    //float  d = max(dot(ddx_, ddx_), dot(ddy_, ddy_));

#if UNITY_REVERSED_Z == 1
    float d = 1.0f - posInput.linearDepth;
#else
    float d = posInput.linearDepth;
#endif
        //(posInput.linearDepth - UNITY_NEAR_CLIP_VALUE)/(UNITY_RAW_FAR_CLIP_VALUE - UNITY_NEAR_CLIP_VALUE);

    //float usedLod = max(0.5f*log2(d/15000.0f), 0.0f);
        //max(0.5 * log2(d) - bias, 0.0);
    //float usedLod = saturate(smoothstep(0.0f, 1.0f, d))*COOKIE_ATLAS_LAST_VALID_MIP;
    float usedLod = saturate( 10000000.0f/**( d )*/ )*COOKIE_ATLAS_LAST_VALID_MIP;
    //COOKIE_ATLAS_LAST_VALID_MIP

    return SampleCookie2D(uv, scaleOffset, usedLod/*lod*/);
}
