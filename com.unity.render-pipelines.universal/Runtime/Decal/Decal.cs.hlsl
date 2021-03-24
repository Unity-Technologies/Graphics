//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef DECAL_CS_HLSL
#define DECAL_CS_HLSL
//
// UnityEngine.Rendering.Universal.Decal+DBufferMaterial:  static fields
//
#define DBUFFERMATERIAL_COUNT (4)

// Generated from UnityEngine.Rendering.Universal.DecalData
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
    float4 scalingBAndRemappingM;
    float3 blendParams;
    uint decalLayerMask;
};

// Generated from UnityEngine.Rendering.Universal.Decal+DecalSurfaceData
// PackingRules = Exact
struct DecalSurfaceData
{
    real4 baseColor;
    real4 normalWS;
    real4 mask;
    real3 emissive;
    real2 MAOSBlend;
};


#endif
