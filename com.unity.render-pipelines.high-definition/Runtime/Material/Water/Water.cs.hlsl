//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef WATER_CS_HLSL
#define WATER_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.Water+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_WATER_STANDARD (1)
#define MATERIALFEATUREFLAGS_WATER_CINEMATIC (2)

//
// UnityEngine.Rendering.HighDefinition.Water+SurfaceData:  static fields
//
#define DEBUGVIEW_WATER_SURFACEDATA_MATERIAL_FEATURES (1600)
#define DEBUGVIEW_WATER_SURFACEDATA_BASE_COLOR (1601)
#define DEBUGVIEW_WATER_SURFACEDATA_NORMAL_WS (1602)
#define DEBUGVIEW_WATER_SURFACEDATA_NORMAL_VIEW_SPACE (1603)
#define DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_WS (1604)
#define DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE (1605)
#define DEBUGVIEW_WATER_SURFACEDATA_SMOOTHNESS (1606)
#define DEBUGVIEW_WATER_SURFACEDATA_FOAM_COLOR (1607)
#define DEBUGVIEW_WATER_SURFACEDATA_SPECULAR_SELF_OCCLUSION (1608)
#define DEBUGVIEW_WATER_SURFACEDATA_TIP_THICKNESS (1609)
#define DEBUGVIEW_WATER_SURFACEDATA_REFRACTION_COLOR (1610)

//
// UnityEngine.Rendering.HighDefinition.Water+BSDFData:  static fields
//
#define DEBUGVIEW_WATER_BSDFDATA_MATERIAL_FEATURES (1650)
#define DEBUGVIEW_WATER_BSDFDATA_DIFFUSE_COLOR (1651)
#define DEBUGVIEW_WATER_BSDFDATA_FRESNEL0 (1652)
#define DEBUGVIEW_WATER_BSDFDATA_SPECULAR_SELF_OCCLUSION (1653)
#define DEBUGVIEW_WATER_BSDFDATA_NORMAL_WS (1654)
#define DEBUGVIEW_WATER_BSDFDATA_NORMAL_VIEW_SPACE (1655)
#define DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_WS (1656)
#define DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE (1657)
#define DEBUGVIEW_WATER_BSDFDATA_PERCEPTUAL_ROUGHNESS (1658)
#define DEBUGVIEW_WATER_BSDFDATA_ROUGHNESS (1659)
#define DEBUGVIEW_WATER_BSDFDATA_REFRACTION_COLOR (1660)
#define DEBUGVIEW_WATER_BSDFDATA_FOAM_COLOR (1661)
#define DEBUGVIEW_WATER_BSDFDATA_TIP_THICKNESS (1662)

// Generated from UnityEngine.Rendering.HighDefinition.Water+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float perceptualSmoothness;
    float3 foamColor;
    float specularSelfOcclusion;
    float tipThickness;
    float3 refractionColor;
};

// Generated from UnityEngine.Rendering.HighDefinition.Water+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float specularSelfOcclusion;
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float perceptualRoughness;
    float roughness;
    float3 refractionColor;
    float3 foamColor;
    float tipThickness;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_WATER_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_NORMAL_WS:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_WS:
            result = IsNormalized(surfacedata.lowFrequencyNormalWS)? surfacedata.lowFrequencyNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.lowFrequencyNormalWS)? surfacedata.lowFrequencyNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_FOAM_COLOR:
            result = surfacedata.foamColor;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_SPECULAR_SELF_OCCLUSION:
            result = surfacedata.specularSelfOcclusion.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_TIP_THICKNESS:
            result = surfacedata.tipThickness.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_REFRACTION_COLOR:
            result = surfacedata.refractionColor;
            break;
    }
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_WATER_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
        case DEBUGVIEW_WATER_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_SPECULAR_SELF_OCCLUSION:
            result = bsdfdata.specularSelfOcclusion.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_NORMAL_WS:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_BSDFDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_WS:
            result = bsdfdata.lowFrequencyNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE:
            result = bsdfdata.lowFrequencyNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ROUGHNESS:
            result = bsdfdata.roughness.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_REFRACTION_COLOR:
            result = bsdfdata.refractionColor;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_FOAM_COLOR:
            result = bsdfdata.foamColor;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_TIP_THICKNESS:
            result = bsdfdata.tipThickness.xxx;
            break;
    }
}


#endif
