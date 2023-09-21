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
// UnityEngine.Rendering.HighDefinition.Water+BSDFData:  static fields
//
#define DEBUGVIEW_WATER_BSDFDATA_DIFFUSE_COLOR (1650)
#define DEBUGVIEW_WATER_BSDFDATA_FRESNEL0 (1651)
#define DEBUGVIEW_WATER_BSDFDATA_NORMAL_WS (1652)
#define DEBUGVIEW_WATER_BSDFDATA_NORMAL_VIEW_SPACE (1653)
#define DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_WS (1654)
#define DEBUGVIEW_WATER_BSDFDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE (1655)
#define DEBUGVIEW_WATER_BSDFDATA_PERCEPTUAL_ROUGHNESS (1656)
#define DEBUGVIEW_WATER_BSDFDATA_ROUGHNESS (1657)
#define DEBUGVIEW_WATER_BSDFDATA_CAUSTICS (1658)
#define DEBUGVIEW_WATER_BSDFDATA_FOAM (1659)
#define DEBUGVIEW_WATER_BSDFDATA_FOAM_COLOR (1660)
#define DEBUGVIEW_WATER_BSDFDATA_TIP_THICKNESS (1661)
#define DEBUGVIEW_WATER_BSDFDATA_FRONT_FACE (1662)
#define DEBUGVIEW_WATER_BSDFDATA_SURFACE_INDEX (1663)

//
// UnityEngine.Rendering.HighDefinition.Water+SurfaceData:  static fields
//
#define DEBUGVIEW_WATER_SURFACEDATA_BASE_COLOR (1600)
#define DEBUGVIEW_WATER_SURFACEDATA_NORMAL_WS (1601)
#define DEBUGVIEW_WATER_SURFACEDATA_NORMAL_VIEW_SPACE (1602)
#define DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_WS (1603)
#define DEBUGVIEW_WATER_SURFACEDATA_LOW_FREQUENCY_NORMAL_VIEW_SPACE (1604)
#define DEBUGVIEW_WATER_SURFACEDATA_SMOOTHNESS (1605)
#define DEBUGVIEW_WATER_SURFACEDATA_FOAM (1606)
#define DEBUGVIEW_WATER_SURFACEDATA_TIP_THICKNESS (1607)
#define DEBUGVIEW_WATER_SURFACEDATA_CAUSTICS (1608)
#define DEBUGVIEW_WATER_SURFACEDATA_REFRACTED_POSITION_WS (1609)

// Generated from UnityEngine.Rendering.HighDefinition.Water+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 diffuseColor;
    float3 fresnel0;
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float perceptualRoughness;
    float roughness;
    float caustics;
    float foam;
    float3 foamColor;
    float tipThickness;
    uint frontFace;
    uint surfaceIndex;
};

// Generated from UnityEngine.Rendering.HighDefinition.Water+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 baseColor;
    float3 normalWS;
    float3 lowFrequencyNormalWS;
    float perceptualSmoothness;
    float foam;
    float tipThickness;
    float caustics;
    float3 refractedPositionWS;
};

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_WATER_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
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
        case DEBUGVIEW_WATER_BSDFDATA_CAUSTICS:
            result = bsdfdata.caustics.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_FOAM:
            result = bsdfdata.foam.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_FOAM_COLOR:
            result = bsdfdata.foamColor;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_TIP_THICKNESS:
            result = bsdfdata.tipThickness.xxx;
            break;
        case DEBUGVIEW_WATER_BSDFDATA_FRONT_FACE:
            result = GetIndexColor(bsdfdata.frontFace);
            break;
        case DEBUGVIEW_WATER_BSDFDATA_SURFACE_INDEX:
            result = GetIndexColor(bsdfdata.surfaceIndex);
            break;
    }
}

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
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
        case DEBUGVIEW_WATER_SURFACEDATA_FOAM:
            result = surfacedata.foam.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_TIP_THICKNESS:
            result = surfacedata.tipThickness.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_CAUSTICS:
            result = surfacedata.caustics.xxx;
            break;
        case DEBUGVIEW_WATER_SURFACEDATA_REFRACTED_POSITION_WS:
            result = surfacedata.refractedPositionWS;
            break;
    }
}


#endif
