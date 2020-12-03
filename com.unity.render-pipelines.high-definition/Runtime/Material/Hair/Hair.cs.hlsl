//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit / Render Pipeline / Generate Shader Includes ] instead
//

#ifndef HAIR_CS_HLSL
#define HAIR_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.Hair+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY (1)

//
// UnityEngine.Rendering.HighDefinition.Hair+SurfaceData:  static fields
//
#define DEBUGVIEW_HAIR_SURFACEDATA_MATERIAL_FEATURES (1400)
#define DEBUGVIEW_HAIR_SURFACEDATA_AMBIENT_OCCLUSION (1401)
#define DEBUGVIEW_HAIR_SURFACEDATA_DIFFUSE (1402)
#define DEBUGVIEW_HAIR_SURFACEDATA_SPECULAR_OCCLUSION (1403)
#define DEBUGVIEW_HAIR_SURFACEDATA_NORMAL (1404)
#define DEBUGVIEW_HAIR_SURFACEDATA_NORMAL_VIEW_SPACE (1405)
#define DEBUGVIEW_HAIR_SURFACEDATA_GEOMETRIC_NORMAL (1406)
#define DEBUGVIEW_HAIR_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1407)
#define DEBUGVIEW_HAIR_SURFACEDATA_SMOOTHNESS (1408)
#define DEBUGVIEW_HAIR_SURFACEDATA_TRANSMITTANCE (1409)
#define DEBUGVIEW_HAIR_SURFACEDATA_RIM_TRANSMISSION_INTENSITY (1410)
#define DEBUGVIEW_HAIR_SURFACEDATA_HAIR_STRAND_DIRECTION (1411)
#define DEBUGVIEW_HAIR_SURFACEDATA_SECONDARY_SMOOTHNESS (1412)
#define DEBUGVIEW_HAIR_SURFACEDATA_SPECULAR_TINT (1413)
#define DEBUGVIEW_HAIR_SURFACEDATA_SECONDARY_SPECULAR_TINT (1414)
#define DEBUGVIEW_HAIR_SURFACEDATA_SPECULAR_SHIFT (1415)
#define DEBUGVIEW_HAIR_SURFACEDATA_SECONDARY_SPECULAR_SHIFT (1416)

//
// UnityEngine.Rendering.HighDefinition.Hair+BSDFData:  static fields
//
#define DEBUGVIEW_HAIR_BSDFDATA_MATERIAL_FEATURES (1450)
#define DEBUGVIEW_HAIR_BSDFDATA_AMBIENT_OCCLUSION (1451)
#define DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_OCCLUSION (1452)
#define DEBUGVIEW_HAIR_BSDFDATA_DIFFUSE_COLOR (1453)
#define DEBUGVIEW_HAIR_BSDFDATA_FRESNEL0 (1454)
#define DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_TINT (1455)
#define DEBUGVIEW_HAIR_BSDFDATA_NORMAL_WS (1456)
#define DEBUGVIEW_HAIR_BSDFDATA_NORMAL_VIEW_SPACE (1457)
#define DEBUGVIEW_HAIR_BSDFDATA_GEOMETRIC_NORMAL (1458)
#define DEBUGVIEW_HAIR_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1459)
#define DEBUGVIEW_HAIR_BSDFDATA_PERCEPTUAL_ROUGHNESS (1460)
#define DEBUGVIEW_HAIR_BSDFDATA_TRANSMITTANCE (1461)
#define DEBUGVIEW_HAIR_BSDFDATA_RIM_TRANSMISSION_INTENSITY (1462)
#define DEBUGVIEW_HAIR_BSDFDATA_HAIR_STRAND_DIRECTION_WS (1463)
#define DEBUGVIEW_HAIR_BSDFDATA_ANISOTROPY (1464)
#define DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_PERCEPTUAL_ROUGHNESS (1465)
#define DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_TINT (1466)
#define DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_EXPONENT (1467)
#define DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_EXPONENT (1468)
#define DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_SHIFT (1469)
#define DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_SHIFT (1470)

// Generated from UnityEngine.Rendering.HighDefinition.Hair+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float ambientOcclusion;
    float3 diffuseColor;
    float specularOcclusion;
    float3 normalWS;
    float3 geomNormalWS;
    float perceptualSmoothness;
    float3 transmittance;
    float rimTransmissionIntensity;
    float3 hairStrandDirectionWS;
    float secondaryPerceptualSmoothness;
    float3 specularTint;
    float3 secondarySpecularTint;
    float specularShift;
    float secondarySpecularShift;
};

// Generated from UnityEngine.Rendering.HighDefinition.Hair+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float ambientOcclusion;
    float specularOcclusion;
    float3 diffuseColor;
    float3 fresnel0;
    float3 specularTint;
    float3 normalWS;
    float3 geomNormalWS;
    float perceptualRoughness;
    float3 transmittance;
    float rimTransmissionIntensity;
    float3 hairStrandDirectionWS;
    float anisotropy;
    float secondaryPerceptualRoughness;
    float3 secondarySpecularTint;
    float specularExponent;
    float secondarySpecularExponent;
    float specularShift;
    float secondarySpecularShift;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_HAIR_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_DIFFUSE:
            result = surfacedata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_NORMAL:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_GEOMETRIC_NORMAL:
            result = IsNormalized(surfacedata.geomNormalWS)? surfacedata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.geomNormalWS)? surfacedata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_TRANSMITTANCE:
            result = surfacedata.transmittance;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_RIM_TRANSMISSION_INTENSITY:
            result = surfacedata.rimTransmissionIntensity.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_HAIR_STRAND_DIRECTION:
            result = surfacedata.hairStrandDirectionWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_SECONDARY_SMOOTHNESS:
            result = surfacedata.secondaryPerceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_SPECULAR_TINT:
            result = surfacedata.specularTint;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_SECONDARY_SPECULAR_TINT:
            result = surfacedata.secondarySpecularTint;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_SPECULAR_SHIFT:
            result = surfacedata.specularShift.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_SECONDARY_SPECULAR_SHIFT:
            result = surfacedata.secondarySpecularShift.xxx;
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
        case DEBUGVIEW_HAIR_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_AMBIENT_OCCLUSION:
            result = bsdfdata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_TINT:
            result = bsdfdata.specularTint;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_NORMAL_WS:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_GEOMETRIC_NORMAL:
            result = IsNormalized(bsdfdata.geomNormalWS)? bsdfdata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = IsNormalized(bsdfdata.geomNormalWS)? bsdfdata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_TRANSMITTANCE:
            result = bsdfdata.transmittance;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_RIM_TRANSMISSION_INTENSITY:
            result = bsdfdata.rimTransmissionIntensity.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_HAIR_STRAND_DIRECTION_WS:
            result = bsdfdata.hairStrandDirectionWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.secondaryPerceptualRoughness.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_TINT:
            result = bsdfdata.secondarySpecularTint;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_EXPONENT:
            result = bsdfdata.specularExponent.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_EXPONENT:
            result = bsdfdata.secondarySpecularExponent.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_SHIFT:
            result = bsdfdata.specularShift.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_SHIFT:
            result = bsdfdata.secondarySpecularShift.xxx;
            break;
    }
}


#endif
