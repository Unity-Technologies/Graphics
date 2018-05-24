//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef STACKLIT_CS_HLSL
#define STACKLIT_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_STACK_LIT_STANDARD (1)
#define MATERIALFEATUREFLAGS_STACK_LIT_DUAL_SPECULAR_LOBE (2)
#define MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY (4)
#define MATERIALFEATUREFLAGS_STACK_LIT_COAT (8)
#define MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE (16)
#define MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING (32)
#define MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION (64)
#define MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP (128)

//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+SurfaceData:  static fields
//
#define DEBUGVIEW_STACKLIT_SURFACEDATA_MATERIAL_FEATURES (1300)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_BASE_COLOR (1301)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_AMBIENT_OCCLUSION (1302)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_METALLIC (1303)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IOR (1304)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL (1305)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL_VIEW_SPACE (1306)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL (1307)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1308)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_NORMAL (1309)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_NORMAL_VIEW_SPACE (1310)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_AVERAGE_NORMAL_LENGTH_A (1311)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_AVERAGE_NORMAL_LENGTH_B (1312)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_A (1313)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_B (1314)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_LOBE_MIXING (1315)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_TANGENT (1316)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_ANISOTROPY (1317)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_IOR (1318)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_THICKNESS (1319)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_SMOOTHNESS (1320)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_IOR (1321)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_THICKNESS (1322)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_EXTINCTION_COEFFICIENT (1323)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_DIFFUSION_PROFILE (1324)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SUBSURFACE_MASK (1325)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_THICKNESS (1326)

//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData:  static fields
//
#define DEBUGVIEW_STACKLIT_BSDFDATA_MATERIAL_FEATURES (1400)
#define DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSE_COLOR (1401)
#define DEBUGVIEW_STACKLIT_BSDFDATA_FRESNEL0 (1402)
#define DEBUGVIEW_STACKLIT_BSDFDATA_AMBIENT_OCCLUSION (1403)
#define DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_WS (1404)
#define DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE (1405)
#define DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL (1406)
#define DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1407)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_NORMAL (1408)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_NORMAL_VIEW_SPACE (1409)
#define DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_A (1410)
#define DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_B (1411)
#define DEBUGVIEW_STACKLIT_BSDFDATA_LOBE_MIX (1412)
#define DEBUGVIEW_STACKLIT_BSDFDATA_TANGENT_WS (1413)
#define DEBUGVIEW_STACKLIT_BSDFDATA_BITANGENT_WS (1414)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AT (1415)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AB (1416)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BT (1417)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BB (1418)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ANISOTROPY (1419)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_ROUGHNESS (1420)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_PERCEPTUAL_ROUGHNESS (1421)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_IOR (1422)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_THICKNESS (1423)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_EXTINCTION (1424)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_IOR (1425)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_THICKNESS (1426)
#define DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSION_PROFILE (1427)
#define DEBUGVIEW_STACKLIT_BSDFDATA_SUBSURFACE_MASK (1428)
#define DEBUGVIEW_STACKLIT_BSDFDATA_THICKNESS (1429)
#define DEBUGVIEW_STACKLIT_BSDFDATA_USE_THICK_OBJECT_MODE (1430)
#define DEBUGVIEW_STACKLIT_BSDFDATA_TRANSMITTANCE (1431)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.StackLit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float ambientOcclusion;
    float metallic;
    float dielectricIor;
    float3 normalWS;
    float3 geomNormalWS;
    float3 coatNormalWS;
    float averageNormalLengthA;
    float averageNormalLengthB;
    float perceptualSmoothnessA;
    float perceptualSmoothnessB;
    float lobeMix;
    float3 tangentWS;
    float anisotropy;
    float iridescenceIor;
    float iridescenceThickness;
    float coatPerceptualSmoothness;
    float coatIor;
    float coatThickness;
    float3 coatExtinction;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float3 normalWS;
    float3 geomNormalWS;
    float3 coatNormalWS;
    float perceptualRoughnessA;
    float perceptualRoughnessB;
    float lobeMix;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessAT;
    float roughnessAB;
    float roughnessBT;
    float roughnessBB;
    float anisotropy;
    float coatRoughness;
    float coatPerceptualRoughness;
    float coatIor;
    float coatThickness;
    float3 coatExtinction;
    float iridescenceIor;
    float iridescenceThickness;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
    bool useThickObjectMode;
    float3 transmittance;
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
        case DEBUGVIEW_STACKLIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_METALLIC:
            result = surfacedata.metallic.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IOR:
            result = surfacedata.dielectricIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_NORMAL:
            result = surfacedata.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_NORMAL_VIEW_SPACE:
            result = surfacedata.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_AVERAGE_NORMAL_LENGTH_A:
            result = surfacedata.averageNormalLengthA.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_AVERAGE_NORMAL_LENGTH_B:
            result = surfacedata.averageNormalLengthB.xxx;
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
        case DEBUGVIEW_STACKLIT_SURFACEDATA_TANGENT:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_IOR:
            result = surfacedata.iridescenceIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_THICKNESS:
            result = surfacedata.iridescenceThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_SMOOTHNESS:
            result = surfacedata.coatPerceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_IOR:
            result = surfacedata.coatIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_THICKNESS:
            result = surfacedata.coatThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_EXTINCTION_COEFFICIENT:
            result = surfacedata.coatExtinction;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(surfacedata.diffusionProfile);
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SUBSURFACE_MASK:
            result = surfacedata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_THICKNESS:
            result = surfacedata.thickness.xxx;
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_AMBIENT_OCCLUSION:
            result = bsdfdata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL:
            result = bsdfdata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = bsdfdata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_NORMAL:
            result = bsdfdata.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_NORMAL_VIEW_SPACE:
            result = bsdfdata.coatNormalWS * 0.5 + 0.5;
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_BITANGENT_WS:
            result = bsdfdata.bitangentWS * 0.5 + 0.5;
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_ROUGHNESS:
            result = bsdfdata.coatRoughness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.coatPerceptualRoughness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_IOR:
            result = bsdfdata.coatIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_THICKNESS:
            result = bsdfdata.coatThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_EXTINCTION:
            result = bsdfdata.coatExtinction;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_IOR:
            result = bsdfdata.iridescenceIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_THICKNESS:
            result = bsdfdata.iridescenceThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(bsdfdata.diffusionProfile);
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_SUBSURFACE_MASK:
            result = bsdfdata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_THICKNESS:
            result = bsdfdata.thickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_USE_THICK_OBJECT_MODE:
            result = (bsdfdata.useThickObjectMode) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_TRANSMITTANCE:
            result = bsdfdata.transmittance;
            break;
    }
}


#endif
