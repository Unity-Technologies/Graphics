//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef STANDARDLIT_CS_HLSL
#define STANDARDLIT_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.StandardLit+StandardBSDFData
// PackingRules = Exact
struct StandardBSDFData
{
    float3 baseColor;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float3 fresnel0;
    float coatMask;
    float3 emissiveAndBaked;
    uint renderingLayers;
    float4 shadowMasks;
    uint isUnlit;
};

//
// Accessors for UnityEngine.Rendering.HighDefinition.StandardLit+StandardBSDFData
//
float3 GetBaseColor(StandardBSDFData value)
{
    return value.baseColor;
}
float GetSpecularOcclusion(StandardBSDFData value)
{
    return value.specularOcclusion;
}
float3 GetNormalWS(StandardBSDFData value)
{
    return value.normalWS;
}
float GetPerceptualRoughness(StandardBSDFData value)
{
    return value.perceptualRoughness;
}
float3 GetFresnel0(StandardBSDFData value)
{
    return value.fresnel0;
}
float GetCoatMask(StandardBSDFData value)
{
    return value.coatMask;
}
float3 GetEmissiveAndBaked(StandardBSDFData value)
{
    return value.emissiveAndBaked;
}
uint GetRenderingLayers(StandardBSDFData value)
{
    return value.renderingLayers;
}
float4 GetShadowMasks(StandardBSDFData value)
{
    return value.shadowMasks;
}
uint GetIsUnlit(StandardBSDFData value)
{
    return value.isUnlit;
}

#endif
