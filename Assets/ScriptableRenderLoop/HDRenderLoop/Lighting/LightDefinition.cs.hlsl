//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderLoop/Lighting/LightDefinition.cs.  Please don't edit by hand.
//

#ifndef LIGHTDEFINITION_CS_HLSL
#define LIGHTDEFINITION_CS_HLSL
//
// UnityEngine.Experimental.ScriptableRenderLoop.ShadowType:  static fields
//
#define SHADOWTYPE_SPOT (0)
#define SHADOWTYPE_DIRECTIONAL (1)
#define SHADOWTYPE_POINT (2)

//
// UnityEngine.Experimental.ScriptableRenderLoop.AreaShapeType:  static fields
//
#define AREASHAPETYPE_RECTANGLE (0)
#define AREASHAPETYPE_LINE (1)
#define AREASHAPETYPE_SPHERE (2)
#define AREASHAPETYPE_DISK (3)
#define AREASHAPETYPE_HEMISPHERE (4)
#define AREASHAPETYPE_CYLINDER (5)

//
// UnityEngine.Experimental.ScriptableRenderLoop.EnvShapeType:  static fields
//
#define ENVSHAPETYPE_NONE (0)
#define ENVSHAPETYPE_BOX (1)
#define ENVSHAPETYPE_SPHERE (2)

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.PunctualLightData
// PackingRules = Exact
struct PunctualLightData
{
	float3 positionWS;
	float invSqrAttenuationRadius;
	float3 color;
	float useDistanceAttenuation;
	float3 forward;
	float angleScale;
	float3 up;
	float angleOffset;
	float3 right;
	float diffuseScale;
	float specularScale;
	float shadowDimmer;
	int shadowIndex;
	int IESIndex;
	int cookieIndex;
	float3 unused;
};

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.PunctualShadowData
// PackingRules = Exact
struct PunctualShadowData
{
	float4x4 worldToShadow;
	int shadowType;
	float bias;
	float quality;
	float2 unused;
};

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.AreaLightData
// PackingRules = Exact
struct AreaLightData
{
	float3 positionWS;
	float invSqrAttenuationRadius;
	float3 color;
	int shapeType;
	float3 forward;
	float diffuseScale;
	float3 up;
	float specularScale;
	float3 right;
	float shadowDimmer;
	float2 size;
	bool twoSided;
	float unused;
};

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.EnvLightData
// PackingRules = Exact
struct EnvLightData
{
	float3 positionWS;
	int envShapeType;
	float3 forward;
	float envIndex;
	float3 up;
	float blendDistance;
	float3 right;
	int unused0;
	float3 innerDistance;
	float unused1;
	float3 offsetLS;
	float unused2;
};

//
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.PunctualLightData
//
float3 GetPositionWS(PunctualLightData value)
{
	return value.positionWS;
}
float GetInvSqrAttenuationRadius(PunctualLightData value)
{
	return value.invSqrAttenuationRadius;
}
float3 GetColor(PunctualLightData value)
{
	return value.color;
}
float GetUseDistanceAttenuation(PunctualLightData value)
{
	return value.useDistanceAttenuation;
}
float3 GetForward(PunctualLightData value)
{
	return value.forward;
}
float GetAngleScale(PunctualLightData value)
{
	return value.angleScale;
}
float3 GetUp(PunctualLightData value)
{
	return value.up;
}
float GetAngleOffset(PunctualLightData value)
{
	return value.angleOffset;
}
float3 GetRight(PunctualLightData value)
{
	return value.right;
}
float GetDiffuseScale(PunctualLightData value)
{
	return value.diffuseScale;
}
float GetSpecularScale(PunctualLightData value)
{
	return value.specularScale;
}
float GetShadowDimmer(PunctualLightData value)
{
	return value.shadowDimmer;
}
int GetShadowIndex(PunctualLightData value)
{
	return value.shadowIndex;
}
int GetIESIndex(PunctualLightData value)
{
	return value.IESIndex;
}
int GetCookieIndex(PunctualLightData value)
{
	return value.cookieIndex;
}
float3 GetUnused(PunctualLightData value)
{
	return value.unused;
}

//
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.PunctualShadowData
//
float4x4 GetWorldToShadow(PunctualShadowData value)
{
	return value.worldToShadow;
}
int GetShadowType(PunctualShadowData value)
{
	return value.shadowType;
}
float GetBias(PunctualShadowData value)
{
	return value.bias;
}
float GetQuality(PunctualShadowData value)
{
	return value.quality;
}
float2 GetUnused(PunctualShadowData value)
{
	return value.unused;
}

//
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.AreaLightData
//
float3 GetPositionWS(AreaLightData value)
{
	return value.positionWS;
}
float GetInvSqrAttenuationRadius(AreaLightData value)
{
	return value.invSqrAttenuationRadius;
}
float3 GetColor(AreaLightData value)
{
	return value.color;
}
int GetShapeType(AreaLightData value)
{
	return value.shapeType;
}
float3 GetForward(AreaLightData value)
{
	return value.forward;
}
float GetDiffuseScale(AreaLightData value)
{
	return value.diffuseScale;
}
float3 GetUp(AreaLightData value)
{
	return value.up;
}
float GetSpecularScale(AreaLightData value)
{
	return value.specularScale;
}
float3 GetRight(AreaLightData value)
{
	return value.right;
}
float GetShadowDimmer(AreaLightData value)
{
	return value.shadowDimmer;
}
float2 GetSize(AreaLightData value)
{
	return value.size;
}
bool GetTwoSided(AreaLightData value)
{
	return value.twoSided;
}
float GetUnused(AreaLightData value)
{
	return value.unused;
}

//
// Accessors for UnityEngine.Experimental.ScriptableRenderLoop.EnvLightData
//
float3 GetPositionWS(EnvLightData value)
{
	return value.positionWS;
}
int GetEnvShapeType(EnvLightData value)
{
	return value.envShapeType;
}
float3 GetForward(EnvLightData value)
{
	return value.forward;
}
float GetEnvIndex(EnvLightData value)
{
	return value.envIndex;
}
float3 GetUp(EnvLightData value)
{
	return value.up;
}
float GetBlendDistance(EnvLightData value)
{
	return value.blendDistance;
}
float3 GetRight(EnvLightData value)
{
	return value.right;
}
int GetUnused0(EnvLightData value)
{
	return value.unused0;
}
float3 GetInnerDistance(EnvLightData value)
{
	return value.innerDistance;
}
float GetUnused1(EnvLightData value)
{
	return value.unused1;
}
float3 GetOffsetLS(EnvLightData value)
{
	return value.offsetLS;
}
float GetUnused2(EnvLightData value)
{
	return value.unused2;
}


#endif
