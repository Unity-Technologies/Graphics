//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIGHTDEFINITION_CS_HLSL
#define LIGHTDEFINITION_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.GPULightType:  static fields
//
#define GPULIGHTTYPE_DIRECTIONAL (0)
#define GPULIGHTTYPE_POINT (1)
#define GPULIGHTTYPE_SPOT (2)
#define GPULIGHTTYPE_PROJECTOR_PYRAMID (3)
#define GPULIGHTTYPE_PROJECTOR_BOX (4)
#define GPULIGHTTYPE_TUBE (5)
#define GPULIGHTTYPE_RECTANGLE (6)

//
// UnityEngine.Rendering.HighDefinition.GPUImageBasedLightingType:  static fields
//
#define GPUIMAGEBASEDLIGHTINGTYPE_REFLECTION (0)
#define GPUIMAGEBASEDLIGHTINGTYPE_REFRACTION (1)

//
// UnityEngine.Rendering.HighDefinition.EnvShapeType:  static fields
//
#define ENVSHAPETYPE_NONE (0)
#define ENVSHAPETYPE_BOX (1)
#define ENVSHAPETYPE_SPHERE (2)
#define ENVSHAPETYPE_SKY (3)

//
// UnityEngine.Rendering.HighDefinition.EnvConstants:  static fields
//
#define ENVCONSTANTS_SPEC_CUBE_LOD_STEP (6)

//
// UnityEngine.Rendering.HighDefinition.EnvCacheType:  static fields
//
#define ENVCACHETYPE_TEXTURE2D (0)
#define ENVCACHETYPE_CUBEMAP (1)

//
// UnityEngine.Rendering.HighDefinition.StencilLightingUsage:  static fields
//
#define STENCILLIGHTINGUSAGE_NO_LIGHTING (0)
#define STENCILLIGHTINGUSAGE_SPLIT_LIGHTING (1)
#define STENCILLIGHTINGUSAGE_REGULAR_LIGHTING (2)

// Generated from UnityEngine.Rendering.HighDefinition.DirectionalLightData
// PackingRules = Exact
struct DirectionalLightData
{
    float3 positionRWS;
    uint lightLayers;
    float lightDimmer;
    float volumetricLightDimmer;
    float3 forward;
    int cookieIndex;
    float3 right;
    int tileCookie;
    float3 up;
    int shadowIndex;
    float3 color;
    int contactShadowMask;
    float3 shadowTint;
    float shadowDimmer;
    float volumetricShadowDimmer;
    int nonLightMappedOnly;
    real minRoughness;
    int screenSpaceShadowIndex;
    real4 shadowMaskSelector;
    float diffuseDimmer;
    float specularDimmer;
    float angularDiameter;
    float distanceFromCamera;
    float isRayTracedContactShadow;
};

// Generated from UnityEngine.Rendering.HighDefinition.LightData
// PackingRules = Exact
struct LightData
{
    float3 positionRWS;
    uint lightLayers;
    float lightDimmer;
    float volumetricLightDimmer;
    real angleScale;
    real angleOffset;
    float3 forward;
    int lightType;
    float3 right;
    real range;
    float3 up;
    float rangeAttenuationScale;
    float3 color;
    float rangeAttenuationBias;
    int cookieIndex;
    int tileCookie;
    int shadowIndex;
    int contactShadowMask;
    float3 shadowTint;
    float shadowDimmer;
    float volumetricShadowDimmer;
    int nonLightMappedOnly;
    real minRoughness;
    int screenSpaceShadowIndex;
    real4 shadowMaskSelector;
    real2 size;
    float diffuseDimmer;
    float specularDimmer;
    float isRayTracedContactShadow;
};

// Generated from UnityEngine.Rendering.HighDefinition.EnvLightData
// PackingRules = Exact
struct EnvLightData
{
    uint lightLayers;
    float3 capturePositionRWS;
    int influenceShapeType;
    float3 proxyExtents;
    real minProjectionDistance;
    float3 proxyPositionRWS;
    float3 proxyForward;
    float3 proxyUp;
    float3 proxyRight;
    float3 influencePositionRWS;
    float3 influenceForward;
    float3 influenceUp;
    float3 influenceRight;
    float3 influenceExtents;
    float unused00;
    float3 blendDistancePositive;
    float3 blendDistanceNegative;
    float3 blendNormalDistancePositive;
    float3 blendNormalDistanceNegative;
    real3 boxSideFadePositive;
    real3 boxSideFadeNegative;
    float weight;
    float multiplier;
    float rangeCompressionFactorCompensation;
    int envIndex;
};


#endif
