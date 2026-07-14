#ifndef SHADOW2D_SPRITE_VERTEX_INCLUDED
#define SHADOW2D_SPRITE_VERTEX_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

struct Attributes
{
    float3 positionOS : POSITION;
    float2 uv         : TEXCOORD0;
    float4 color      : COLOR;
    UNITY_SKINNED_VERTEX_INPUTS
};

struct Varyings
{
    float4 vertex   : SV_POSITION;
    float2 uv       : TEXCOORD0;
    float4 color    : COLOR;
};

sampler2D _MainTex;
float4    _MainTex_ST;
float4    _Color;

Varyings ShadowSpriteVert(Attributes v)
{
    Varyings o;
    UNITY_SKINNED_VERTEX_COMPUTE(v);
    v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
    o.vertex = TransformObjectToHClip(v.positionOS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.color = _Color.a * v.color;
    return o;
}

#endif // SHADOW2D_SPRITE_VERTEX_INCLUDED
