#ifndef CAPSULE_SHADOWS_GLOBALS_DEF
#define CAPSULE_SHADOWS_GLOBALS_DEF

#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/CapsuleShadows/Shaders/CapsuleShadows.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/CapsuleShadows/CapsuleOccluderData.cs.hlsl"

#define _CapsuleDirectShadowCount           (_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_COUNT_MASK)
#define _CapsuleDirectShadowMethod          ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_METHOD_MASK) >> CAPSULESHADOWFLAGS_METHOD_SHIFT)
#define _CapsuleFadeDirectSelfShadow        ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_FADE_SELF_SHADOW_BIT) != 0)

#define _CapsuleShadowInLightLoop           ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_LIGHT_LOOP_BIT) != 0)
#define _CapsuleShadowIsHalfRes             ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_HALF_RES_BIT) != 0)
#define _CapsuleSplitDepthRange             ((_CapsuleDirectShadowCountAndFlags & CAPSULESHADOWFLAGS_SPLIT_DEPTH_RANGE_BIT) != 0)

#define _CapsuleIndirectShadowCount         (_CapsuleIndirectShadowCountAndFlags & CAPSULESHADOWFLAGS_COUNT_MASK)
#define _CapsuleIndirectShadowMethod        ((_CapsuleIndirectShadowCountAndFlags & CAPSULESHADOWFLAGS_METHOD_MASK) >> CAPSULESHADOWFLAGS_METHOD_SHIFT)
#define _CapsuleIndirectShadowExtra         ((_CapsuleIndirectShadowCountAndFlags & CAPSULESHADOWFLAGS_EXTRA_MASK) >> CAPSULESHADOWFLAGS_EXTRA_SHIFT)

#define _FirstDepthMipOffset                uint2(_FirstDepthMipOffsetX, _FirstDepthMipOffsetY)

uint GetCapsuleDirectOcclusionFlags()
{
#if 0
    return CAPSULE_SHADOW_FLAG_FLATTEN | CAPSULE_SHADOW_FLAG_FADE_SELF_SHADOW;
#else
    uint flags = 0;
    switch (_CapsuleDirectShadowMethod) {
    case CAPSULESHADOWMETHOD_ELLIPSOID:
        flags |= CAPSULE_SHADOW_FLAG_ELLIPSOID;
        break;
    case CAPSULESHADOWMETHOD_FLATTEN_THEN_CLOSEST_SPHERE:
        flags |= CAPSULE_SHADOW_FLAG_FLATTEN;
        break;
    }
    if (_CapsuleFadeDirectSelfShadow) {
        flags |= CAPSULE_SHADOW_FLAG_FADE_SELF_SHADOW;
    }
    return flags;
#endif
}

uint GetCapsuleAmbientOcclusionFlags()
{
#if 0
    return 0;
#else
    uint flags = 0;
    if (_CapsuleIndirectShadowExtra == CAPSULEAMBIENTOCCLUSIONMETHOD_LINE_AND_CLOSEST_SPHERE)
        flags |= CAPSULE_AMBIENT_OCCLUSION_FLAG_WITH_LINE;
    return flags;
#endif
}

uint GetCapsuleIndirectOcclusionFlags()
{
    // hardcoded (probably cheapest) shadow function
    return CAPSULE_SHADOW_FLAG_ELLIPSOID | CAPSULE_SHADOW_FLAG_FADE_SELF_SHADOW | CAPSULE_SHADOW_FLAG_HORIZON_FADE;
}

uint GetCasterType(CapsuleOccluderData capsuleData) { return (capsuleData.packedData >> 16) & 0xffU; }
uint GetLightIndex(CapsuleOccluderData capsuleData) { return (capsuleData.packedData >> 8) & 0xffU; }
uint GetLayerMask(CapsuleOccluderData capsuleData)  { return capsuleData.packedData & 0xffU; }

// store in gamma 2 to increase precision at the low end
float PackCapsuleVisibility(float visibility)   { return 1.f - sqrt(max(0.f, visibility)); }
float UnpackCapsuleVisibility(float texel)      { return Sq(1.f - texel); }
float4 UnpackCapsuleVisibility(float4 texels)   { return Sq(1.f - texels); }

#endif // ndef CAPSULE_SHADOWS_GLOBALS_DEF
