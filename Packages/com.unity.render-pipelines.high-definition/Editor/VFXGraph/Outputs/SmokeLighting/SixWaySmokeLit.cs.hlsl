//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SIXWAYSMOKELIT_CS_HLSL
#define SIXWAYSMOKELIT_CS_HLSL
//
// UnityEditor.VFX.HDRP.SixWaySmokeLit+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_LIT_SIX_WAY_SMOKE (1)

//
// UnityEditor.VFX.HDRP.SixWaySmokeLit+SurfaceData:  static fields
//
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_MATERIAL_FEATURES (1700)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BASE_COLOR (1701)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_NORMAL (1702)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_NORMAL_VIEW_SPACE (1703)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_TANGENT_WS (1704)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_AMBIENT_OCCLUSION (1705)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_GEOMETRIC_NORMAL (1706)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1707)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_RIG_RIGHT_TOP_BACK (1708)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_RIG_LEFT_BOTTOM_FRONT (1709)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING0 (1710)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING1 (1711)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING2 (1712)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BACK_BAKE_DIFFUSE_LIGHTING0 (1713)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BACK_BAKE_DIFFUSE_LIGHTING1 (1714)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BACK_BAKE_DIFFUSE_LIGHTING2 (1715)

//
// UnityEditor.VFX.HDRP.SixWaySmokeLit+BSDFData:  static fields
//
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_MATERIAL_FEATURES (1750)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_DIFFUSE_COLOR (1751)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_FRESNEL0 (1752)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_AMBIENT_OCCLUSION (1753)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_NORMAL_WS (1754)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_NORMAL_VIEW_SPACE (1755)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_TANGENT_WS (1756)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_GEOMETRIC_NORMAL (1757)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1758)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_RIG_RIGHT_TOP_BACK (1759)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_RIG_LEFT_BOTTOM_FRONT (1760)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING0 (1761)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING1 (1762)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING2 (1763)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BACK_BAKE_DIFFUSE_LIGHTING0 (1764)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BACK_BAKE_DIFFUSE_LIGHTING1 (1765)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BACK_BAKE_DIFFUSE_LIGHTING2 (1766)

// Generated from UnityEditor.VFX.HDRP.SixWaySmokeLit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    real3 baseColor;
    float3 normalWS;
    float3 tangentWS;
    real ambientOcclusion;
    real3 geomNormalWS;
    real3 rigRTBk;
    real3 rigLBtF;
    real3 bakeDiffuseLighting0;
    real3 bakeDiffuseLighting1;
    real3 bakeDiffuseLighting2;
    real3 backBakeDiffuseLighting0;
    real3 backBakeDiffuseLighting1;
    real3 backBakeDiffuseLighting2;
};

// Generated from UnityEditor.VFX.HDRP.SixWaySmokeLit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    real3 diffuseColor;
    real3 fresnel0;
    real ambientOcclusion;
    float3 normalWS;
    float3 tangentWS;
    real3 geomNormalWS;
    real3 rigRTBk;
    real3 rigLBtF;
    real3 bakeDiffuseLighting0;
    real3 bakeDiffuseLighting1;
    real3 bakeDiffuseLighting2;
    real3 backBakeDiffuseLighting0;
    real3 backBakeDiffuseLighting1;
    real3 backBakeDiffuseLighting2;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_NORMAL:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_TANGENT_WS:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_GEOMETRIC_NORMAL:
            result = IsNormalized(surfacedata.geomNormalWS)? surfacedata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.geomNormalWS)? surfacedata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_RIG_RIGHT_TOP_BACK:
            result = surfacedata.rigRTBk;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_RIG_LEFT_BOTTOM_FRONT:
            result = surfacedata.rigLBtF;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING0:
            result = surfacedata.bakeDiffuseLighting0;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING1:
            result = surfacedata.bakeDiffuseLighting1;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING2:
            result = surfacedata.bakeDiffuseLighting2;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BACK_BAKE_DIFFUSE_LIGHTING0:
            result = surfacedata.backBakeDiffuseLighting0;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BACK_BAKE_DIFFUSE_LIGHTING1:
            result = surfacedata.backBakeDiffuseLighting1;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BACK_BAKE_DIFFUSE_LIGHTING2:
            result = surfacedata.backBakeDiffuseLighting2;
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
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_AMBIENT_OCCLUSION:
            result = bsdfdata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_NORMAL_WS:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_GEOMETRIC_NORMAL:
            result = IsNormalized(bsdfdata.geomNormalWS)? bsdfdata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = IsNormalized(bsdfdata.geomNormalWS)? bsdfdata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_RIG_RIGHT_TOP_BACK:
            result = bsdfdata.rigRTBk;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_RIG_LEFT_BOTTOM_FRONT:
            result = bsdfdata.rigLBtF;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING0:
            result = bsdfdata.bakeDiffuseLighting0;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING1:
            result = bsdfdata.bakeDiffuseLighting1;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING2:
            result = bsdfdata.bakeDiffuseLighting2;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BACK_BAKE_DIFFUSE_LIGHTING0:
            result = bsdfdata.backBakeDiffuseLighting0;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BACK_BAKE_DIFFUSE_LIGHTING1:
            result = bsdfdata.backBakeDiffuseLighting1;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BACK_BAKE_DIFFUSE_LIGHTING2:
            result = bsdfdata.backBakeDiffuseLighting2;
            break;
    }
}


#endif
