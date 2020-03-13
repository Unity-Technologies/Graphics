//
// This file was automatically generated. Please don't edit by hand.
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
#define DEBUGVIEW_WATER_SURFACEDATA_MATERIAL_FEATURES (1500)
#define DEBUGVIEW_WATER_SURFACEDATA_BASE_COLOR (1501)
#define DEBUGVIEW_WATER_SURFACEDATA_NORMAL (1502)
#define DEBUGVIEW_WATER_SURFACEDATA_NORMAL_VIEW_SPACE (1503)
#define DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL (1504)
#define DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE (1505)
#define DEBUGVIEW_WATER_SURFACEDATA_GEOMETRIC_NORMAL (1506)
#define DEBUGVIEW_WATER_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1507)
#define DEBUGVIEW_WATER_SURFACEDATA_SMOOTHNESS (1508)
#define DEBUGVIEW_WATER_SURFACEDATA_SPECULAR_OCCLUSION (1509)
#define DEBUGVIEW_WATER_SURFACEDATA_SELF_OCCLUSION (1510)
#define DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY (1511)
#define DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY_OFFSET (1512)
#define DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY_IOR (1513)

//
// UnityEngine.Rendering.HighDefinition.Water+BSDFData:  static fields
//
#define DEBUGVIEW_WATER_BSDFDATA_MATERIAL_FEATURES (1550)
#define DEBUGVIEW_WATER_BSDFDATA_DIFFUSE_COLOR (1551)
#define DEBUGVIEW_WATER_BSDFDATA_FRESNEL0 (1552)
#define DEBUGVIEW_WATER_BSDFDATA_NORMAL_WS (1553)
#define DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_WS (1554)
#define DEBUGVIEW_WATER_BSDFDATA_GEOM_NORMAL_WS (1555)
#define DEBUGVIEW_WATER_BSDFDATA_PERCEPTUAL_ROUGHNESS (1556)
#define DEBUGVIEW_WATER_BSDFDATA_ROUGHNESS (1557)
#define DEBUGVIEW_WATER_BSDFDATA_SPECULAR_OCCLUSION (1558)
#define DEBUGVIEW_WATER_BSDFDATA_SELF_OCCLUSION (1559)
#define DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY (1560)
#define DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY_OFFSET (1561)
#define DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY_IOR (1562)

// Generated from UnityEngine.Rendering.HighDefinition.Water+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float3 geomNormalWS;
    float perceptualSmoothness;
    float specularOcclusion;
    float selfOcclusion;
    float anisotropy;
    float anisotropyWeight;
    float anisotropyIOR;
    float anisotropyOffset;
    float3 customRefractionColor;
};

// Generated from UnityEngine.Rendering.HighDefinition.Water+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float3 geomNormalWS;
    float perceptualRoughness;
    float roughness;
    float specularOcclusion;
    float selfOcclusion;
    float anisotropy;
    float anisotropyWeight;
    float anisotropyIOR;
    float anisotropyOffset;
    float3 customRefractionColor;
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
        case DEBUGVIEW_WATER_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL:
            result = surfacedata.lowFrequencyNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE:
            result = surfacedata.lowFrequencyNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_GEOMETRIC_NORMAL:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_SELF_OCCLUSION:
            result = surfacedata.selfOcclusion.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY_OFFSET:
            result = surfacedata.anisotropyWeight.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_ANISOTROPY_IOR:
            result = surfacedata.anisotropyIOR.xxx;
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
        case DEBUGVIEW_WATER_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_WS:
            result = bsdfdata.lowFrequencyNormalWS;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_GEOM_NORMAL_WS:
            result = bsdfdata.geomNormalWS;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ROUGHNESS:
            result = bsdfdata.roughness.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_SELF_OCCLUSION:
            result = bsdfdata.selfOcclusion.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY_OFFSET:
            result = bsdfdata.anisotropyWeight.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_ANISOTROPY_IOR:
            result = bsdfdata.anisotropyIOR.xxx;
            break;
    }
}


#endif
