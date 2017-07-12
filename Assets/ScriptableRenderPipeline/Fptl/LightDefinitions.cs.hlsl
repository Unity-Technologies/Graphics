//
// This file was automatically generated from Assets/ScriptableRenderPipeline/Fptl/LightDefinitions.cs.  Please don't edit by hand.
//

#ifndef LIGHTDEFINITIONS_CS_HLSL
#define LIGHTDEFINITIONS_CS_HLSL
//
// LightDefinitions:  static fields
//
#define MAX_NR_LIGHTS_PER_CAMERA (1024)
#define MAX_NR_BIGTILE_LIGHTS_PLUSONE (512)
#define VIEWPORT_SCALE_Z (1)
#define TILE_SIZE_CLUSTERED (32)
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

// Generated from SFiniteLightData
// PackingRules = Exact
struct SFiniteLightData
{
    float penumbra;
    int flags;
    uint lightType;
    uint lightModel;
    float3 lightPos;
    float lightIntensity;
    float3 lightAxisX;
    float recipRange;
    float3 lightAxisY;
    float radiusSq;
    float3 lightAxisZ;
    float cotan;
    float3 color;
    int sliceIndex;
    float3 boxInnerDist;
    float decodeExp;
    float3 boxInvRange;
    uint shadowLightIndex;
    float3 localCubeCapturePoint;
    float probeBlendDistance;
};

// Generated from SFiniteLightBound
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

// Generated from DirectionalLight
// PackingRules = Exact
struct DirectionalLight
{
    float3 color;
    float intensity;
    float3 lightAxisX;
    uint shadowLightIndex;
    float3 lightAxisY;
    float pad0;
    float3 lightAxisZ;
    float pad1;
};

//
// Accessors for SFiniteLightData
//
float GetPenumbra(SFiniteLightData value)
{
	return value.penumbra;
}
int GetFlags(SFiniteLightData value)
{
	return value.flags;
}
uint GetLightType(SFiniteLightData value)
{
	return value.lightType;
}
uint GetLightModel(SFiniteLightData value)
{
	return value.lightModel;
}
float3 GetLightPos(SFiniteLightData value)
{
	return value.lightPos;
}
float GetLightIntensity(SFiniteLightData value)
{
	return value.lightIntensity;
}
float3 GetLightAxisX(SFiniteLightData value)
{
	return value.lightAxisX;
}
float GetRecipRange(SFiniteLightData value)
{
	return value.recipRange;
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
float3 GetColor(SFiniteLightData value)
{
	return value.color;
}
int GetSliceIndex(SFiniteLightData value)
{
	return value.sliceIndex;
}
float3 GetBoxInnerDist(SFiniteLightData value)
{
	return value.boxInnerDist;
}
float GetDecodeExp(SFiniteLightData value)
{
	return value.decodeExp;
}
float3 GetBoxInvRange(SFiniteLightData value)
{
	return value.boxInvRange;
}
uint GetShadowLightIndex(SFiniteLightData value)
{
	return value.shadowLightIndex;
}
float3 GetLocalCubeCapturePoint(SFiniteLightData value)
{
	return value.localCubeCapturePoint;
}
float GetProbeBlendDistance(SFiniteLightData value)
{
	return value.probeBlendDistance;
}

//
// Accessors for SFiniteLightBound
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
// Accessors for DirectionalLight
//
float3 GetColor(DirectionalLight value)
{
	return value.color;
}
float GetIntensity(DirectionalLight value)
{
	return value.intensity;
}
float3 GetLightAxisX(DirectionalLight value)
{
	return value.lightAxisX;
}
uint GetShadowLightIndex(DirectionalLight value)
{
	return value.shadowLightIndex;
}
float3 GetLightAxisY(DirectionalLight value)
{
	return value.lightAxisY;
}
float GetPad0(DirectionalLight value)
{
	return value.pad0;
}
float3 GetLightAxisZ(DirectionalLight value)
{
	return value.lightAxisZ;
}
float GetPad1(DirectionalLight value)
{
	return value.pad1;
}


#endif
