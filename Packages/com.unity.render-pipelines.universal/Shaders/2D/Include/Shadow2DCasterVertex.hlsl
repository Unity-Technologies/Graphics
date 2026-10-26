#ifndef SHADOW2D_CASTER_VERTEX_INCLUDED
#define SHADOW2D_CASTER_VERTEX_INCLUDED

// Shared vertex/fragment helpers for the merged Shadow2D caster shader.
// The SHADOW_SPRITE_CASTER keyword selects the sprite variant (sprite alpha
// coverage + optional 2D-Animation skinning) vs. the default geometry variant
// (position-only, coverage constant 1). Keeping both variants behind a keyword
// on one shader is what lets the future vertex-only ShaderGraph master node
// emit a single shader that satisfies both caster roles.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if defined(SHADOW_SPRITE_CASTER)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
#endif

struct Attributes
{
    float3 positionOS : POSITION;
#if defined(SHADOW_SPRITE_CASTER)
    float2 uv         : TEXCOORD0;
    float4 color      : COLOR;
    UNITY_SKINNED_VERTEX_INPUTS
#endif
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
#if defined(SHADOW_SPRITE_CASTER)
    float2 uv          : TEXCOORD0;
    float4 color       : COLOR;
#endif
};

#if defined(SHADOW_SPRITE_CASTER)
sampler2D _MainTex;
float4    _MainTex_ST;
float4    _Color;
#endif

// Referenced by the UnshadowMark / UnshadowUnmark passes; harmless when the
// geometry variant (which never samples it) is compiled.
float _ShadowAlphaCutoff;

Varyings ShadowCasterVert(Attributes v)
{
    Varyings o;
#if defined(SHADOW_SPRITE_CASTER)
    UNITY_SKINNED_VERTEX_COMPUTE(v);
    v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
    o.positionHCS = TransformObjectToHClip(v.positionOS);
    o.uv          = TRANSFORM_TEX(v.uv, _MainTex);
    o.color       = _Color.a * v.color;
#else
    o.positionHCS = TransformObjectToHClip(v.positionOS);
#endif
    return o;
}

// Caster coverage in [0,1]: sprite-tinted alpha for sprite variant, constant 1
// for geometry. Consumed by every pass to feed R (Self) or B (Unmark).
half GetShadowCasterAlpha(Varyings i)
{
#if defined(SHADOW_SPRITE_CASTER)
    return (i.color * tex2D(_MainTex, i.uv)).a;
#else
    return 1;
#endif
}

// No-op for geometry variant. Only sprite pixels are alpha-tested against
// the global _ShadowAlphaCutoff before writing stencil / B.
void DiscardShadowCasterIfBelowCutoff(half a)
{
#if defined(SHADOW_SPRITE_CASTER)
    if (a <= _ShadowAlphaCutoff)
        discard;
#endif
}

#endif // SHADOW2D_CASTER_VERTEX_INCLUDED
