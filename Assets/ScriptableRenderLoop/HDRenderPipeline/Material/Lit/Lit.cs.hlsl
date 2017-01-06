//
// This file was automatically generated from Assets/ScriptableRenderLoop/HDRenderPipeline/Material/Lit/Lit.cs.  Please don't edit by hand.
//

#ifndef LIT_CS_HLSL
#define LIT_CS_HLSL
//
// UnityEngine.Experimental.ScriptableRenderLoop.Lit.MaterialId:  static fields
//
#define MATERIALID_LIT_STANDARD (0)
#define MATERIALID_LIT_SSS (1)
#define MATERIALID_LIT_CLEAR_COAT (2)
#define MATERIALID_LIT_SPECULAR (3)
#define MATERIALID_LIT_ANISO (4)

//
// UnityEngine.Experimental.ScriptableRenderLoop.Lit.SurfaceData:  static fields
//
#define DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR (1000)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION (1001)
#define DEBUGVIEW_LIT_SURFACEDATA_NORMAL_WS (1002)
#define DEBUGVIEW_LIT_SURFACEDATA_PERCEPTUAL_SMOOTHNESS (1003)
#define DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_ID (1004)
#define DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION (1005)
#define DEBUGVIEW_LIT_SURFACEDATA_TANGENT_WS (1006)
#define DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY (1007)
#define DEBUGVIEW_LIT_SURFACEDATA_METALLIC (1008)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR (1009)
#define DEBUGVIEW_LIT_SURFACEDATA_SUB_SURFACE_RADIUS (1010)
#define DEBUGVIEW_LIT_SURFACEDATA_THICKNESS (1011)
#define DEBUGVIEW_LIT_SURFACEDATA_SUB_SURFACE_PROFILE (1012)
#define DEBUGVIEW_LIT_SURFACEDATA_COAT_NORMAL_WS (1013)
#define DEBUGVIEW_LIT_SURFACEDATA_COAT_PERCEPTUAL_SMOOTHNESS (1014)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR (1015)

//
// UnityEngine.Experimental.ScriptableRenderLoop.Lit.BSDFData:  static fields
//
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR (1030)
#define DEBUGVIEW_LIT_BSDFDATA_FRESNEL0 (1031)
#define DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION (1032)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS (1033)
#define DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS (1034)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS (1035)
#define DEBUGVIEW_LIT_BSDFDATA_MATERIAL_ID (1036)
#define DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS (1037)
#define DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS (1038)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T (1039)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B (1040)
#define DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY (1041)
#define DEBUGVIEW_LIT_BSDFDATA_SUB_SURFACE_RADIUS (1042)
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS (1043)
#define DEBUGVIEW_LIT_BSDFDATA_SUB_SURFACE_PROFILE (1044)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_NORMAL_WS (1045)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS (1046)

//
// UnityEngine.Experimental.ScriptableRenderLoop.Lit.GBufferMaterial:  static fields
//
#define GBUFFERMATERIAL_COUNT (4)

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.Lit.SurfaceData
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
	float metallic;
	float specular;
	float subSurfaceRadius;
	float thickness;
	int subSurfaceProfile;
	float3 coatNormalWS;
	float coatPerceptualSmoothness;
	float3 specularColor;
};

// Generated from UnityEngine.Experimental.ScriptableRenderLoop.Lit.BSDFData
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
	float anisotropy;
	float subSurfaceRadius;
	float thickness;
	int subSurfaceProfile;
	float3 coatNormalWS;
	float coatRoughness;
};


#endif
