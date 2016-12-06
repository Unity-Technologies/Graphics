//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderLoop/Lighting/TilePass/TilePass.cs.  Please don't edit by hand.
//

#ifndef TILEPASS_CS_HLSL
#define TILEPASS_CS_HLSL
//
// UnityEngine.Experimental.ScriptableRenderLoop.TilePass.LightDefinitions:  static fields
//
#define MAX_NR_LIGHTS_PER_CAMERA (1024)
#define MAX_NR_BIGTILE_LIGHTS_PLUSONE (512)
#define VIEWPORT_SCALE_Z (1)
#define USE_LEFTHAND_CAMERASPACE (0)
#define IS_CIRCULAR_SPOT_SHAPE (1)
#define HAS_COOKIE_TEXTURE (2)
#define IS_BOX_PROJECTED (4)
#define HAS_SHADOW (8)
#define MAX_VOLUME_TYPES (3)
#define SPOT_VOLUME (0)
#define SPHERE_VOLUME (1)
#define BOX_VOLUME (2)
#define DIRECTIONAL_VOLUME (3)
#define NR_LIGHT_CATEGORIES (3)
#define DIRECT_LIGHT_CATEGORY (0)
#define REFLECTION_LIGHT_CATEGORY (1)
#define AREA_LIGHT_CATEGORY (2)

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.TilePass.SFiniteLightBound
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

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.TilePass.LightShapeData
// PackingRules = Exact
struct LightShapeData
{
	float3 lightPos;
	uint lightIndex;
	float3 lightAxisX;
	uint lightVolume;
	float3 lightAxisY;
	float radiusSq;
	float3 lightAxisZ;
	float cotan;
	float3 boxInnerDist;
	uint lightCategory;
	float3 boxInvRange;
	float unused2;
};

//
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.TilePass.SFiniteLightBound
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
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.TilePass.LightShapeData
//
float3 GetLightPos(LightShapeData value)
{
	return value.lightPos;
}
uint GetLightIndex(LightShapeData value)
{
	return value.lightIndex;
}
float3 GetLightAxisX(LightShapeData value)
{
	return value.lightAxisX;
}
uint GetLightVolume(LightShapeData value)
{
	return value.lightVolume;
}
float3 GetLightAxisY(LightShapeData value)
{
	return value.lightAxisY;
}
float GetRadiusSq(LightShapeData value)
{
	return value.radiusSq;
}
float3 GetLightAxisZ(LightShapeData value)
{
	return value.lightAxisZ;
}
float GetCotan(LightShapeData value)
{
	return value.cotan;
}
float3 GetBoxInnerDist(LightShapeData value)
{
	return value.boxInnerDist;
}
uint GetLightCategory(LightShapeData value)
{
	return value.lightCategory;
}
float3 GetBoxInvRange(LightShapeData value)
{
	return value.boxInvRange;
}
float GetUnused2(LightShapeData value)
{
	return value.unused2;
}


#endif
