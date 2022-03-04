#ifndef CAPSULE_SHADOWS_GLOBALS_DEF
#define CAPSULE_SHADOWS_GLOBALS_DEF

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/CapsuleOccluderFlags.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOccluderData.cs.hlsl"

#define _CapsuleDirectShadowCount           (_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_COUNT_MASK)
#define _CapsuleDirectShadowMethod          ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_METHOD_MASK) >> CAPSULESHADOWFLAGS_METHOD_SHIFT)
#define _CapsuleDirectShadowsEnabled        ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_DIRECT_ENABLED_BIT) != 0)
#define _CapsuleIndirectShadowsEnabled      ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_INDIRECT_ENABLED_BIT) != 0)
#define _CapsuleFadeDirectSelfShadow        ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_FADE_SELF_SHADOW_BIT) != 0)
#define _CapsuleAdjustEllipsoidCone         ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_ADJUST_ELLIPSOID_CONE_BIT) != 0)
#define _CapsuleSoftPartialOcclusion        ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_SOFT_PARTIAL_OCCLUSION_BIT) != 0)
#define _CapsuleShadowInLightLoop           ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_LIGHT_LOOP_BIT) != 0)
#define _CapsuleSplitDepthRange             ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_SPLIT_DEPTH_RANGE_BIT) != 0)
#define _CapsuleShadowIsHalfRes             ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_HALF_RES_BIT) != 0)
#define _CapsuleShadowsNeedsTileCheck       ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_NEEDS_TILE_CHECK_BIT) != 0)

#define _CapsuleIndirectShadowCount         (_CapsuleIndirectShadowCountAndExtra & CAPSULESHADOWFLAGS_COUNT_MASK)
#define _CapsuleIndirectShadowMethod        ((_CapsuleIndirectShadowCountAndExtra & CAPSULESHADOWFLAGS_METHOD_MASK) >> CAPSULESHADOWFLAGS_METHOD_SHIFT)
#define _CapsuleIndirectShadowExtra         ((_CapsuleIndirectShadowCountAndExtra & CAPSULESHADOWFLAGS_EXTRA_MASK) >> CAPSULESHADOWFLAGS_EXTRA_SHIFT)

#define _FirstDepthMipOffset                uint2(_FirstDepthMipOffsetX, _FirstDepthMipOffsetY)
#define _CapsuleRenderSizeInTiles           uint2(_CapsuleRenderSizeInTilesX, _CapsuleRenderSizeInTilesY)
#define _CapsuleUpscaledSizeInTiles         uint2(_CapsuleUpscaledSizeInTilesX, _CapsuleUpscaledSizeInTilesY)

#if defined(CAPSULE_DIRECT_SHADOW_0)
#define CAPSULE_DIRECT_SHADOW_FIXED_METHOD 0
#elif defined(CAPSULE_DIRECT_SHADOW_1)
#define CAPSULE_DIRECT_SHADOW_FIXED_METHOD 1
#elif defined(CAPSULE_DIRECT_SHADOW_2)
#define CAPSULE_DIRECT_SHADOW_FIXED_METHOD 2
#elif defined(CAPSULE_DIRECT_SHADOW_3)
#define CAPSULE_DIRECT_SHADOW_FIXED_METHOD 3
#endif

uint GetCapsuleDirectOcclusionFlags()
{
#if defined(CAPSULE_DIRECT_SHADOW_FIXED_METHOD)
    uint method = CAPSULE_DIRECT_SHADOW_FIXED_METHOD;
    bool fadeSelfShadow = true;
    bool adjustEllipsoidCone = true;
    bool softPartialOcclusion = true;
#else
    uint method = _CapsuleDirectShadowMethod;
    bool fadeSelfShadow = _CapsuleFadeDirectSelfShadow;
    bool adjustEllipsoidCone = _CapsuleAdjustEllipsoidCone;
    bool softPartialOcclusion = _CapsuleSoftPartialOcclusion;
#endif
    uint flags = 0;
    switch (method) {
    case CAPSULESHADOWMETHOD_ELLIPSOID:
        flags |= CAPSULEOCCLUSIONFLAGS_CAPSULE_AXIS_SCALE;
        break;
    case CAPSULESHADOWMETHOD_CLIP_THEN_ELLIPSOID:
        flags |= CAPSULEOCCLUSIONFLAGS_CLIP_TO_CONE | CAPSULEOCCLUSIONFLAGS_CAPSULE_AXIS_SCALE;
        break;
    case CAPSULESHADOWMETHOD_CLOSEST_SPHERE:
        // no additional flags needed
        break;
    case CAPSULESHADOWMETHOD_FLATTEN_THEN_CLOSEST_SPHERE:
        flags |= CAPSULEOCCLUSIONFLAGS_LIGHT_AXIS_SCALE;
        break;
    case CAPSULESHADOWMETHOD_RAY_TRACED_REFERENCE:
        flags |= CAPSULEOCCLUSIONFLAGS_RAY_TRACED_REFERENCE;
        break;
    }
    if (fadeSelfShadow)
        flags |= CAPSULEOCCLUSIONFLAGS_FADE_SELF_SHADOW;
    if (adjustEllipsoidCone)
        flags |= CAPSULEOCCLUSIONFLAGS_ADJUST_LIGHT_CONE_DURING_CAPSULE_AXIS_SCALE;
    if (softPartialOcclusion)
        flags |= CAPSULEOCCLUSIONFLAGS_SOFT_PARTIAL_OCCLUSION;
    return flags;
}

uint GetCapsuleAmbientOcclusionFlags()
{
    uint flags = 0;
    if (_CapsuleIndirectShadowExtra == CAPSULEAMBIENTOCCLUSIONMETHOD_LINE_AND_CLOSEST_SPHERE)
        flags |= CAPSULEAMBIENTOCCLUSIONFLAGS_INCLUDE_AXIS;
    return flags;
}

uint GetCapsuleIndirectOcclusionFlags()
{
    // hardcoded (probably cheapest) shadow function
    return CAPSULEOCCLUSIONFLAGS_CAPSULE_AXIS_SCALE | CAPSULEOCCLUSIONFLAGS_FADE_SELF_SHADOW | CAPSULEOCCLUSIONFLAGS_FADE_AT_HORIZON;
}

struct CapsuleShadowsUpscaleTile
{
    uint coord; // half resolution tile coordinate [31:30]=viewIndex, [29:15]=y, [14:0]=x
    uint bits;  // one bit per caster
};

CapsuleShadowsUpscaleTile makeCapsuleShadowsUpscaleTile(uint2 coord, uint bits)
{
    uint viewIndex = unity_StereoEyeIndex;

    CapsuleShadowsUpscaleTile tile;
    tile.bits = bits;
    tile.coord = (viewIndex << 30) | (coord.y << 15) | coord.x;
    return tile;
}
uint GetUpscaleViewIndex(CapsuleShadowsUpscaleTile tile)    { return tile.coord >> 30; }
uint2 GetUpscaleTileCoord(CapsuleShadowsUpscaleTile tile)   { return uint2(tile.coord & 0x7fffU, (tile.coord >> 15) & 0x7fffU); }
uint GetUpscaleTileBits(CapsuleShadowsUpscaleTile tile)     { return tile.bits; }

uint GetCapsuleLayerMask(CapsuleOccluderData capsule)   { return (capsule.packedData >> 16) & 0xffU; }
uint GetCapsuleCasterType(CapsuleOccluderData capsule)  { return (capsule.packedData >> 8) & 0xffU; }
uint GetCapsuleCasterIndex(CapsuleOccluderData capsule) { return capsule.packedData & 0xffU; }

// store in gamma 2 to increase precision at the low end
float PackCapsuleVisibility(float visibility)   { return 1.f - sqrt(max(0.f, visibility)); }
float UnpackCapsuleVisibility(float texel)      { return Sq(1.f - texel); }

#endif // ndef CAPSULE_SHADOWS_GLOBALS_DEF
