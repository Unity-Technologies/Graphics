//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef CAPSULEOCCLUDERDATA_CS_HLSL
#define CAPSULEOCCLUDERDATA_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.CapsuleShadowMethod:  static fields
//
#define CAPSULESHADOWMETHOD_FLATTEN_THEN_CLOSEST_SPHERE (0)
#define CAPSULESHADOWMETHOD_CLOSEST_SPHERE (1)
#define CAPSULESHADOWMETHOD_ELLIPSOID (2)

//
// UnityEngine.Rendering.HighDefinition.CapsuleIndirectShadowMethod:  static fields
//
#define CAPSULEINDIRECTSHADOWMETHOD_AMBIENT_OCCLUSION (0)
#define CAPSULEINDIRECTSHADOWMETHOD_DIRECTION_AT_SURFACE (1)
#define CAPSULEINDIRECTSHADOWMETHOD_DIRECTION_AT_CAPSULE (2)

//
// UnityEngine.Rendering.HighDefinition.CapsuleAmbientOcclusionMethod:  static fields
//
#define CAPSULEAMBIENTOCCLUSIONMETHOD_CLOSEST_SPHERE (0)
#define CAPSULEAMBIENTOCCLUSIONMETHOD_LINE_AND_CLOSEST_SPHERE (1)

//
// UnityEngine.Rendering.HighDefinition.CapsuleShadowUpscaleMethod:  static fields
//
#define CAPSULESHADOWUPSCALEMETHOD_SINGLE_GATHER4 (0)
#define CAPSULESHADOWUPSCALEMETHOD_DOUBLE_GATHER4 (1)
#define CAPSULESHADOWUPSCALEMETHOD_QUAD_GATHER4 (2)

//
// UnityEngine.Rendering.HighDefinition.CapsuleShadowFlags:  static fields
//
#define CAPSULESHADOWFLAGS_COUNT_MASK (4095)
#define CAPSULESHADOWFLAGS_METHOD_SHIFT (12)
#define CAPSULESHADOWFLAGS_METHOD_MASK (61440)
#define CAPSULESHADOWFLAGS_EXTRA_SHIFT (16)
#define CAPSULESHADOWFLAGS_EXTRA_MASK (983040)
#define CAPSULESHADOWFLAGS_DIRECT_ENABLED_BIT (65536)
#define CAPSULESHADOWFLAGS_INDIRECT_ENABLED_BIT (131072)
#define CAPSULESHADOWFLAGS_FADE_SELF_SHADOW_BIT (262144)
#define CAPSULESHADOWFLAGS_LIGHT_LOOP_BIT (524288)
#define CAPSULESHADOWFLAGS_SPLIT_DEPTH_RANGE_BIT (1048576)
#define CAPSULESHADOWFLAGS_HALF_RES_BIT (2097152)
#define CAPSULESHADOWFLAGS_NEEDS_UPSCALE_BIT (4194304)
#define CAPSULESHADOWFLAGS_NEEDS_TILE_CHECK_BIT (8388608)
#define CAPSULESHADOWFLAGS_UPSCALE_SHIFT (24)
#define CAPSULESHADOWFLAGS_UPSCALE_MASK (50331648)

//
// UnityEngine.Rendering.HighDefinition.CapsuleShadowCasterType:  static fields
//
#define CAPSULESHADOWCASTERTYPE_DIRECTIONAL (0)
#define CAPSULESHADOWCASTERTYPE_POINT (1)
#define CAPSULESHADOWCASTERTYPE_SPOT (2)
#define CAPSULESHADOWCASTERTYPE_INDIRECT (3)

//
// UnityEngine.Rendering.HighDefinition.CapsuleShadowCaster:  static fields
//
#define MAX_CAPSULE_SHADOW_CASTER_COUNT (8)
#define CAPSULE_SHADOW_INDIRECT_INDEX_TILE_COUNT (0)
#define CAPSULE_SHADOW_INDIRECT_INDEX_SHADOW_COUNT (3)
#define CAPSULE_SHADOW_INDIRECT_UINT_COUNT (4)

// Generated from UnityEngine.Rendering.HighDefinition.CapsuleOccluderData
// PackingRules = Exact
struct CapsuleOccluderData
{
    float3 centerRWS;
    float radius;
    float3 axisDirWS;
    float offset;
    float3 indirectDirWS;
    uint packedData;
};

// Generated from UnityEngine.Rendering.HighDefinition.CapsuleShadowCaster
// PackingRules = Exact
struct CapsuleShadowCaster
{
    uint casterType;
    float shadowRange;
    float maxCosTheta;
    float lightRange;
    float3 directionWS;
    float spotCosTheta;
    float3 positionRWS;
    float radiusWS;
};


#endif
