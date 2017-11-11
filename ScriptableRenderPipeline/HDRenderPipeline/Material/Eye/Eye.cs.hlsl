//
// This file was automatically generated from Assets/ScriptableRenderPipeline/HDRenderPipeline/Material/Eye/Eye.cs.  Please don't edit by hand.
//

#ifndef EYE_CS_HLSL
#define EYE_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+MaterialId:  static fields
//
#define MATERIALID_LIT_SSS (0)
#define MATERIALID_LIT_STANDARD (1)
#define MATERIALID_LIT_UNUSED0 (2)
#define MATERIALID_LIT_UNUSED1 (3)
#define MATERIALID_LIT_ANISO (4)
#define MATERIALID_LIT_SPECULAR (5)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_LIT_SSS (4096)
#define MATERIALFEATUREFLAGS_LIT_STANDARD (8192)
#define MATERIALFEATUREFLAGS_LIT_ANISO (16384)
#define MATERIALFEATUREFLAGS_LIT_SPECULAR (32768)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+SurfaceData:  static fields
//
#define DEBUGVIEW_EYE_SURFACEDATA_BASE_COLOR (1000)
#define DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_OCCLUSION (1001)
#define DEBUGVIEW_EYE_SURFACEDATA_NORMAL_WS (1002)
#define DEBUGVIEW_EYE_SURFACEDATA_PERCEPTUAL_SMOOTHNESS (1003)
#define DEBUGVIEW_EYE_SURFACEDATA_MATERIAL_ID (1004)
#define DEBUGVIEW_EYE_SURFACEDATA_AMBIENT_OCCLUSION (1005)
#define DEBUGVIEW_EYE_SURFACEDATA_TANGENT_WS (1006)
#define DEBUGVIEW_EYE_SURFACEDATA_ANISOTROPY (1007)
#define DEBUGVIEW_EYE_SURFACEDATA_SPECULAR (1008)
#define DEBUGVIEW_EYE_SURFACEDATA_SUBSURFACE_RADIUS (1009)
#define DEBUGVIEW_EYE_SURFACEDATA_THICKNESS (1010)
#define DEBUGVIEW_EYE_SURFACEDATA_SUBSURFACE_PROFILE (1011)
#define DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_COLOR (1012)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+TransmissionType:  static fields
//
#define TRANSMISSIONTYPE_NONE (0)
#define TRANSMISSIONTYPE_REGULAR (1)
#define TRANSMISSIONTYPE_THIN_OBJECT (2)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+BSDFData:  static fields
//
#define DEBUGVIEW_EYE_BSDFDATA_DIFFUSE_COLOR (1030)
#define DEBUGVIEW_EYE_BSDFDATA_FRESNEL0 (1031)
#define DEBUGVIEW_EYE_BSDFDATA_SPECULAR_OCCLUSION (1032)
#define DEBUGVIEW_EYE_BSDFDATA_NORMAL_WS (1033)
#define DEBUGVIEW_EYE_BSDFDATA_PERCEPTUAL_ROUGHNESS (1034)
#define DEBUGVIEW_EYE_BSDFDATA_ROUGHNESS (1035)
#define DEBUGVIEW_EYE_BSDFDATA_MATERIAL_ID (1036)
#define DEBUGVIEW_EYE_BSDFDATA_TANGENT_WS (1037)
#define DEBUGVIEW_EYE_BSDFDATA_BITANGENT_WS (1038)
#define DEBUGVIEW_EYE_BSDFDATA_ROUGHNESS_T (1039)
#define DEBUGVIEW_EYE_BSDFDATA_ROUGHNESS_B (1040)
#define DEBUGVIEW_EYE_BSDFDATA_ANISOTROPY (1041)
#define DEBUGVIEW_EYE_BSDFDATA_SUBSURFACE_RADIUS (1042)
#define DEBUGVIEW_EYE_BSDFDATA_THICKNESS (1043)
#define DEBUGVIEW_EYE_BSDFDATA_SUBSURFACE_PROFILE (1044)
#define DEBUGVIEW_EYE_BSDFDATA_ENABLE_TRANSMISSION (1045)
#define DEBUGVIEW_EYE_BSDFDATA_USE_THIN_OBJECT_MODE (1046)
#define DEBUGVIEW_EYE_BSDFDATA_TRANSMITTANCE (1047)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+GBufferMaterial:  static fields
//
#define GBUFFERMATERIAL_COUNT (4)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Eye+SurfaceData
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
    float specular;
    float subsurfaceRadius;
    float thickness;
    int subsurfaceProfile;
    float3 specularColor;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Eye+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 diffuseColor;
    float3 fresnel0;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float roughness;
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
    bool useThinObjectMode;
    float3 transmittance;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_EYE_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_NORMAL_WS:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_PERCEPTUAL_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_MATERIAL_ID:
            result = GetIndexColor(surfacedata.materialId);
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_TANGENT_WS:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SPECULAR:
            result = surfacedata.specular.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SUBSURFACE_RADIUS:
            result = surfacedata.subsurfaceRadius.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_THICKNESS:
            result = surfacedata.thickness.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SUBSURFACE_PROFILE:
            result = GetIndexColor(surfacedata.subsurfaceProfile);
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
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
        case DEBUGVIEW_EYE_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_ROUGHNESS:
            result = bsdfdata.roughness.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_MATERIAL_ID:
            result = GetIndexColor(bsdfdata.materialId);
            break;
        case DEBUGVIEW_EYE_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_BITANGENT_WS:
            result = bsdfdata.bitangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_ROUGHNESS_T:
            result = bsdfdata.roughnessT.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_ROUGHNESS_B:
            result = bsdfdata.roughnessB.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_SUBSURFACE_RADIUS:
            result = bsdfdata.subsurfaceRadius.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_THICKNESS:
            result = bsdfdata.thickness.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_SUBSURFACE_PROFILE:
            result = GetIndexColor(bsdfdata.subsurfaceProfile);
            break;
        case DEBUGVIEW_EYE_BSDFDATA_ENABLE_TRANSMISSION:
            result = (bsdfdata.enableTransmission) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_EYE_BSDFDATA_USE_THIN_OBJECT_MODE:
            result = (bsdfdata.useThinObjectMode) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_EYE_BSDFDATA_TRANSMITTANCE:
            result = bsdfdata.transmittance;
            break;
    }
}


#endif
