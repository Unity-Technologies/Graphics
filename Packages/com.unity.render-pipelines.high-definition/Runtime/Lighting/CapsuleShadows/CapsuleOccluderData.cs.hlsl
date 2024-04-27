//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef CAPSULEOCCLUDERDATA_CS_HLSL
#define CAPSULEOCCLUDERDATA_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.CapsuleShadowCasterType:  static fields
//
#define CAPSULESHADOWCASTERTYPE_DIRECTIONAL (0)
#define CAPSULESHADOWCASTERTYPE_POINT (1)
#define CAPSULESHADOWCASTERTYPE_SPOT (2)
#define CAPSULESHADOWCASTERTYPE_INDIRECT (3)

// Generated from UnityEngine.Rendering.HighDefinition.CapsuleShadowOccluder
// PackingRules = Exact
struct CapsuleShadowOccluder
{
    float3 centerRWS;
    float radius;
    float3 axisDirWS;
    float offset;
    float3 indirectDirWS;
    uint layerMask;
};

// Generated from UnityEngine.Rendering.HighDefinition.CapsuleShadowCaster
// PackingRules = Exact
struct CapsuleShadowCaster
{
    uint casterTypeAndLayerMask;
    float shadowRange;
    float maxCosTheta;
    float lightRange;
    float3 directionWS;
    float spotCosTheta;
    float3 positionRWS;
    float radiusWS;
};

// Generated from UnityEngine.Rendering.HighDefinition.CapsuleShadowVolume
// PackingRules = Exact
struct CapsuleShadowVolume
{
    uint bits;
};


#endif
