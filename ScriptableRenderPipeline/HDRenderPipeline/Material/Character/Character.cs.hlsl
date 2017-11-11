//
// This file was automatically generated from Assets/ScriptableRenderPipeline/HDRenderPipeline/Material/Character/Character.cs.  Please don't edit by hand.
//

#ifndef CHARACTER_CS_HLSL
#define CHARACTER_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Character+CharacterMaterialID:  static fields
//
#define CHARACTERMATERIALID_SKIN (0)
#define CHARACTERMATERIALID_HAIR (1)
#define CHARACTERMATERIALID_EYE (2)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Character+SurfaceData:  static fields
//
#define DEBUGVIEW_CHARACTER_SURFACEDATA_DIFFUSE_COLOR (4000)
#define DEBUGVIEW_CHARACTER_SURFACEDATA_SPECULAR_OCCLUSION (4001)
#define DEBUGVIEW_CHARACTER_SURFACEDATA_NORMAL_WS (4002)
#define DEBUGVIEW_CHARACTER_SURFACEDATA_PERCEPTUAL_SMOOTHNESS (4003)
#define DEBUGVIEW_CHARACTER_SURFACEDATA_AMBIENT_OCCLUSION (4004)
#define DEBUGVIEW_CHARACTER_SURFACEDATA_TANGENT_WS (4005)
#define DEBUGVIEW_CHARACTER_SURFACEDATA_ANISOTROPY (4006)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Character+BSDFData:  static fields
//
#define DEBUGVIEW_CHARACTER_BSDFDATA_DIFFUSE_COLOR (4030)
#define DEBUGVIEW_CHARACTER_BSDFDATA_FRESNEL0 (4031)
#define DEBUGVIEW_CHARACTER_BSDFDATA_SPECULAR_OCCLUSION (4032)
#define DEBUGVIEW_CHARACTER_BSDFDATA_NORMAL_WS (4033)
#define DEBUGVIEW_CHARACTER_BSDFDATA_PERCEPTUAL_ROUGHNESS (4034)
#define DEBUGVIEW_CHARACTER_BSDFDATA_ROUGHNESS (4035)
#define DEBUGVIEW_CHARACTER_BSDFDATA_TANGENT_WS (4036)
#define DEBUGVIEW_CHARACTER_BSDFDATA_BITANGENT_WS (4037)
#define DEBUGVIEW_CHARACTER_BSDFDATA_ROUGHNESS_T (4038)
#define DEBUGVIEW_CHARACTER_BSDFDATA_ROUGHNESS_B (4039)
#define DEBUGVIEW_CHARACTER_BSDFDATA_ANISOTROPY (4040)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Character+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 diffuseColor;
    float specularOcclusion;
    float3 normalWS;
    float perceptualSmoothness;
    float ambientOcclusion;
    float3 tangentWS;
    float anisotropy;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Character+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 diffuseColor;
    float3 fresnel0;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float roughness;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessT;
    float roughnessB;
    float anisotropy;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_CHARACTER_SURFACEDATA_DIFFUSE_COLOR:
            result = surfacedata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_CHARACTER_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_CHARACTER_SURFACEDATA_NORMAL_WS:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_CHARACTER_SURFACEDATA_PERCEPTUAL_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_CHARACTER_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_CHARACTER_SURFACEDATA_TANGENT_WS:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_CHARACTER_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
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
        case DEBUGVIEW_CHARACTER_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_ROUGHNESS:
            result = bsdfdata.roughness.xxx;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_BITANGENT_WS:
            result = bsdfdata.bitangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_ROUGHNESS_T:
            result = bsdfdata.roughnessT.xxx;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_ROUGHNESS_B:
            result = bsdfdata.roughnessB.xxx;
            break;
        case DEBUGVIEW_CHARACTER_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
    }
}


#endif
