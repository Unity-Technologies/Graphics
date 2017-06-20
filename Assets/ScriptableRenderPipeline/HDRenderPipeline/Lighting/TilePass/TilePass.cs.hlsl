//
// This file was automatically generated from Assets/ScriptableRenderPipeline/HDRenderPipeline/Lighting/TilePass/TilePass.cs.  Please don't edit by hand.
//

#ifndef TILEPASS_CS_HLSL
#define TILEPASS_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.TilePass.LightVolumeType:  static fields
//
#define LIGHTVOLUMETYPE_CONE (0)
#define LIGHTVOLUMETYPE_SPHERE (1)
#define LIGHTVOLUMETYPE_BOX (2)
#define LIGHTVOLUMETYPE_COUNT (3)

//
// UnityEngine.Experimental.Rendering.HDPipeline.TilePass.LightCategory:  static fields
//
#define LIGHTCATEGORY_PUNCTUAL (0)
#define LIGHTCATEGORY_AREA (1)
#define LIGHTCATEGORY_PROJECTOR (2)
#define LIGHTCATEGORY_ENV (3)
#define LIGHTCATEGORY_COUNT (4)

//
// UnityEngine.Experimental.Rendering.HDPipeline.TilePass.LightFeatureFlags:  static fields
//
#define LIGHTFEATUREFLAGS_PUNCTUAL (1)
#define LIGHTFEATUREFLAGS_AREA (2)
#define LIGHTFEATUREFLAGS_DIRECTIONAL (4)
#define LIGHTFEATUREFLAGS_PROJECTOR (8)
#define LIGHTFEATUREFLAGS_ENV (16)
#define LIGHTFEATUREFLAGS_SKY (32)

//
// UnityEngine.Experimental.Rendering.HDPipeline.TilePass.LightDefinitions:  static fields
//
#define MAX_NR_LIGHTS_PER_CAMERA (1024)
#define MAX_NR_BIG_TILE_LIGHTS_PLUS_ONE (512)
#define VIEWPORT_SCALE_Z (1)
#define USE_LEFT_HAND_CAMERA_SPACE (1)
#define TILE_SIZE_FPTL (16)
#define TILE_SIZE_CLUSTERED (32)
#define NUM_FEATURE_VARIANTS (16)
#define LIGHTFEATUREFLAGS_MASK (4095)
#define MATERIALFEATUREFLAGS_MASK (61440)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.TilePass.SFiniteLightBound
// PackingRules = Exact
struct SFiniteLightBound
{
    float3 boxAxisX;
    float3 boxAxisY;
    float3 boxAxisZ;
    float3 center;
    float2 scaleXY;
    float radius;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.TilePass.LightVolumeData
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
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.TilePass.SFiniteLightBound
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
float2 GetScaleXY(SFiniteLightBound value)
{
	return value.scaleXY;
}
float GetRadius(SFiniteLightBound value)
{
	return value.radius;
}

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.TilePass.LightVolumeData
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
