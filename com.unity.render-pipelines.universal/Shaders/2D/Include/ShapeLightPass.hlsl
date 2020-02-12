#ifndef UNIVERSAL_2D_SHAPE_LIGHT_PASS_INCLUDED
#define UNIVERSAL_2D_SHAPE_LIGHT_PASS_INCLUDED

struct Attributes
{
    float3 positionOS   : POSITION;
    float4 color        : COLOR;

#ifdef SPRITE_LIGHT
    float2 uv           : TEXCOORD0;
#endif
};

struct Varyings
{
    float4  positionCS  : SV_POSITION;
    float4  color       : COLOR;
    float2  uv          : TEXCOORD0;
    float2  gBufferUV    : TEXCOORD1;

    //SHADOW_COORDS(TEXCOORD1)
    NORMALS_LIGHTING_COORDS(TEXCOORD2, TEXCOORD3)
};

float  _InverseHDREmulationScale;
float4 _LightColor;
float  _FalloffDistance;
float4 _FalloffOffset;
float   _VolumeOpacity;

#ifdef SPRITE_LIGHT
    TEXTURE2D(_CookieTex);			// This can either be a sprite texture uv or a falloff texture
    SAMPLER(sampler_CookieTex);
#else
    float _FalloffIntensity;
    TEXTURE2D(_FalloffLookup);
    SAMPLER(sampler_FalloffLookup);
#endif

NORMALS_LIGHTING_VARIABLES
SHADOW_VARIABLES

Varyings vert(Attributes attributes)
{
    Varyings o = (Varyings)0;

    float3 positionOS = attributes.positionOS;
    positionOS.x = positionOS.x + _FalloffDistance * attributes.color.r + (1 - attributes.color.a) * _FalloffOffset.x;
    positionOS.y = positionOS.y + _FalloffDistance * attributes.color.g + (1 - attributes.color.a) * _FalloffOffset.y;

    o.positionCS = TransformObjectToHClip(positionOS);
    o.color = _LightColor * _InverseHDREmulationScale;
    o.color.a = attributes.color.a;

#ifdef SPRITE_LIGHT
    o.uv = attributes.uv;
#else
    o.uv = float2(o.color.a, _FalloffIntensity);
#endif

    float4 worldSpacePos;
    worldSpacePos.xyz = TransformObjectToWorld(positionOS);
    worldSpacePos.w = 1;
    TRANSFER_NORMALS_LIGHTING(o, worldSpacePos)
    //TRANSFER_SHADOWS(o)

    o.gBufferUV = ComputeScreenPos(o.positionCS / o.positionCS.w).xy * _GBufferColor_TexelSize.zw;

    return o;
}

half4 frag(Varyings i) : SV_Target
{
    half4 color = i.color;

#if SPRITE_LIGHT
    half4 cookie = SAMPLE_TEXTURE2D(_CookieTex, sampler_CookieTex, i.uv);
    #if USE_ADDITIVE_BLENDING
        color *= cookie * cookie.a;
    #else
        color *= cookie;
    #endif
#else
    #if USE_ADDITIVE_BLENDING
        color *= SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
    #else
        color.a = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, i.uv).r;
    #endif
#endif

    half4 volumeColor = color * _VolumeOpacity;

    APPLY_NORMALS_LIGHTING_NEW(i, color);
    //APPLY_SHADOWS(i, color, _ShadowIntensity);

    return BlendLightingWithBaseColor(color.rgb, i.gBufferUV) + volumeColor;
}
#endif
