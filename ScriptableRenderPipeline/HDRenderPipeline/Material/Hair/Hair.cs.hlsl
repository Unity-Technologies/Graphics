//
// This file was automatically generated from Assets/ScriptableRenderPipeline/HDRenderPipeline/Material/Hair/Hair.cs.  Please don't edit by hand.
//

#ifndef HAIR_CS_HLSL
#define HAIR_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Hair+SurfaceData:  static fields
//
#define DEBUGVIEW_HAIR_SURFACEDATA_DIFFUSE_COLOR (2570)
#define DEBUGVIEW_HAIR_SURFACEDATA_SPECULAR_OCCLUSION (2571)
#define DEBUGVIEW_HAIR_SURFACEDATA_NORMAL_WS (2572)
#define DEBUGVIEW_HAIR_SURFACEDATA_PERCEPTUAL_SMOOTHNESS (2573)
#define DEBUGVIEW_HAIR_SURFACEDATA_AMBIENT_OCCLUSION (2574)
#define DEBUGVIEW_HAIR_SURFACEDATA_TANGENT_WS (2575)
#define DEBUGVIEW_HAIR_SURFACEDATA_ANISOTROPY (2576)
#define DEBUGVIEW_HAIR_SURFACEDATA_IS_FRONT_FACE (2577)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Hair+BSDFData:  static fields
//
#define DEBUGVIEW_HAIR_BSDFDATA_DIFFUSE_COLOR (2600)
#define DEBUGVIEW_HAIR_BSDFDATA_FRESNEL0 (2601)
#define DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_OCCLUSION (2602)
#define DEBUGVIEW_HAIR_BSDFDATA_NORMAL_WS (2603)
#define DEBUGVIEW_HAIR_BSDFDATA_PERCEPTUAL_ROUGHNESS (2604)
#define DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS (2605)
#define DEBUGVIEW_HAIR_BSDFDATA_TANGENT_WS (2606)
#define DEBUGVIEW_HAIR_BSDFDATA_BITANGENT_WS (2607)
#define DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_T (2608)
#define DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_B (2609)
#define DEBUGVIEW_HAIR_BSDFDATA_ANISOTROPY (2610)
#define DEBUGVIEW_HAIR_BSDFDATA_IS_FRONT_FACE (2611)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Hair+SurfaceData
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
    bool isFrontFace;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Hair+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 diffuseColor;
    float3 fresnel0;
    float specularOcclusion;
    float3 normalWS;
    float perceptualRoughness;
    float roughness;
    int materialId;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessT;
    float roughnessB;
    float anisotropy;
    bool isFrontFace;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_HAIR_SURFACEDATA_DIFFUSE_COLOR:
            result = surfacedata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_NORMAL_WS:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_PERCEPTUAL_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_TANGENT_WS:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_HAIR_SURFACEDATA_IS_FRONT_FACE:
            result = (surfacedata.isFrontFace) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
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
        case DEBUGVIEW_HAIR_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_SPECULAR_OCCLUSION:
            result = bsdfdata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.perceptualRoughness.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS:
            result = bsdfdata.roughness.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_BITANGENT_WS:
            result = bsdfdata.bitangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_T:
            result = bsdfdata.roughnessT.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ROUGHNESS_B:
            result = bsdfdata.roughnessB.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_ANISOTROPY:
            result = bsdfdata.anisotropy.xxx;
            break;
        case DEBUGVIEW_HAIR_BSDFDATA_IS_FRONT_FACE:
            result = (bsdfdata.isFrontFace) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
    }
}


#endif
