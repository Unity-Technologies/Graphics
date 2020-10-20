//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DECAL_CS_HLSL
#define DECAL_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.Decal+DBufferMaterial:  static fields
//
#define DBUFFERMATERIAL_COUNT (4)

// Generated from UnityEngine.Rendering.HighDefinition.Decal+DecalSurfaceData
// PackingRules = Exact
struct DecalSurfaceData
{
    float4 baseColor;
    float4 normalWS;
    float4 mask;
    float3 emissive;
    float2 MAOSBlend;
};

// Generated from UnityEngine.Rendering.HighDefinition.DecalData
// PackingRules = Exact
struct DecalData
{
    float4x4 worldToDecal;
    float4x4 normalToWorld;
    float4 diffuseScaleBias;
    float4 normalScaleBias;
    float4 maskScaleBias;
    float4 baseColor;
    float4 remappingAOS;
    float4 scalingMBAndAngle;
    float3 blendParams;
    uint decalLayerMask;
};


#endif
