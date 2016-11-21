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
#define MAX_TYPES (3)
#define SPOT_LIGHT (0)
#define SPHERE_LIGHT (1)
#define BOX_LIGHT (2)
#define DIRECTIONAL_LIGHT (3)
#define NR_LIGHT_MODELS (2)
#define DIRECT_LIGHT (0)
#define REFLECTION_LIGHT (1)

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

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.TilePass.SFiniteLightData
// PackingRules = Exact
struct SFiniteLightData
{
	float3 lightPos;
	float3 lightAxisX;
	uint lightType;
	float3 lightAxisY;
	float radiusSq;
	float3 lightAxisZ;
	float cotan;
	float3 boxInnerDist;
	uint lightModel;
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
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.TilePass.SFiniteLightData
//
float3 GetLightPos(SFiniteLightData value)
{
	return value.lightPos;
}
float3 GetLightAxisX(SFiniteLightData value)
{
	return value.lightAxisX;
}
uint GetLightType(SFiniteLightData value)
{
	return value.lightType;
}
float3 GetLightAxisY(SFiniteLightData value)
{
	return value.lightAxisY;
}
float GetRadiusSq(SFiniteLightData value)
{
	return value.radiusSq;
}
float3 GetLightAxisZ(SFiniteLightData value)
{
	return value.lightAxisZ;
}
float GetCotan(SFiniteLightData value)
{
	return value.cotan;
}
float3 GetBoxInnerDist(SFiniteLightData value)
{
	return value.boxInnerDist;
}
uint GetLightModel(SFiniteLightData value)
{
	return value.lightModel;
}
float3 GetBoxInvRange(SFiniteLightData value)
{
	return value.boxInvRange;
}
float GetUnused2(SFiniteLightData value)
{
	return value.unused2;
}


#endif
