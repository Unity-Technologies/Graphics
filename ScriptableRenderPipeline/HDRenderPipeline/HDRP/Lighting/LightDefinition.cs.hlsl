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
// UnityEngine.Experimental.Rendering.HDPipeline.EnvCacheType:  static fields
//
#define ENVCACHETYPE_TEXTURE2D (0)
#define ENVCACHETYPE_CUBEMAP (1)

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
    float capturePositionWSX;
    float capturePositionWSY;
    float capturePositionWSZ;
    int influenceShapeType;
    float proxyExtentsX;
    float proxyExtentsY;
    float proxyExtentsZ;
    float minProjectionDistance;
    float proxyPositionWSX;
    float proxyPositionWSY;
    float proxyPositionWSZ;
    float proxyForwardX;
    float proxyForwardY;
    float proxyForwardZ;
    float proxyUpX;
    float proxyUpY;
    float proxyUpZ;
    float proxyRightX;
    float proxyRightY;
    float proxyRightZ;
    float influencePositionWSX;
    float influencePositionWSY;
    float influencePositionWSZ;
    float influenceForwardX;
    float influenceForwardY;
    float influenceForwardZ;
    float influenceUpX;
    float influenceUpY;
    float influenceUpZ;
    float influenceRightX;
    float influenceRightY;
    float influenceRightZ;
    float influenceExtentsX;
    float influenceExtentsY;
    float influenceExtentsZ;
    float unused00;
    float blendDistancePositiveX;
    float blendDistancePositiveY;
    float blendDistancePositiveZ;
    float blendDistanceNegativeX;
    float blendDistanceNegativeY;
    float blendDistanceNegativeZ;
    float blendNormalDistancePositiveX;
    float blendNormalDistancePositiveY;
    float blendNormalDistancePositiveZ;
    float blendNormalDistanceNegativeX;
    float blendNormalDistanceNegativeY;
    float blendNormalDistanceNegativeZ;
    float boxSideFadePositiveX;
    float boxSideFadePositiveY;
    float boxSideFadePositiveZ;
    float boxSideFadeNegativeX;
    float boxSideFadeNegativeY;
    float boxSideFadeNegativeZ;
    float dimmer;
    float unused01;
    float sampleDirectionDiscardWSX;
    float sampleDirectionDiscardWSY;
    float sampleDirectionDiscardWSZ;
    int envIndex;
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
float GetCapturePositionWSX(EnvLightData value)
{
	return value.capturePositionWSX;
}
float GetCapturePositionWSY(EnvLightData value)
{
	return value.capturePositionWSY;
}
float GetCapturePositionWSZ(EnvLightData value)
{
	return value.capturePositionWSZ;
}
int GetInfluenceShapeType(EnvLightData value)
{
	return value.influenceShapeType;
}
float GetProxyExtentsX(EnvLightData value)
{
	return value.proxyExtentsX;
}
float GetProxyExtentsY(EnvLightData value)
{
	return value.proxyExtentsY;
}
float GetProxyExtentsZ(EnvLightData value)
{
	return value.proxyExtentsZ;
}
float GetMinProjectionDistance(EnvLightData value)
{
	return value.minProjectionDistance;
}
float GetProxyPositionWSX(EnvLightData value)
{
	return value.proxyPositionWSX;
}
float GetProxyPositionWSY(EnvLightData value)
{
	return value.proxyPositionWSY;
}
float GetProxyPositionWSZ(EnvLightData value)
{
	return value.proxyPositionWSZ;
}
float GetProxyForwardX(EnvLightData value)
{
	return value.proxyForwardX;
}
float GetProxyForwardY(EnvLightData value)
{
	return value.proxyForwardY;
}
float GetProxyForwardZ(EnvLightData value)
{
	return value.proxyForwardZ;
}
float GetProxyUpX(EnvLightData value)
{
	return value.proxyUpX;
}
float GetProxyUpY(EnvLightData value)
{
	return value.proxyUpY;
}
float GetProxyUpZ(EnvLightData value)
{
	return value.proxyUpZ;
}
float GetProxyRightX(EnvLightData value)
{
	return value.proxyRightX;
}
float GetProxyRightY(EnvLightData value)
{
	return value.proxyRightY;
}
float GetProxyRightZ(EnvLightData value)
{
	return value.proxyRightZ;
}
float GetInfluencePositionWSX(EnvLightData value)
{
	return value.influencePositionWSX;
}
float GetInfluencePositionWSY(EnvLightData value)
{
	return value.influencePositionWSY;
}
float GetInfluencePositionWSZ(EnvLightData value)
{
	return value.influencePositionWSZ;
}
float GetInfluenceForwardX(EnvLightData value)
{
	return value.influenceForwardX;
}
float GetInfluenceForwardY(EnvLightData value)
{
	return value.influenceForwardY;
}
float GetInfluenceForwardZ(EnvLightData value)
{
	return value.influenceForwardZ;
}
float GetInfluenceUpX(EnvLightData value)
{
	return value.influenceUpX;
}
float GetInfluenceUpY(EnvLightData value)
{
	return value.influenceUpY;
}
float GetInfluenceUpZ(EnvLightData value)
{
	return value.influenceUpZ;
}
float GetInfluenceRightX(EnvLightData value)
{
	return value.influenceRightX;
}
float GetInfluenceRightY(EnvLightData value)
{
	return value.influenceRightY;
}
float GetInfluenceRightZ(EnvLightData value)
{
	return value.influenceRightZ;
}
float GetInfluenceExtentsX(EnvLightData value)
{
	return value.influenceExtentsX;
}
float GetInfluenceExtentsY(EnvLightData value)
{
	return value.influenceExtentsY;
}
float GetInfluenceExtentsZ(EnvLightData value)
{
	return value.influenceExtentsZ;
}
float GetUnused00(EnvLightData value)
{
	return value.unused00;
}
float GetBlendDistancePositiveX(EnvLightData value)
{
	return value.blendDistancePositiveX;
}
float GetBlendDistancePositiveY(EnvLightData value)
{
	return value.blendDistancePositiveY;
}
float GetBlendDistancePositiveZ(EnvLightData value)
{
	return value.blendDistancePositiveZ;
}
float GetBlendDistanceNegativeX(EnvLightData value)
{
	return value.blendDistanceNegativeX;
}
float GetBlendDistanceNegativeY(EnvLightData value)
{
	return value.blendDistanceNegativeY;
}
float GetBlendDistanceNegativeZ(EnvLightData value)
{
	return value.blendDistanceNegativeZ;
}
float GetBlendNormalDistancePositiveX(EnvLightData value)
{
	return value.blendNormalDistancePositiveX;
}
float GetBlendNormalDistancePositiveY(EnvLightData value)
{
	return value.blendNormalDistancePositiveY;
}
float GetBlendNormalDistancePositiveZ(EnvLightData value)
{
	return value.blendNormalDistancePositiveZ;
}
float GetBlendNormalDistanceNegativeX(EnvLightData value)
{
	return value.blendNormalDistanceNegativeX;
}
float GetBlendNormalDistanceNegativeY(EnvLightData value)
{
	return value.blendNormalDistanceNegativeY;
}
float GetBlendNormalDistanceNegativeZ(EnvLightData value)
{
	return value.blendNormalDistanceNegativeZ;
}
float GetBoxSideFadePositiveX(EnvLightData value)
{
	return value.boxSideFadePositiveX;
}
float GetBoxSideFadePositiveY(EnvLightData value)
{
	return value.boxSideFadePositiveY;
}
float GetBoxSideFadePositiveZ(EnvLightData value)
{
	return value.boxSideFadePositiveZ;
}
float GetBoxSideFadeNegativeX(EnvLightData value)
{
	return value.boxSideFadeNegativeX;
}
float GetBoxSideFadeNegativeY(EnvLightData value)
{
	return value.boxSideFadeNegativeY;
}
float GetBoxSideFadeNegativeZ(EnvLightData value)
{
	return value.boxSideFadeNegativeZ;
}
float GetDimmer(EnvLightData value)
{
	return value.dimmer;
}
float GetUnused01(EnvLightData value)
{
	return value.unused01;
}
float GetSampleDirectionDiscardWSX(EnvLightData value)
{
	return value.sampleDirectionDiscardWSX;
}
float GetSampleDirectionDiscardWSY(EnvLightData value)
{
	return value.sampleDirectionDiscardWSY;
}
float GetSampleDirectionDiscardWSZ(EnvLightData value)
{
	return value.sampleDirectionDiscardWSZ;
}
int GetEnvIndex(EnvLightData value)
{
	return value.envIndex;
}


#endif
#include "LightDefinition.cs.custom.hlsl"