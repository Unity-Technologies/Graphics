//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef HAIR_CS_HLSL
#define HAIR_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.Hair+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_HAIR_KAJIYA_KAY (1)
#define MATERIALFEATUREFLAGS_HAIR_MARSCHNER (2)

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
#define DEBUGVIEW_HAIR_SURFACEDATA_ABSORPTION_COEFFICIENT (1417)
#define DEBUGVIEW_HAIR_SURFACEDATA_EUMELANIN (1418)
#define DEBUGVIEW_HAIR_SURFACEDATA_PHEOMELANIN (1419)
#define DEBUGVIEW_HAIR_SURFACEDATA_AZIMUTHAL_ROUGHNESS (1420)
#define DEBUGVIEW_HAIR_SURFACEDATA_CUTICLE_ANGLE (1421)
#define DEBUGVIEW_HAIR_SURFACEDATA_STRAND_COUNT_PROBE (1422)
#define DEBUGVIEW_HAIR_SURFACEDATA_STRAND_SHADOW_BIAS (1423)

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
#define DEBUGVIEW_HAIR_BSDFDATA_TANGENT_WS (1465)
#define DEBUGVIEW_HAIR_BSDFDATA_BITANGENT_WS (1466)
#define DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_T (1467)
#define DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_B (1468)
#define DEBUGVIEW_HAIR_BSDFDATA_H (1469)
#define DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_PERCEPTUAL_ROUGHNESS (1470)
#define DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_TINT (1471)
#define DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_EXPONENT (1472)
#define DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_EXPONENT (1473)
#define DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_SHIFT (1474)
#define DEBUGVIEW_HAIR_BSDFDATA_SECONDARY_SPECULAR_SHIFT (1475)
#define DEBUGVIEW_HAIR_BSDFDATA_ABSORPTION (1476)
#define DEBUGVIEW_HAIR_BSDFDATA_LIGHT_PATH_LENGTH (1477)
#define DEBUGVIEW_HAIR_BSDFDATA_CUTICLE_ANGLE (1478)
#define DEBUGVIEW_HAIR_BSDFDATA_CUTICLE_ANGLE_R (1479)
#define DEBUGVIEW_HAIR_BSDFDATA_CUTICLE_ANGLE_TT (1480)
#define DEBUGVIEW_HAIR_BSDFDATA_CUTICLE_ANGLE_TRT (1481)
#define DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_R (1482)
#define DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_TT (1483)
#define DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_TRT (1484)
#define DEBUGVIEW_HAIR_BSDFDATA_PERCEPTUAL_ROUGHNESS_RADIAL (1485)
#define DEBUGVIEW_HAIR_BSDFDATA_DISTRIBUTION_NORMALIZATION_FACTOR (1486)
#define DEBUGVIEW_HAIR_BSDFDATA_STRAND_COUNT_PROBE (1487)
#define DEBUGVIEW_HAIR_BSDFDATA_STRAND_SHADOW_BIAS (1488)
#define DEBUGVIEW_HAIR_BSDFDATA_SPLINE_VISIBILITY (1489)

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
    float3 absorption;
    float eumelanin;
    float pheomelanin;
    float perceptualRadialSmoothness;
    float cuticleAngle;
    float4 strandCountProbe;
    float strandShadowBias;
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
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessT;
    float roughnessB;
    float h;
    float secondaryPerceptualRoughness;
    float3 secondarySpecularTint;
    float specularExponent;
    float secondarySpecularExponent;
    float specularShift;
    float secondarySpecularShift;
    float3 absorption;
    float lightPathLength;
    float cuticleAngle;
    float cuticleAngleR;
    float cuticleAngleTT;
    float cuticleAngleTRT;
    float roughnessR;
    float roughnessTT;
    float roughnessTRT;
    float perceptualRoughnessRadial;
    float3 distributionNormalizationFactor;
    float4 strandCountProbe;
    float strandShadowBias;
    float splineVisibility;
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
        case DEBUGVIEW_HAIR_SURFACEDATA_ABSORPTION_COEFFICIENT:
            result = surfacedata.absorption;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_EUMELANIN:
            result = surfacedata.eumelanin.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_PHEOMELANIN:
            result = surfacedata.pheomelanin.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_AZIMUTHAL_ROUGHNESS:
            result = surfacedata.perceptualRadialSmoothness.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_CUTICLE_ANGLE:
            result = surfacedata.cuticleAngle.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_STRAND_COUNT_PROBE:
            result = surfacedata.strandCountProbe.xyz;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_STRAND_SHADOW_BIAS:
            result = surfacedata.strandShadowBias.xxx;
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
        case DEBUGVIEW_HAIR_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_BITANGENT_WS:
            result = bsdfdata.bitangentWS;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_T:
            result = bsdfdata.roughnessT.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_B:
            result = bsdfdata.roughnessB.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_H:
            result = bsdfdata.h.xxx;
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
        case DEBUGVIEW_HAIR_BSDFDATA_ABSORPTION:
            result = bsdfdata.absorption;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_LIGHT_PATH_LENGTH:
            result = bsdfdata.lightPathLength.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_CUTICLE_ANGLE:
            result = bsdfdata.cuticleAngle.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_CUTICLE_ANGLE_R:
            result = bsdfdata.cuticleAngleR.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_CUTICLE_ANGLE_TT:
            result = bsdfdata.cuticleAngleTT.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_CUTICLE_ANGLE_TRT:
            result = bsdfdata.cuticleAngleTRT.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_R:
            result = bsdfdata.roughnessR.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_TT:
            result = bsdfdata.roughnessTT.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_TRT:
            result = bsdfdata.roughnessTRT.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_PERCEPTUAL_ROUGHNESS_RADIAL:
            result = bsdfdata.perceptualRoughnessRadial.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_DISTRIBUTION_NORMALIZATION_FACTOR:
            result = bsdfdata.distributionNormalizationFactor;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_STRAND_COUNT_PROBE:
            result = bsdfdata.strandCountProbe.xyz;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_STRAND_SHADOW_BIAS:
            result = bsdfdata.strandShadowBias.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SPLINE_VISIBILITY:
            result = bsdfdata.splineVisibility.xxx;
            break;
    }
}


#endif
