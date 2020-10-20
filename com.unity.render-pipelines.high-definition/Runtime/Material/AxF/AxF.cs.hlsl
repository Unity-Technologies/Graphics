//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef AXF_CS_HLSL
#define AXF_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.AxF+FeatureFlags:  static fields
//
#define FEATUREFLAGS_AXF_ANISOTROPY (1)
#define FEATUREFLAGS_AXF_CLEAR_COAT (2)
#define FEATUREFLAGS_AXF_CLEAR_COAT_REFRACTION (4)
#define FEATUREFLAGS_AXF_USE_HEIGHT_MAP (8)
#define FEATUREFLAGS_AXF_BRDFCOLOR_DIAGONAL_CLAMP (16)
#define FEATUREFLAGS_AXF_HONOR_MIN_ROUGHNESS (256)
#define FEATUREFLAGS_AXF_HONOR_MIN_ROUGHNESS_COAT (512)

//
// UnityEngine.Rendering.HighDefinition.AxF+SurfaceData:  static fields
//
#define DEBUGVIEW_AXF_SURFACEDATA_AMBIENT_OCCLUSION (1200)
#define DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_OCCLUSION (1201)
#define DEBUGVIEW_AXF_SURFACEDATA_NORMAL (1202)
#define DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE (1203)
#define DEBUGVIEW_AXF_SURFACEDATA_TANGENT (1204)
#define DEBUGVIEW_AXF_SURFACEDATA_DIFFUSE_COLOR (1205)
#define DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_COLOR (1206)
#define DEBUGVIEW_AXF_SURFACEDATA_FRESNEL_F0 (1207)
#define DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_LOBE (1208)
#define DEBUGVIEW_AXF_SURFACEDATA_HEIGHT (1209)
#define DEBUGVIEW_AXF_SURFACEDATA_ANISOTROPIC_ANGLE (1210)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_UV_(OR_PLANAR_ZY) (1211)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_PLANAR_XZ (1212)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_PLANAR_XY (1213)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP_(AND_FOR_PLANAR_ZY) (1214)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP_FOR_PLANAR_XZ (1215)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP_FOR_PLANAR_XY (1216)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_TRIPLANAR_WEIGHTS (1217)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDX_(AND_FOR_PLANAR_ZY) (1218)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDY_(AND_FOR_PLANAR_ZY) (1219)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDX_FOR_PLANAR_XZ (1220)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDY_FOR_PLANAR_XZ (1221)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDX_FOR_PLANAR_XY (1222)
#define DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDY_FOR_PLANAR_XY (1223)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_COLOR (1224)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_NORMAL (1225)
#define DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_IOR (1226)
#define DEBUGVIEW_AXF_SURFACEDATA_GEOMETRIC_NORMAL (1227)
#define DEBUGVIEW_AXF_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1228)

//
// UnityEngine.Rendering.HighDefinition.AxF+BSDFData:  static fields
//
#define DEBUGVIEW_AXF_BSDFDATA_AMBIENT_OCCLUSION (1250)
#define DEBUGVIEW_AXF_BSDFDATA_SPECULAR_OCCLUSION (1251)
#define DEBUGVIEW_AXF_BSDFDATA_NORMAL_WS (1252)
#define DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE (1253)
#define DEBUGVIEW_AXF_BSDFDATA_TANGENT_WS (1254)
#define DEBUGVIEW_AXF_BSDFDATA_BI_TANGENT_WS (1255)
#define DEBUGVIEW_AXF_BSDFDATA_DIFFUSE_COLOR (1256)
#define DEBUGVIEW_AXF_BSDFDATA_SPECULAR_COLOR (1257)
#define DEBUGVIEW_AXF_BSDFDATA_FRESNEL_F0 (1258)
#define DEBUGVIEW_AXF_BSDFDATA_ROUGHNESS (1259)
#define DEBUGVIEW_AXF_BSDFDATA_HEIGHT_MM (1260)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_UVZY (1261)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_UVXZ (1262)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_UVXY (1263)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP_LEVEL_ZY (1264)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP_LEVEL_XZ (1265)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP_LEVEL_XY (1266)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_TRIPLANAR_WEIGHTS (1267)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDX_ZY (1268)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDY_ZY (1269)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDX_XZ (1270)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDY_XZ (1271)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDX_XY (1272)
#define DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDY_XY (1273)
#define DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_COLOR (1274)
#define DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_NORMAL_WS (1275)
#define DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_IOR (1276)
#define DEBUGVIEW_AXF_BSDFDATA_GEOMETRIC_NORMAL (1277)
#define DEBUGVIEW_AXF_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1278)

// Generated from UnityEngine.Rendering.HighDefinition.AxF+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float3 tangentWS;
    float3 diffuseColor;
    float3 specularColor;
    float3 fresnelF0;
    float3 specularLobe;
    float height_mm;
    float anisotropyAngle;
    float2 flakesUVZY;
    float2 flakesUVXZ;
    float2 flakesUVXY;
    float flakesMipLevelZY;
    float flakesMipLevelXZ;
    float flakesMipLevelXY;
    float3 flakesTriplanarWeights;
    float2 flakesDdxZY;
    float2 flakesDdyZY;
    float2 flakesDdxXZ;
    float2 flakesDdyXZ;
    float2 flakesDdxXY;
    float2 flakesDdyXY;
    float3 clearcoatColor;
    float3 clearcoatNormalWS;
    float clearcoatIOR;
    float3 geomNormalWS;
};

// Generated from UnityEngine.Rendering.HighDefinition.AxF+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float ambientOcclusion;
    float specularOcclusion;
    float3 normalWS;
    float3 tangentWS;
    float3 biTangentWS;
    float3 diffuseColor;
    float3 specularColor;
    float3 fresnelF0;
    float3 roughness;
    float height_mm;
    float2 flakesUVZY;
    float2 flakesUVXZ;
    float2 flakesUVXY;
    float flakesMipLevelZY;
    float flakesMipLevelXZ;
    float flakesMipLevelXY;
    float3 flakesTriplanarWeights;
    float2 flakesDdxZY;
    float2 flakesDdyZY;
    float2 flakesDdxXZ;
    float2 flakesDdyXZ;
    float2 flakesDdxXY;
    float2 flakesDdyXY;
    float3 clearcoatColor;
    float3 clearcoatNormalWS;
    float clearcoatIOR;
    float3 geomNormalWS;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_AXF_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_NORMAL:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.normalWS)? surfacedata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_TANGENT:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_DIFFUSE_COLOR:
            result = surfacedata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FRESNEL_F0:
            result = surfacedata.fresnelF0;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_SPECULAR_LOBE:
            result = surfacedata.specularLobe;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_HEIGHT:
            result = surfacedata.height_mm.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_ANISOTROPIC_ANGLE:
            result = surfacedata.anisotropyAngle.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_UV_(OR_PLANAR_ZY):
            result = float3(surfacedata.flakesUVZY, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_PLANAR_XZ:
            result = float3(surfacedata.flakesUVXZ, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_PLANAR_XY:
            result = float3(surfacedata.flakesUVXY, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP_(AND_FOR_PLANAR_ZY):
            result = surfacedata.flakesMipLevelZY.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP_FOR_PLANAR_XZ:
            result = surfacedata.flakesMipLevelXZ.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_MIP_FOR_PLANAR_XY:
            result = surfacedata.flakesMipLevelXY.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_TRIPLANAR_WEIGHTS:
            result = surfacedata.flakesTriplanarWeights;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDX_(AND_FOR_PLANAR_ZY):
            result = float3(surfacedata.flakesDdxZY, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDY_(AND_FOR_PLANAR_ZY):
            result = float3(surfacedata.flakesDdyZY, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDX_FOR_PLANAR_XZ:
            result = float3(surfacedata.flakesDdxXZ, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDY_FOR_PLANAR_XZ:
            result = float3(surfacedata.flakesDdyXZ, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDX_FOR_PLANAR_XY:
            result = float3(surfacedata.flakesDdxXY, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_FLAKES_DDY_FOR_PLANAR_XY:
            result = float3(surfacedata.flakesDdyXY, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_COLOR:
            result = surfacedata.clearcoatColor;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_NORMAL:
            result = surfacedata.clearcoatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_CLEARCOAT_IOR:
            result = surfacedata.clearcoatIOR.xxx;
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_GEOMETRIC_NORMAL:
            result = IsNormalized(surfacedata.geomNormalWS)? surfacedata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_AXF_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = IsNormalized(surfacedata.geomNormalWS)? surfacedata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
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
        case DEBUGVIEW_AXF_BSDFDATA_AMBIENT_OCCLUSION:
            result = bsdfdata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_NORMAL_WS:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_NORMAL_VIEW_SPACE:
            result = IsNormalized(bsdfdata.normalWS)? bsdfdata.normalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_BI_TANGENT_WS:
            result = bsdfdata.biTangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_SPECULAR_COLOR:
            result = bsdfdata.specularColor;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FRESNEL_F0:
            result = bsdfdata.fresnelF0;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_ROUGHNESS:
            result = bsdfdata.roughness;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_HEIGHT_MM:
            result = bsdfdata.height_mm.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_UVZY:
            result = float3(bsdfdata.flakesUVZY, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_UVXZ:
            result = float3(bsdfdata.flakesUVXZ, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_UVXY:
            result = float3(bsdfdata.flakesUVXY, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP_LEVEL_ZY:
            result = bsdfdata.flakesMipLevelZY.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP_LEVEL_XZ:
            result = bsdfdata.flakesMipLevelXZ.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_MIP_LEVEL_XY:
            result = bsdfdata.flakesMipLevelXY.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_TRIPLANAR_WEIGHTS:
            result = bsdfdata.flakesTriplanarWeights;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDX_ZY:
            result = float3(bsdfdata.flakesDdxZY, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDY_ZY:
            result = float3(bsdfdata.flakesDdyZY, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDX_XZ:
            result = float3(bsdfdata.flakesDdxXZ, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDY_XZ:
            result = float3(bsdfdata.flakesDdyXZ, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDX_XY:
            result = float3(bsdfdata.flakesDdxXY, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_FLAKES_DDY_XY:
            result = float3(bsdfdata.flakesDdyXY, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_COLOR:
            result = bsdfdata.clearcoatColor;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_NORMAL_WS:
            result = bsdfdata.clearcoatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_CLEARCOAT_IOR:
            result = bsdfdata.clearcoatIOR.xxx;
            break;
        case DEBUGVIEW_AXF_BSDFDATA_GEOMETRIC_NORMAL:
            result = IsNormalized(bsdfdata.geomNormalWS)? bsdfdata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_AXF_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = IsNormalized(bsdfdata.geomNormalWS)? bsdfdata.geomNormalWS * 0.5 + 0.5 : float3(1.0, 0.0, 0.0);
            break;
    }
}


#endif
