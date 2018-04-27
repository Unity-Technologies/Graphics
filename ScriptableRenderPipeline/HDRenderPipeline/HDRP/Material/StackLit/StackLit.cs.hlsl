//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef STACKLIT_CS_HLSL
#define STACKLIT_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_LIT_STANDARD (1)

//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+SurfaceData:  static fields
//
#define DEBUGVIEW_STACKLIT_SURFACEDATA_MATERIAL_FEATURES (1300)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_BASE_COLOR (1301)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL (1302)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL_VIEW_SPACE (1303)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_A (1304)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_B (1305)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_LOBE_MIXING (1306)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_METALLIC (1307)

//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData:  static fields
//
#define DEBUGVIEW_STACKLIT_BSDFDATA_MATERIAL_FEATURES (1400)
#define DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSE_COLOR (1401)
#define DEBUGVIEW_STACKLIT_BSDFDATA_FRESNEL0 (1402)
#define DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_WS (1403)
#define DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE (1404)
#define DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_A (1405)
#define DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_B (1406)
#define DEBUGVIEW_STACKLIT_BSDFDATA_LOBE_MIX (1407)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AT (1408)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AB (1409)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BT (1410)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BB (1411)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ANISOTROPY (1412)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.StackLit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float3 normalWS;
    float perceptualSmoothnessA;
    float perceptualSmoothnessB;
    float lobeMix;
    float metallic;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float3 normalWS;
    float perceptualRoughnessA;
    float perceptualRoughnessB;
    float lobeMix;
    float roughnessAT;
    float roughnessAB;
    float roughnessBT;
    float roughnessBB;
    float anisotropy;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_STACKLIT_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_A:
            result = surfacedata.perceptualSmoothnessA.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_B:
            result = surfacedata.perceptualSmoothnessB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_LOBE_MIXING:
            result = surfacedata.lobeMix.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_METALLIC:
            result = surfacedata.metallic.xxx;
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_A:
            result = bsdfdata.perceptualRoughnessA.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_B:
            result = bsdfdata.perceptualRoughnessB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_LOBE_MIX:
            result = bsdfdata.lobeMix.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AT:
            result = bsdfdata.roughnessAT.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AB:
            result = bsdfdata.roughnessAB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BT:
            result = bsdfdata.roughnessBT.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BB:
            result = bsdfdata.roughnessBB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
    }
}


#endif
