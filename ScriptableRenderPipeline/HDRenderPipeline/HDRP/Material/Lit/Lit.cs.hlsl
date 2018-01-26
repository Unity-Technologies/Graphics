//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIT_CS_HLSL
#define LIT_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_LIT_STANDARD (1)
#define MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR (2)
#define MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING (4)
#define MATERIALFEATUREFLAGS_LIT_TRANSMISSION (8)
#define MATERIALFEATUREFLAGS_LIT_ANISOTROPY (16)
#define MATERIALFEATUREFLAGS_LIT_IRIDESCENCE (32)
#define MATERIALFEATUREFLAGS_LIT_CLEAR_COAT (64)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+RefractionMode:  static fields
//
#define REFRACTIONMODE_NONE (0)
#define REFRACTIONMODE_PLANE (1)
#define REFRACTIONMODE_SPHERE (2)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData:  static fields
//
#define DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_FEATURES (1000)
#define DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR (1001)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION (1002)
#define DEBUGVIEW_LIT_SURFACEDATA_NORMAL_WS (1003)
#define DEBUGVIEW_LIT_SURFACEDATA_PERCEPTUAL_SMOOTHNESS (1004)
#define DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION (1005)
#define DEBUGVIEW_LIT_SURFACEDATA_METALLIC (1006)
#define DEBUGVIEW_LIT_SURFACEDATA_COAT_MASK (1007)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR (1008)
#define DEBUGVIEW_LIT_SURFACEDATA_DIFFUSION_PROFILE (1009)
#define DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_MASK (1010)
#define DEBUGVIEW_LIT_SURFACEDATA_THICKNESS (1011)
#define DEBUGVIEW_LIT_SURFACEDATA_TANGENT_WS (1012)
#define DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY (1013)
#define DEBUGVIEW_LIT_SURFACEDATA_THICKNESS_IRID (1014)
#define DEBUGVIEW_LIT_SURFACEDATA_IOR (1015)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_COLOR (1016)
#define DEBUGVIEW_LIT_SURFACEDATA_AT_DISTANCE (1017)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_MASK (1018)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+BSDFData:  static fields
//
#define DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES (1030)
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR (1031)
#define DEBUGVIEW_LIT_BSDFDATA_FRESNEL0 (1032)
#define DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION (1033)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS (1034)
#define DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS (1035)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_MASK (1036)
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSION_PROFILE (1037)
#define DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_MASK (1038)
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS (1039)
#define DEBUGVIEW_LIT_BSDFDATA_USE_THICK_OBJECT_MODE (1040)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE (1041)
#define DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS (1042)
#define DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS (1043)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T (1044)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B (1045)
#define DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY (1046)
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS_IRID (1047)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS (1048)
#define DEBUGVIEW_LIT_BSDFDATA_IOR (1049)
#define DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT (1050)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK (1051)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+GBufferMaterial:  static fields
//
#define GBUFFERMATERIAL_COUNT (4)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float specularOcclusion;
    float3 normalWS;
    float perceptualSmoothness;
    float ambientOcclusion;
    float metallic;
    float coatMask;
    float3 specularColor;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
    float3 tangentWS;
    float anisotropy;
    float thicknessIrid;
    float ior;
    float3 transmittanceColor;
    float atDistance;
    float transmittanceMask;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float coatMask;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
    bool useThickObjectMode;
    float3 transmittance;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessT;
    float roughnessB;
    float anisotropy;
    float thicknessIrid;
    float coatRoughness;
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
        case DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
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
        case DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_METALLIC:
            result = surfacedata.metallic.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_COAT_MASK:
            result = surfacedata.coatMask.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(surfacedata.diffusionProfile);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_MASK:
            result = surfacedata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_THICKNESS:
            result = surfacedata.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TANGENT_WS:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_THICKNESS_IRID:
            result = surfacedata.thicknessIrid.xxx;
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
        case DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
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
        case DEBUGVIEW_LIT_BSDFDATA_COAT_MASK:
            result = bsdfdata.coatMask.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(bsdfdata.diffusionProfile);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_MASK:
            result = bsdfdata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_THICKNESS:
            result = bsdfdata.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_USE_THICK_OBJECT_MODE:
            result = (bsdfdata.useThickObjectMode) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE:
            result = bsdfdata.transmittance;
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
        case DEBUGVIEW_LIT_BSDFDATA_THICKNESS_IRID:
            result = bsdfdata.thicknessIrid.xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS:
            result = bsdfdata.coatRoughness.xxx;
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
