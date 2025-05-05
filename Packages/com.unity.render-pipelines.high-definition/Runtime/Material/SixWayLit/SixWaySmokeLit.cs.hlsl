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
// UnityEditor.VFX.HDRP.SixWaySmokeLit+BSDFData:  static fields
//
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_ABSORPTION_RANGE (1750)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_DIFFUSE_COLOR (1751)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_AMBIENT_OCCLUSION (1752)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_NORMAL_WS (1753)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_NORMAL_VIEW_SPACE (1754)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_TANGENT (1755)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_TANGENT_WORLD_SPACE (1756)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BITANGENT (1757)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BITANGENT_WORLD_SPACE (1758)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_RIG_RIGHT_TOP_BACK (1759)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_RIG_LEFT_BOTTOM_FRONT (1760)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING0 (1761)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING1 (1762)
#define DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING2 (1763)

//
// UnityEditor.VFX.HDRP.SixWaySmokeLit+SurfaceData:  static fields
//
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_ABSORPTION_RANGE (1700)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BASE_COLOR (1701)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_NORMAL (1702)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_NORMAL_WORLD_SPACE (1703)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_TANGENT (1704)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_TANGENT_WORLD_SPACE (1705)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BITANGENT (1706)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BITANGENT_WORLD_SPACE (1707)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_AMBIENT_OCCLUSION (1708)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_RIG_RIGHT_TOP_BACK (1709)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_RIG_LEFT_BOTTOM_FRONT (1710)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING0 (1711)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING1 (1712)
#define DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING2 (1713)

// Generated from UnityEditor.VFX.HDRP.SixWaySmokeLit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float absorptionRange;
    real4 diffuseColor;
    real ambientOcclusion;
    float3 normalWS;
    float4 tangentWS;
    float3 bitangentWS;
    real3 rightTopBack;
    real3 leftBottomFront;
    real4 bakeDiffuseLighting0;
    real4 bakeDiffuseLighting1;
    real4 bakeDiffuseLighting2;
};

// Generated from UnityEditor.VFX.HDRP.SixWaySmokeLit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float absorptionRange;
    real4 baseColor;
    float3 normalWS;
    float4 tangentWS;
    float3 bitangentWS;
    real ambientOcclusion;
    real3 rightTopBack;
    real3 leftBottomFront;
    real4 bakeDiffuseLighting0;
    real4 bakeDiffuseLighting1;
    real4 bakeDiffuseLighting2;
};

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_ABSORPTION_RANGE:
            result = bsdfdata.absorptionRange.xxx;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor.xyz;
            needLinearToSRGB = true;
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
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_TANGENT:
            result = bsdfdata.tangentWS.xyz;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_TANGENT_WORLD_SPACE:
            result = bsdfdata.tangentWS.xyz;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BITANGENT:
            result = bsdfdata.bitangentWS;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BITANGENT_WORLD_SPACE:
            result = bsdfdata.bitangentWS;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_RIG_RIGHT_TOP_BACK:
            result = bsdfdata.rightTopBack;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_RIG_LEFT_BOTTOM_FRONT:
            result = bsdfdata.leftBottomFront;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING0:
            result = bsdfdata.bakeDiffuseLighting0.xyz;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING1:
            result = bsdfdata.bakeDiffuseLighting1.xyz;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_BSDFDATA_BAKE_DIFFUSE_LIGHTING2:
            result = bsdfdata.bakeDiffuseLighting2.xyz;
            break;
    }
}

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_ABSORPTION_RANGE:
            result = surfacedata.absorptionRange.xxx;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor.xyz;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_NORMAL:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_NORMAL_WORLD_SPACE:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_TANGENT:
            result = surfacedata.tangentWS.xyz;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_TANGENT_WORLD_SPACE:
            result = surfacedata.tangentWS.xyz;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BITANGENT:
            result = surfacedata.bitangentWS;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BITANGENT_WORLD_SPACE:
            result = surfacedata.bitangentWS;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_RIG_RIGHT_TOP_BACK:
            result = surfacedata.rightTopBack;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_RIG_LEFT_BOTTOM_FRONT:
            result = surfacedata.leftBottomFront;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING0:
            result = surfacedata.bakeDiffuseLighting0.xyz;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING1:
            result = surfacedata.bakeDiffuseLighting1.xyz;
            break;
        case DEBUGVIEW_SIXWAYSMOKELIT_SURFACEDATA_BAKE_DIFFUSE_LIGHTING2:
            result = surfacedata.bakeDiffuseLighting2.xyz;
            break;
    }
}


#endif
