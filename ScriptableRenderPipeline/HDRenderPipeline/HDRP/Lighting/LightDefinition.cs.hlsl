//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIGHTDEFINITION_CS_HLSL
#define LIGHTDEFINITION_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.GPULightType:  static fields
//
#define GPULIGHTTYPE_DIRECTIONAL (0)
#define GPULIGHTTYPE_POINT (1)
#define GPULIGHTTYPE_SPOT (2)
#define GPULIGHTTYPE_PROJECTOR_PYRAMID (3)
#define GPULIGHTTYPE_PROJECTOR_BOX (4)
#define GPULIGHTTYPE_LINE (5)
#define GPULIGHTTYPE_RECTANGLE (6)

//
// UnityEngine.Experimental.Rendering.HDPipeline.GPUImageBasedLightingType:  static fields
//
#define GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION (0)
#define GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION (1)

//
// UnityEngine.Experimental.Rendering.HDPipeline.EnvShapeType:  static fields
//
#define ENVSHAPETYPE_NONE (0)
#define ENVSHAPETYPE_BOX (1)
#define ENVSHAPETYPE_SPHERE (2)
#define ENVSHAPETYPE_SKY (3)

//
// UnityEngine.Experimental.Rendering.HDPipeline.EnvConstants:  static fields
//
#define ENVCONSTANTS_SPEC_CUBE_LOD_STEP (6)

//
// UnityEngine.Experimental.Rendering.HDPipeline.StencilLightingUsage:  static fields
//
#define STENCILLIGHTINGUSAGE_NO_LIGHTING (0)
#define STENCILLIGHTINGUSAGE_SPLIT_LIGHTING (1)
#define STENCILLIGHTINGUSAGE_REGULAR_LIGHTING (2)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.DirectionalLightData
// PackingRules = Exact
struct DirectionalLightData
{
    float3 positionWS;
    int tileCookie;
    float3 color;
    int shadowIndex;
    float3 forward;
    int cookieIndex;
    float3 right;
    float specularScale;
    float3 up;
    float diffuseScale;
    float2 fadeDistanceScaleAndBias;
    float unused0;
    int dynamicShadowCasterOnly;
    float4 shadowMaskSelector;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.LightData
// PackingRules = Exact
struct LightData
{
    float3 positionWS;
    float invSqrAttenuationRadius;
    float3 color;
    int shadowIndex;
    float3 forward;
    int cookieIndex;
    float3 right;
    float specularScale;
    float3 up;
    float diffuseScale;
    float angleScale;
    float angleOffset;
    float shadowDimmer;
    int dynamicShadowCasterOnly;
    float4 shadowMaskSelector;
    float2 size;
    int lightType;
    float minRoughness;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.EnvLightData
// PackingRules = Exact
struct EnvLightData
{
    float3 positionWS;
    int envShapeType;
    float3 forward;
    int envIndex;
    float3 up;
    float dimmer;
    float3 right;
    float minProjectionDistance;
    float3 influenceExtents;
    float unused0;
    float3 offsetLS;
    float unused1;
    float3 blendDistancePositive;
    float unused2;
    float3 blendDistanceNegative;
    float unused3;
    float3 blendNormalDistancePositive;
    float unused4;
    float3 blendNormalDistanceNegative;
    float unused5;
    float3 boxSideFadePositive;
    float unused6;
    float3 boxSideFadeNegative;
    float unused7;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.DirectionalLightData
//
float3 GetPositionWS(DirectionalLightData value)
{
	return value.positionWS;
}
int GetTileCookie(DirectionalLightData value)
{
	return value.tileCookie;
}
float3 GetColor(DirectionalLightData value)
{
	return value.color;
}
int GetShadowIndex(DirectionalLightData value)
{
	return value.shadowIndex;
}
float3 GetForward(DirectionalLightData value)
{
	return value.forward;
}
int GetCookieIndex(DirectionalLightData value)
{
	return value.cookieIndex;
}
float3 GetRight(DirectionalLightData value)
{
	return value.right;
}
float GetSpecularScale(DirectionalLightData value)
{
	return value.specularScale;
}
float3 GetUp(DirectionalLightData value)
{
	return value.up;
}
float GetDiffuseScale(DirectionalLightData value)
{
	return value.diffuseScale;
}
float2 GetFadeDistanceScaleAndBias(DirectionalLightData value)
{
	return value.fadeDistanceScaleAndBias;
}
float GetUnused0(DirectionalLightData value)
{
	return value.unused0;
}
int GetDynamicShadowCasterOnly(DirectionalLightData value)
{
	return value.dynamicShadowCasterOnly;
}
float4 GetShadowMaskSelector(DirectionalLightData value)
{
	return value.shadowMaskSelector;
}

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.LightData
//
float3 GetPositionWS(LightData value)
{
	return value.positionWS;
}
float GetInvSqrAttenuationRadius(LightData value)
{
	return value.invSqrAttenuationRadius;
}
float3 GetColor(LightData value)
{
	return value.color;
}
int GetShadowIndex(LightData value)
{
	return value.shadowIndex;
}
float3 GetForward(LightData value)
{
	return value.forward;
}
int GetCookieIndex(LightData value)
{
	return value.cookieIndex;
}
float3 GetRight(LightData value)
{
	return value.right;
}
float GetSpecularScale(LightData value)
{
	return value.specularScale;
}
float3 GetUp(LightData value)
{
	return value.up;
}
float GetDiffuseScale(LightData value)
{
	return value.diffuseScale;
}
float GetAngleScale(LightData value)
{
	return value.angleScale;
}
float GetAngleOffset(LightData value)
{
	return value.angleOffset;
}
float GetShadowDimmer(LightData value)
{
	return value.shadowDimmer;
}
int GetDynamicShadowCasterOnly(LightData value)
{
	return value.dynamicShadowCasterOnly;
}
float4 GetShadowMaskSelector(LightData value)
{
	return value.shadowMaskSelector;
}
float2 GetSize(LightData value)
{
	return value.size;
}
int GetLightType(LightData value)
{
	return value.lightType;
}
float GetMinRoughness(LightData value)
{
	return value.minRoughness;
}

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.EnvLightData
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
int GetEnvIndex(EnvLightData value)
{
	return value.envIndex;
}
float3 GetUp(EnvLightData value)
{
	return value.up;
}
float GetDimmer(EnvLightData value)
{
	return value.dimmer;
}
float3 GetRight(EnvLightData value)
{
	return value.right;
}
float GetMinProjectionDistance(EnvLightData value)
{
	return value.minProjectionDistance;
}
float3 GetInfluenceExtents(EnvLightData value)
{
	return value.influenceExtents;
}
float GetUnused0(EnvLightData value)
{
	return value.unused0;
}
float3 GetOffsetLS(EnvLightData value)
{
	return value.offsetLS;
}
float GetUnused1(EnvLightData value)
{
	return value.unused1;
}
float3 GetBlendDistancePositive(EnvLightData value)
{
	return value.blendDistancePositive;
}
float GetUnused2(EnvLightData value)
{
	return value.unused2;
}
float3 GetBlendDistanceNegative(EnvLightData value)
{
	return value.blendDistanceNegative;
}
float GetUnused3(EnvLightData value)
{
	return value.unused3;
}
float3 GetBlendNormalDistancePositive(EnvLightData value)
{
	return value.blendNormalDistancePositive;
}
float GetUnused4(EnvLightData value)
{
	return value.unused4;
}
float3 GetBlendNormalDistanceNegative(EnvLightData value)
{
	return value.blendNormalDistanceNegative;
}
float GetUnused5(EnvLightData value)
{
	return value.unused5;
}
float3 GetBoxSideFadePositive(EnvLightData value)
{
	return value.boxSideFadePositive;
}
float GetUnused6(EnvLightData value)
{
	return value.unused6;
}
float3 GetBoxSideFadeNegative(EnvLightData value)
{
	return value.boxSideFadeNegative;
}
float GetUnused7(EnvLightData value)
{
	return value.unused7;
}


#endif
