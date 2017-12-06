//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIT_CS_HLSL
#define LIT_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+MaterialId:  static fields
//
#define MATERIALID_LIT_SSS (0)
#define MATERIALID_LIT_STANDARD (1)
#define MATERIALID_LIT_ANISO (2)
#define MATERIALID_LIT_CLEAR_COAT (3)
#define MATERIALID_LIT_SPECULAR (4)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_LIT_SSS (1)
#define MATERIALFEATUREFLAGS_LIT_STANDARD (2)
#define MATERIALFEATUREFLAGS_LIT_ANISO (4)
#define MATERIALFEATUREFLAGS_LIT_CLEAR_COAT (8)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+StandardDefinitions:  static fields
//
#define GBUFFER_LIT_STANDARD_REGULAR_ID (0)
#define GBUFFER_LIT_STANDARD_SPECULAR_COLOR_ID (1)
#define DEFAULT_SPECULAR_VALUE (0.04)
#define SKIN_SPECULAR_VALUE (0.028)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+RefractionMode:  static fields
//
#define REFRACTIONMODE_NONE (0)
#define REFRACTIONMODE_PLANE (1)
#define REFRACTIONMODE_SPHERE (2)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData:  static fields
//
#define DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR (1000)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION (1001)
#define DEBUGVIEW_LIT_SURFACEDATA_NORMAL_WS (1002)
#define DEBUGVIEW_LIT_SURFACEDATA_PERCEPTUAL_SMOOTHNESS (1003)
#define DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_ID (1004)
#define DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION (1005)
#define DEBUGVIEW_LIT_SURFACEDATA_TANGENT_WS (1006)
#define DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY (1007)
#define DEBUGVIEW_LIT_SURFACEDATA_METALLIC (1008)
#define DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_RADIUS (1009)
#define DEBUGVIEW_LIT_SURFACEDATA_THICKNESS (1010)
#define DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_PROFILE (1011)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR (1012)
#define DEBUGVIEW_LIT_SURFACEDATA_COAT_NORMAL_WS (1013)
#define DEBUGVIEW_LIT_SURFACEDATA_COAT_COVERAGE (1014)
#define DEBUGVIEW_LIT_SURFACEDATA_COAT_IOR (1015)
#define DEBUGVIEW_LIT_SURFACEDATA_IOR (1016)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_COLOR (1017)
#define DEBUGVIEW_LIT_SURFACEDATA_AT_DISTANCE (1018)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_MASK (1019)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+BSDFData:  static fields
//
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR (1030)
#define DEBUGVIEW_LIT_BSDFDATA_FRESNEL0 (1031)
#define DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION (1032)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS (1033)
#define DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS (1034)
#define DEBUGVIEW_LIT_BSDFDATA_MATERIAL_ID (1035)
#define DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS (1036)
#define DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS (1037)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T (1038)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B (1039)
#define DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY (1040)
#define DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_RADIUS (1041)
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS (1042)
#define DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_PROFILE (1043)
#define DEBUGVIEW_LIT_BSDFDATA_ENABLE_TRANSMISSION (1044)
#define DEBUGVIEW_LIT_BSDFDATA_USE_THICK_OBJECT_MODE (1045)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE (1046)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_NORMAL_WS (1047)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_COVERAGE (1048)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_IOR (1049)
#define DEBUGVIEW_LIT_BSDFDATA_IOR (1050)
#define DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT (1051)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK (1052)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+GBufferMaterial:  static fields
//
#define GBUFFERMATERIAL_COUNT (4)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 baseColor;
    float specularOcclusion;
    float3 normalWS;
    float perceptualSmoothness;
    int materialId;
    float ambientOcclusion;
    float3 tangentWS;
    float anisotropy;
    float metallic;
    float subsurfaceRadius;
    float thickness;
    int subsurfaceProfile;
    float3 specularColor;
    float3 coatNormalWS;
    float coatCoverage;
    float coatIOR;
    float ior;
    float3 transmittanceColor;
    float atDistance;
    float transmittanceMask;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 diffuseColor;
    float3 fresnel0;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    int materialId;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessT;
    float roughnessB;
    float anisotropy;
    float subsurfaceRadius;
    float thickness;
    int subsurfaceProfile;
    bool enableTransmission;
    bool useThickObjectMode;
    float3 transmittance;
    float3 coatNormalWS;
    float coatCoverage;
    float coatIOR;
    float ior;
    float3 absorptionCoefficient;
    float transmittanceMask;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_NORMAL_WS:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_PERCEPTUAL_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_ID:
            result = GetIndexColor(surfacedata.materialId);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TANGENT_WS:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_METALLIC:
            result = surfacedata.metallic.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_RADIUS:
            result = surfacedata.subsurfaceRadius.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_THICKNESS:
            result = surfacedata.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_PROFILE:
            result = GetIndexColor(surfacedata.subsurfaceProfile);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_COAT_NORMAL_WS:
            result = surfacedata.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_COAT_COVERAGE:
            result = surfacedata.coatCoverage.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_COAT_IOR:
            result = surfacedata.coatIOR.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_IOR:
            result = surfacedata.ior.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_COLOR:
            result = surfacedata.transmittanceColor;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_AT_DISTANCE:
            result = surfacedata.atDistance.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_MASK:
            result = surfacedata.transmittanceMask.xxx;
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
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_MATERIAL_ID:
            result = GetIndexColor(bsdfdata.materialId);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS:
            result = bsdfdata.bitangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T:
            result = bsdfdata.roughnessT.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B:
            result = bsdfdata.roughnessB.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_RADIUS:
            result = bsdfdata.subsurfaceRadius.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_THICKNESS:
            result = bsdfdata.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_PROFILE:
            result = GetIndexColor(bsdfdata.subsurfaceProfile);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ENABLE_TRANSMISSION:
            result = (bsdfdata.enableTransmission) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_USE_THICK_OBJECT_MODE:
            result = (bsdfdata.useThickObjectMode) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE:
            result = bsdfdata.transmittance;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_NORMAL_WS:
            result = bsdfdata.coatNormalWS;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_COVERAGE:
            result = bsdfdata.coatCoverage.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_IOR:
            result = bsdfdata.coatIOR.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IOR:
            result = bsdfdata.ior.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT:
            result = bsdfdata.absorptionCoefficient;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK:
            result = bsdfdata.transmittanceMask.xxx;
            break;
    }
}


#endif
