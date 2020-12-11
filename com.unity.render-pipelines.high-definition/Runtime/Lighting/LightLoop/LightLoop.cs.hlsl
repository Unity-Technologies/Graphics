//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit / Render Pipeline / Generate Shader Includes ] instead
//

#ifndef LIGHTLOOP_CS_HLSL
#define LIGHTLOOP_CS_HLSL

//
// UnityEngine.Rendering.HighDefinition.LightVolumeType:  static fields
//
#define LIGHTVOLUMETYPE_CONE (0)
#define LIGHTVOLUMETYPE_SPHERE (1)
#define LIGHTVOLUMETYPE_BOX (2)
#define LIGHTVOLUMETYPE_COUNT (3)

//
// UnityEngine.Rendering.HighDefinition.LightCategory:  static fields
//
#define LIGHTCATEGORY_PUNCTUAL (0)
#define LIGHTCATEGORY_AREA (1)
#define LIGHTCATEGORY_ENV (2)
#define LIGHTCATEGORY_PROBE_VOLUME (3)
#define LIGHTCATEGORY_DECAL (4)
#define LIGHTCATEGORY_DENSITY_VOLUME (5)
#define LIGHTCATEGORY_COUNT (6)

//
// UnityEngine.Rendering.HighDefinition.BoundedEntityCategory:  static fields
//
#define BOUNDEDENTITYCATEGORY_PUNCTUAL_LIGHT (0)
#define BOUNDEDENTITYCATEGORY_AREA_LIGHT (1)
#define BOUNDEDENTITYCATEGORY_REFLECTION_PROBE (2)
#define BOUNDEDENTITYCATEGORY_DECAL (3)
#define BOUNDEDENTITYCATEGORY_DENSITY_VOLUME (4)
#define BOUNDEDENTITYCATEGORY_COUNT (5)
#define BOUNDEDENTITYCATEGORY_NONE (5)

//
// UnityEngine.Rendering.HighDefinition.LightFeatureFlags:  static fields
//
#define LIGHTFEATUREFLAGS_PUNCTUAL (4096)
#define LIGHTFEATUREFLAGS_AREA (8192)
#define LIGHTFEATUREFLAGS_DIRECTIONAL (16384)
#define LIGHTFEATUREFLAGS_ENV (32768)
#define LIGHTFEATUREFLAGS_SKY (65536)
#define LIGHTFEATUREFLAGS_SSREFRACTION (131072)
#define LIGHTFEATUREFLAGS_SSREFLECTION (262144)
#define LIGHTFEATUREFLAGS_PROBE_VOLUME (524288)

//
// UnityEngine.Rendering.HighDefinition.TiledLightingConstants:  static fields
//
#define MAX_NR_BIG_TILE_LIGHTS_PLUS_ONE (512)
#define VIEWPORT_SCALE_Z (1)
#define USE_LEFT_HAND_CAMERA_SPACE (1)
#define TILE_SIZE_FPTL (16)
#define TILE_SIZE_CLUSTERED (32)
#define TILE_SIZE_BIG_TILE (64)
#define TILE_INDEX_MASK (32767)
#define TILE_INDEX_SHIFT_X (0)
#define TILE_INDEX_SHIFT_Y (15)
#define TILE_INDEX_SHIFT_EYE (30)
#define NUM_FEATURE_VARIANTS (29)
#define LIGHT_LIST_MAX_COARSE_ENTRIES (64)
#define LIGHT_LIST_MAX_PRUNED_ENTRIES (24)
#define LIGHT_CLUSTER_MAX_COARSE_ENTRIES (128)
#define LIGHT_FEATURE_MASK_FLAGS (16773120)
#define LIGHT_FEATURE_MASK_FLAGS_OPAQUE (16642048)
#define LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT (16510976)
#define MATERIAL_FEATURE_MASK_FLAGS (4095)
#define SCREEN_SPACE_COLOR_SHADOW_FLAG (256)
#define INVALID_SCREEN_SPACE_SHADOW (255)
#define SCREEN_SPACE_SHADOW_INDEX_MASK (255)
#define COARSE_TILE_ENTRY_LIMIT (64)
#define FINE_TILE_ENTRY_LIMIT (16)
#define COARSE_TILE_SIZE (64)
#define FINE_TILE_SIZE (8)
#define Z_BIN_COUNT (8192)
#define MAX_REFLECTION_PROBES_PER_PIXEL (4)
#define MAX_WORD_PER_ENTITY (16)

//
// UnityEngine.Rendering.HighDefinition.ClusterDebugMode:  static fields
//
#define CLUSTERDEBUGMODE_VISUALIZE_OPAQUE (0)
#define CLUSTERDEBUGMODE_VISUALIZE_SLICE (1)

// Generated from UnityEngine.Rendering.HighDefinition.FiniteLightBound
// PackingRules = Exact
struct FiniteLightBound
{
    float3 center;
    float radius;
    float3 boxAxisX;
    float scaleXY;
    float3 boxAxisY;
    float __pad0__;
    float3 boxAxisZ;
    float __pad1__;
};

// Generated from UnityEngine.Rendering.HighDefinition.LightVolumeData
// PackingRules = Exact
struct LightVolumeData
{
    float3 lightPos;
    uint lightVolume;
    float3 lightAxisX;
    uint lightCategory;
    float3 lightAxisY;
    float radiusSq;
    float3 lightAxisZ;
    float cotan;
    float3 boxInnerDist;
    uint featureFlags;
    float3 boxInvRange;
    float unused2;
};

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesLightList
// PackingRules = Exact
CBUFFER_START(ShaderVariablesLightList)
    float4x4 g_mInvScrProjectionArr[2];
    float4x4 g_mScrProjectionArr[2];
    float4x4 g_mInvProjectionArr[2];
    float4x4 g_mProjectionArr[2];
    float4 g_screenSize;
    int2 g_viDimensions;
    uint _BoundedEntityCount;
    uint g_isOrthographic;
    uint g_BaseFeatureFlags;
    int g_iNumSamplesMSAA;
    uint _EnvLightIndexShift;
    uint _DecalIndexShift;
    uint _DensityVolumeIndexShift;
    uint _ProbeVolumeIndexShift;
    uint _Pad0_SVLL;
    uint _Pad1_SVLL;
CBUFFER_END

//
// Accessors for UnityEngine.Rendering.HighDefinition.FiniteLightBound
//
float3 GetCenter(FiniteLightBound value)
{
    return value.center;
}
float GetRadius(FiniteLightBound value)
{
    return value.radius;
}
float3 GetBoxAxisX(FiniteLightBound value)
{
    return value.boxAxisX;
}
float GetScaleXY(FiniteLightBound value)
{
    return value.scaleXY;
}
float3 GetBoxAxisY(FiniteLightBound value)
{
    return value.boxAxisY;
}
float Get__pad0__(FiniteLightBound value)
{
    return value.__pad0__;
}
float3 GetBoxAxisZ(FiniteLightBound value)
{
    return value.boxAxisZ;
}
float Get__pad1__(FiniteLightBound value)
{
    return value.__pad1__;
}
//
// Accessors for UnityEngine.Rendering.HighDefinition.LightVolumeData
//
float3 GetLightPos(LightVolumeData value)
{
    return value.lightPos;
}
uint GetLightVolume(LightVolumeData value)
{
    return value.lightVolume;
}
float3 GetLightAxisX(LightVolumeData value)
{
    return value.lightAxisX;
}
uint GetLightCategory(LightVolumeData value)
{
    return value.lightCategory;
}
float3 GetLightAxisY(LightVolumeData value)
{
    return value.lightAxisY;
}
float GetRadiusSq(LightVolumeData value)
{
    return value.radiusSq;
}
float3 GetLightAxisZ(LightVolumeData value)
{
    return value.lightAxisZ;
}
float GetCotan(LightVolumeData value)
{
    return value.cotan;
}
float3 GetBoxInnerDist(LightVolumeData value)
{
    return value.boxInnerDist;
}
uint GetFeatureFlags(LightVolumeData value)
{
    return value.featureFlags;
}
float3 GetBoxInvRange(LightVolumeData value)
{
    return value.boxInvRange;
}
float GetUnused2(LightVolumeData value)
{
    return value.unused2;
}

#endif
