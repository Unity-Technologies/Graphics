//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef LIGHTLOOP_CS_HLSL
#define LIGHTLOOP_CS_HLSL
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
#define LIGHTCATEGORY_DECAL (3)
#define LIGHTCATEGORY_COUNT (4)

//
// UnityEngine.Rendering.HighDefinition.LightDefinitions:  static fields
//
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
#define LIGHT_FEATURE_MASK_FLAGS (16773120)
#define LIGHT_FEATURE_MASK_FLAGS_OPAQUE (16642048)
#define LIGHT_FEATURE_MASK_FLAGS_TRANSPARENT (16510976)
#define MATERIAL_FEATURE_MASK_FLAGS (4095)
#define RAY_TRACED_SCREEN_SPACE_SHADOW_FLAG (4096)
#define SCREEN_SPACE_COLOR_SHADOW_FLAG (256)
#define INVALID_SCREEN_SPACE_SHADOW (255)
#define SCREEN_SPACE_SHADOW_INDEX_MASK (255)
#define CONTACT_SHADOW_FADE_BITS (8)
#define CONTACT_SHADOW_MASK_BITS (24)
#define CONTACT_SHADOW_FADE_MASK (255)
#define CONTACT_SHADOW_MASK_MASK (16777215)

//
// UnityEngine.Rendering.HighDefinition.ClusterDebugMode:  static fields
//
#define CLUSTERDEBUGMODE_VISUALIZE_OPAQUE (0)
#define CLUSTERDEBUGMODE_VISUALIZE_SLICE (1)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesLightList
// PackingRules = Exact
CBUFFER_START(ShaderVariablesLightList)
    float4x4 g_mInvScrProjectionArr[2];
    float4x4 g_mScrProjectionArr[2];
    float4x4 g_mInvProjectionArr[2];
    float4x4 g_mProjectionArr[2];
    float4 g_screenSize;
    int2 g_viDimensions;
    int g_iNrVisibLights;
    uint g_isOrthographic;
    uint g_BaseFeatureFlags;
    int g_iNumSamplesMSAA;
    uint _EnvLightIndexShift;
    uint _DecalIndexShift;
CBUFFER_END

// Generated from UnityEngine.Rendering.HighDefinition.SFiniteLightBound
// PackingRules = Exact
struct SFiniteLightBound
{
    float3 boxAxisX;
    float3 boxAxisY;
    float3 boxAxisZ;
    float3 center;
    float scaleXY;
    float radius;
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

//
// Accessors for UnityEngine.Rendering.HighDefinition.SFiniteLightBound
//
float3 GetBoxAxisX(SFiniteLightBound value)
{
    return value.boxAxisX;
}
float3 GetBoxAxisY(SFiniteLightBound value)
{
    return value.boxAxisY;
}
float3 GetBoxAxisZ(SFiniteLightBound value)
{
    return value.boxAxisZ;
}
float3 GetCenter(SFiniteLightBound value)
{
    return value.center;
}
float GetScaleXY(SFiniteLightBound value)
{
    return value.scaleXY;
}
float GetRadius(SFiniteLightBound value)
{
    return value.radius;
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
