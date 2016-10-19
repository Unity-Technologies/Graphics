//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Lighting/LightDefinition.cs.  Please don't edit by hand.
//

//
// UnityEngine.ScriptableRenderLoop.AreaShapeType:  static fields
//
#define AREASHAPETYPE_RECTANGLE (0)
#define AREASHAPETYPE_LINE (1)
#define AREASHAPETYPE_SPHERE (2)
#define AREASHAPETYPE_DISK (3)
#define AREASHAPETYPE_HEMISPHERE (4)
#define AREASHAPETYPE_CYLINDER (5)

//
// UnityEngine.ScriptableRenderLoop.EnvShapeType:  static fields
//
#define ENVSHAPETYPE_NONE (0)
#define ENVSHAPETYPE_BOX (1)
#define ENVSHAPETYPE_SPHERE (2)

// Generated from UnityEngine.ScriptableRenderLoop.PunctualLightData
// PackingRules = Exact
struct PunctualLightData
{
	float3 positionWS;
	float invSqrAttenuationRadius;
	float3 color;
	float useDistanceAttenuation;
	float3 forward;
	float diffuseScale;
	float3 up;
	float specularScale;
	float3 right;
	float shadowDimmer;
	float angleScale;
	float angleOffset;
	float2 unused2;
};

// Generated from UnityEngine.ScriptableRenderLoop.AreaLightData
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
	float twoSided;
	float unused;
};

// Generated from UnityEngine.ScriptableRenderLoop.EnvLightData
// PackingRules = Exact
struct EnvLightData
{
	float3 positionWS;
	int shapeType;
	float3 forward;
	int sliceIndex;
	float3 up;
	float blendDistance;
	float3 right;
	float unused0;
	float3 innerDistance;
	float unused1;
	float3 offsetLS;
	float unused2;
};

// Generated from UnityEngine.ScriptableRenderLoop.PlanarLightData
// PackingRules = Exact
struct PlanarLightData
{
	float3 positionWS;
};

//
// Accessors for UnityEngine.ScriptableRenderLoop.PunctualLightData
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
float GetDiffuseScale(PunctualLightData value)
{
	return value.diffuseScale;
}
float3 GetUp(PunctualLightData value)
{
	return value.up;
}
float GetSpecularScale(PunctualLightData value)
{
	return value.specularScale;
}
float3 GetRight(PunctualLightData value)
{
	return value.right;
}
float GetShadowDimmer(PunctualLightData value)
{
	return value.shadowDimmer;
}
float GetAngleScale(PunctualLightData value)
{
	return value.angleScale;
}
float GetAngleOffset(PunctualLightData value)
{
	return value.angleOffset;
}
float2 GetUnused2(PunctualLightData value)
{
	return value.unused2;
}

//
// Accessors for UnityEngine.ScriptableRenderLoop.AreaLightData
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
float GetTwoSided(AreaLightData value)
{
	return value.twoSided;
}
float GetUnused(AreaLightData value)
{
	return value.unused;
}

//
// Accessors for UnityEngine.ScriptableRenderLoop.EnvLightData
//
float3 GetPositionWS(EnvLightData value)
{
	return value.positionWS;
}
int GetShapeType(EnvLightData value)
{
	return value.shapeType;
}
float3 GetForward(EnvLightData value)
{
	return value.forward;
}
int GetSliceIndex(EnvLightData value)
{
	return value.sliceIndex;
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
float GetUnused0(EnvLightData value)
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

//
// Accessors for UnityEngine.ScriptableRenderLoop.PlanarLightData
//
float3 GetPositionWS(PlanarLightData value)
{
	return value.positionWS;
}


