//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef EYE_CS_HLSL
#define EYE_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_EYE_CINEMATIC (1)
#define MATERIALFEATUREFLAGS_EYE_SUBSURFACE_SCATTERING (2)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+SurfaceData:  static fields
//
#define DEBUGVIEW_EYE_SURFACEDATA_MATERIAL_FEATURES (1300)
#define DEBUGVIEW_EYE_SURFACEDATA_BASE_COLOR (1301)
#define DEBUGVIEW_EYE_SURFACEDATA_NORMAL (1302)
#define DEBUGVIEW_EYE_SURFACEDATA_CORNEA_NORMAL_VIEW_SPACE (1303)
#define DEBUGVIEW_EYE_SURFACEDATA_IRIS_NORMAL (1304)
#define DEBUGVIEW_EYE_SURFACEDATA__IRIS_NORMAL_VIEW_SPACE (1305)
#define DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL (1306)
#define DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1307)
#define DEBUGVIEW_EYE_SURFACEDATA_SCLERA_SMOOTHNESS (1308)
#define DEBUGVIEW_EYE_SURFACEDATA_CORNEA_SMOOTHNESS (1309)
#define DEBUGVIEW_EYE_SURFACEDATA_AMBIENT_OCCLUSION (1310)
#define DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_OCCLUSION (1311)
#define DEBUGVIEW_EYE_SURFACEDATA_SCLERA_IOR (1312)
#define DEBUGVIEW_EYE_SURFACEDATA_CORNEA_IOR (1313)
#define DEBUGVIEW_EYE_SURFACEDATA_MASK (1314)
#define DEBUGVIEW_EYE_SURFACEDATA_DIFFUSION_PROFILE_HASH (1315)
#define DEBUGVIEW_EYE_SURFACEDATA_SUBSURFACE_MASK (1316)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+BSDFData:  static fields
//
#define DEBUGVIEW_EYE_BSDFDATA_MATERIAL_FEATURES (1350)
#define DEBUGVIEW_EYE_BSDFDATA_DIFFUSE_COLOR (1351)
#define DEBUGVIEW_EYE_BSDFDATA_FRESNEL0 (1352)
#define DEBUGVIEW_EYE_BSDFDATA_AMBIENT_OCCLUSION (1353)
#define DEBUGVIEW_EYE_BSDFDATA_SPECULAR_OCCLUSION (1354)
#define DEBUGVIEW_EYE_BSDFDATA_NORMAL_WS (1355)
#define DEBUGVIEW_EYE_BSDFDATA_NORMAL_VIEW_SPACE (1356)
#define DEBUGVIEW_EYE_BSDFDATA_DIFFUSE_NORMAL_WS (1357)
#define DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL (1358)
#define DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1359)
#define DEBUGVIEW_EYE_BSDFDATA_PERCEPTUAL_ROUGHNESS (1360)
#define DEBUGVIEW_EYE_BSDFDATA_MASK (1361)
#define DEBUGVIEW_EYE_BSDFDATA_DIFFUSION_PROFILE_INDEX (1362)
#define DEBUGVIEW_EYE_BSDFDATA_SUBSURFACE_MASK (1363)
#define DEBUGVIEW_EYE_BSDFDATA_ROUGHNESS (1364)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Eye+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float3 normalWS;
    float3 irisNormalWS;
    float3 geomNormalWS;
    float perceptualSmoothness;
    float corneaPerceptualSmoothness;
    float ambientOcclusion;
    float specularOcclusion;
    float scleraIOR;
    float corneaIOR;
    float3 mask;
    uint diffusionProfileHash;
    float subsurfaceMask;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Eye+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float3 diffuseNormalWS;
    float3 geomNormalWS;
    float perceptualRoughness;
    float3 mask;
    uint diffusionProfileIndex;
    float subsurfaceMask;
    float roughness;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_EYE_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_CORNEA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_IRIS_NORMAL:
            result = surfacedata.irisNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA__IRIS_NORMAL_VIEW_SPACE:
            result = surfacedata.irisNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SCLERA_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_CORNEA_SMOOTHNESS:
            result = surfacedata.corneaPerceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SCLERA_IOR:
            result = surfacedata.scleraIOR.xxx;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_CORNEA_IOR:
            result = surfacedata.corneaIOR.xxx;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_MASK:
            result = surfacedata.mask;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_DIFFUSION_PROFILE_HASH:
            result = GetIndexColor(surfacedata.diffusionProfileHash);
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SUBSURFACE_MASK:
            result = surfacedata.subsurfaceMask.xxx;
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
        case DEBUGVIEW_EYE_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
        case DEBUGVIEW_EYE_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_AMBIENT_OCCLUSION:
            result = bsdfdata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_NORMAL_VIEW_SPACE:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_DIFFUSE_NORMAL_WS:
            result = bsdfdata.diffuseNormalWS;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL:
            result = bsdfdata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = bsdfdata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_MASK:
            result = bsdfdata.mask;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_DIFFUSION_PROFILE_INDEX:
            result = GetIndexColor(bsdfdata.diffusionProfileIndex);
            break;
        case DEBUGVIEW_EYE_BSDFDATA_SUBSURFACE_MASK:
            result = bsdfdata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_ROUGHNESS:
            result = bsdfdata.roughness.xxx;
            break;
    }
}


#endif
