//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/Material/Lit.cs.  Please don't edit by hand.
//

//
// UnityEngine.ScriptableRenderLoop.Lit.SurfaceData:  static fields
//
#define DEBUGVIEW_LIT_SURFACEDATA_BASECOLOR (1000)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAROCCLUSION (1001)
#define DEBUGVIEW_LIT_SURFACEDATA_NORMALWS (1002)
#define DEBUGVIEW_LIT_SURFACEDATA_PERCEPTUALSMOOTHNESS (1003)
#define DEBUGVIEW_LIT_SURFACEDATA_MATERIALID (1004)
#define DEBUGVIEW_LIT_SURFACEDATA_AMBIENTOCCLUSION (1005)
#define DEBUGVIEW_LIT_SURFACEDATA_TANGENTWS (1006)
#define DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY (1007)
#define DEBUGVIEW_LIT_SURFACEDATA_METALIC (1008)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR (1009)
#define DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACERADIUS (1010)
#define DEBUGVIEW_LIT_SURFACEDATA_THICKNESS (1011)
#define DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACEPROFILE (1012)
#define DEBUGVIEW_LIT_SURFACEDATA_COATNORMALWS (1013)
#define DEBUGVIEW_LIT_SURFACEDATA_COATPERCEPTUALSMOOTHNESS (1014)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULARCOLOR (1015)

//
// UnityEngine.ScriptableRenderLoop.Lit.BSDFData:  static fields
//
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSECOLOR (1030)
#define DEBUGVIEW_LIT_BSDFDATA_FRESNEL0 (1031)
#define DEBUGVIEW_LIT_BSDFDATA_SPECULAROCCLUSION (1032)
#define DEBUGVIEW_LIT_BSDFDATA_NORMALWS (1033)
#define DEBUGVIEW_LIT_BSDFDATA_PERCEPTUALROUGHNESS (1034)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS (1035)
#define DEBUGVIEW_LIT_BSDFDATA_MATERIALID (1036)
#define DEBUGVIEW_LIT_BSDFDATA_TANGENTWS (1037)
#define DEBUGVIEW_LIT_BSDFDATA_BITANGENTWS (1038)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESST (1039)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESSB (1040)
#define DEBUGVIEW_LIT_BSDFDATA_SUBSURFACERADIUS (1041)
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS (1042)
#define DEBUGVIEW_LIT_BSDFDATA_SUBSURFACEPROFILE (1043)
#define DEBUGVIEW_LIT_BSDFDATA_COATNORMALWS (1044)
#define DEBUGVIEW_LIT_BSDFDATA_COATROUGHNESS (1045)

// Generated from UnityEngine.ScriptableRenderLoop.Lit.SurfaceData
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
	float metalic;
	float specular;
	float subSurfaceRadius;
	float thickness;
	int subSurfaceProfile;
	float3 coatNormalWS;
	float coatPerceptualSmoothness;
	float3 specularColor;
};

// Generated from UnityEngine.ScriptableRenderLoop.Lit.BSDFData
// PackingRules = Exact
struct BSDFData
{
	float3 diffuseColor;
	float3 fresnel0;
	float specularOcclusion;
	float3 normalWS;
	float perceptualRoughness;
	float roughness;
	float materialId;
	float3 tangentWS;
	float3 bitangentWS;
	float roughnessT;
	float roughnessB;
	float subSurfaceRadius;
	float thickness;
	int subSurfaceProfile;
	float3 coatNormalWS;
	float coatRoughness;
};


