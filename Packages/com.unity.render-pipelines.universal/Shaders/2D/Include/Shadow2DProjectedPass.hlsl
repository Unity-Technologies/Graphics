#ifndef SHADOW2D_PROJECTED_PASS_INCLUDED
#define SHADOW2D_PROJECTED_PASS_INCLUDED

// Entry points shared by the ProjectedSelf and ProjectedUnshadow passes of Hidden/Shadow2D.
//
// This lives in its own header rather than in the shader's HLSLINCLUDE block because the merged
// Shadow2D shader also carries the caster passes, and Shadow2DCasterVertex.hlsl declares its own
// Attributes / Varyings with different members. The two headers therefore cannot share a
// SubShader-scope include; each pass includes only the one it needs, and this file keeps the two
// projected passes from duplicating the fragment.
//
// SHADOW2D_SOFT_GEOMETRY selects which shadow geometry layout the projected passes read, matching
// Shadow2DGeometrySettings.activeFormat. Only these two passes vary: the caster passes read
// POSITION alone (plus the sprite's uv/color under SHADOW_SPRITE_CASTER), so they are shared
// verbatim across both formats. Both branches below declare `Attributes` and `Varyings` with the
// same names, so exactly one may ever be included -- which is also why the switch is here rather
// than at SubShader scope.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#if defined(SHADOW2D_SOFT_GEOMETRY)
    #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SoftShadowProjectVertex.hlsl"

    // _ShadowRadius is declared by ShadowProjectVertex.hlsl on the other branch and has no consumer
    // inside the soft header, so it belongs to this file when this is the branch in play.
    uniform float _ShadowRadius;

    // The fragment below is shared, and names the legacy header's constant. Aliased rather than
    // redefined so there is one value, not two that could drift.
    #define MIN_SHADOW_Y SOFT_SHADOW_MIN_Y
#else
    #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShadowProjectVertex.hlsl"
#endif

TEXTURE2D(_FalloffLookup);
SAMPLER(sampler_FalloffLookup);
half _ShadowSoftnessFalloffIntensity;

#if defined(SHADOW2D_SOFT_GEOMETRY)

// Projection displacement for one point, reaching a circle of radius _ShadowRadius about the light.
// Radial rather than parallel because that is the reach Legacy geometry produces and what the rest
// of URP's 2D shadows are built around: _ShadowRadius already bounds the light's influence, so a
// shadow that stops short of it would let a caster's shadow end inside the lit region.
//
// max() on the LENGTH before it becomes a direction: a point outside the radius yields a negative
// distance, which would throw it BACK through itself toward the light and invert the shadow. The
// projection no longer clamps on the caller's behalf -- a displacement has no wrong sign to clamp,
// since pointing backwards is a legitimate authored value -- so this is the only guard left on the
// built-in path.
//
// `pt` rather than `point`: point is an HLSL primitive-type keyword in geometry shader signatures
// and is rejected as a parameter name by some compilers.
float2 SoftShadowRadialThrow(float2 pt, float2 light)
{
    return SoftShadowRadialDisplacement(pt, light, max(0.0f, _ShadowRadius - length(pt - light)));
}

// The five points SoftShadowDisplacements carries, evaluated AFTER SoftShadowPrepare.
//
// The order matters and it is not cosmetic. The projection has to be the same function of a point
// no matter which vertex is asking -- adjacent edges meet only if the throw computed for `prev` at
// this vertex equals the throw computed for `v` at the previous vertex. Evaluating before prepare
// would measure the unscaled payload against a light that prepare has already moved into the scaled
// space, so the two would disagree under caster scale and the seams would open.
Varyings shadow_vert(Attributes v)
{
    SoftShadowPayload p;
    p.positionOS = v.positionOS;
    p.tangent    = v.tangent;
    p.next       = v.next;
    p.neighbor2  = v.neighbor2;

    float2 light;
    SoftShadowPrepare(p, light);

    SoftShadowDisplacements disp;
    disp.v     = SoftShadowRadialThrow(p.positionOS.xy, light);
    disp.prev  = SoftShadowRadialThrow(p.tangent.zw,    light);
    disp.next  = SoftShadowRadialThrow(p.next.xy,       light);
    disp.prev2 = SoftShadowRadialThrow(p.neighbor2.xy,  light);
    disp.next2 = SoftShadowRadialThrow(p.neighbor2.zw,  light);

    // Uniform softness: the built-in path has no per-point source for it. Only a ShaderGraph caster can
    // vary softness across a silhouette.
    float2 position, value;
    SoftShadowVert(p, disp, SoftShadowUniformSoftness(_SoftShadowAngle), position, value);

    Varyings o;
    o.vertex = SoftShadowToHClip(position);
    o.shadow = value;
    return o;
}

#else

Varyings shadow_vert(Attributes v)
{
    return ProjectShadow(v);
}

#endif

// The vertex program above -- ProjectShadow or SoftShadowVert -- performs the silhouette pole test
// and the interior half-wedge collapse, so fin geometry never overlaps the projected body and needs
// no stencil interlock to keep it out. Both projected passes share this fragment; they differ only
// in render state and colour channel.
//
// Shared across both geometry formats as well, which is by construction rather than luck: the soft
// path's SOFT_VALUE_APEX / _OPQ / _CLR are (0,0), (0,1) and (1,1), chosen to land on full shadow,
// full shadow and no shadow under exactly this expression.
half4 shadow_frag(Varyings i) : SV_Target
{
    float clamppedY = clamp(i.shadow.y, MIN_SHADOW_Y, 1);
    float value = 1.0f - saturate(abs(i.shadow.x) / clamppedY);
    half2 mappedUV = half2(value, _ShadowSoftnessFalloffIntensity);
    value = SAMPLE_TEXTURE2D(_FalloffLookup, sampler_FalloffLookup, mappedUV).r;
    return half4(value, value, value, value);
}

#endif // SHADOW2D_PROJECTED_PASS_INCLUDED
