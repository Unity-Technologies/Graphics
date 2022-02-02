//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef CAPSULEOCCLUDERDATA_CS_HLSL
#define CAPSULEOCCLUDERDATA_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.CapsuleShadowMethod:  static fields
//
#define CAPSULESHADOWMETHOD_FLATTEN_THEN_CLOSEST_SPHERE (0)
#define CAPSULESHADOWMETHOD_CLOSEST_SPHERE (1)
#define CAPSULESHADOWMETHOD_ELLIPSOID (2)

//
// UnityEngine.Rendering.HighDefinition.CapsuleIndirectShadowMethod:  static fields
//
#define CAPSULEINDIRECTSHADOWMETHOD_AMBIENT_OCCLUSION (0)
#define CAPSULEINDIRECTSHADOWMETHOD_DIRECTIONAL (1)

//
// UnityEngine.Rendering.HighDefinition.CapsuleAmbientOcclusionMethod:  static fields
//
#define CAPSULEAMBIENTOCCLUSIONMETHOD_CLOSEST_SPHERE (0)
#define CAPSULEAMBIENTOCCLUSIONMETHOD_LINE_INTEGRAL (1)

// Generated from UnityEngine.Rendering.HighDefinition.CapsuleOccluderData
// PackingRules = Exact
struct CapsuleOccluderData
{
    float3 centerRWS;
    float radius;
    float3 axisDirWS;
    float offset;
    uint lightLayers;
    float pad0;
    float pad1;
    float pad2;
};


#endif
