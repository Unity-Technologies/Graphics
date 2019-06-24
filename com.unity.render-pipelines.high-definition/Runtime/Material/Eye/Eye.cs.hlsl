//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef EYE_CS_HLSL
#define EYE_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_EYE_GAMES (1)
#define MATERIALFEATUREFLAGS_EYE_CINEMATICS (2)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Eye+SurfaceData:  static fields
//
#define DEBUGVIEW_EYE_SURFACEDATA_BASE_COLOR (1300)
#define DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_OCCLUSION (1301)
#define DEBUGVIEW_EYE_SURFACEDATA_NORMAL (1302)
#define DEBUGVIEW_EYE_SURFACEDATA_NORMAL_VIEW_SPACE (1303)
#define DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL (1304)
#define DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1305)
#define DEBUGVIEW_EYE_SURFACEDATA_IRIS_SMOOTHNESS (1306)
#define DEBUGVIEW_EYE_SURFACEDATA_SCLERA_SMOOTHNESS (1307)
#define DEBUGVIEW_EYE_SURFACEDATA_AMBIENT_OCCLUSION (1308)
#define DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_TINT (1309)
#define DEBUGVIEW_EYE_SURFACEDATA_CORNEA_HEIGHT (1310)

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
#define DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL (1357)
#define DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1358)
#define DEBUGVIEW_EYE_BSDFDATA_IRIS_ROUGHNESS (1359)
#define DEBUGVIEW_EYE_BSDFDATA_SCLERA_ROUGHNESS (1360)
#define DEBUGVIEW_EYE_BSDFDATA_CORNEA_HEIGHT (1361)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Eye+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 baseColor;
    float specularOcclusion;
    float3 normalWS;
    float3 geomNormalWS;
    float irisSmoothness;
    float scleraSmoothness;
    float ambientOcclusion;
    float3 specularColor;
    float corneaHeight;
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
    float3 geomNormalWS;
    float irisRoughness;
    float scleraRoughness;
    float corneaHeight;
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
        case DEBUGVIEW_EYE_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_IRIS_SMOOTHNESS:
            result = surfacedata.irisSmoothness.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SCLERA_SMOOTHNESS:
            result = surfacedata.scleraSmoothness.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_SPECULAR_TINT:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_EYE_SURFACEDATA_CORNEA_HEIGHT:
            result = surfacedata.corneaHeight.xxx;
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
        case DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL:
            result = bsdfdata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = bsdfdata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_IRIS_ROUGHNESS:
            result = bsdfdata.irisRoughness.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_SCLERA_ROUGHNESS:
            result = bsdfdata.scleraRoughness.xxx;
            break;
        case DEBUGVIEW_EYE_BSDFDATA_CORNEA_HEIGHT:
            result = bsdfdata.corneaHeight.xxx;
            break;
    }
}


#endif
