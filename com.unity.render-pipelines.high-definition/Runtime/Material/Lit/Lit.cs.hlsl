//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef LIT_CS_HLSL
#define LIT_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_LIT_STANDARD (1)
#define MATERIALFEATUREFLAGS_LIT_SPECULAR_COLOR (2)
#define MATERIALFEATUREFLAGS_LIT_SUBSURFACE_SCATTERING (4)
#define MATERIALFEATUREFLAGS_LIT_TRANSMISSION (8)
#define MATERIALFEATUREFLAGS_LIT_ANISOTROPY (16)
#define MATERIALFEATUREFLAGS_LIT_IRIDESCENCE (32)
#define MATERIALFEATUREFLAGS_LIT_CLEAR_COAT (64)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData:  static fields
//
#define DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_FEATURES (1000)
#define DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR (1001)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION (1002)
#define DEBUGVIEW_LIT_SURFACEDATA_NORMAL (1003)
#define DEBUGVIEW_LIT_SURFACEDATA_NORMAL_VIEW_SPACE (1004)
#define DEBUGVIEW_LIT_SURFACEDATA_SMOOTHNESS (1005)
#define DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION (1006)
#define DEBUGVIEW_LIT_SURFACEDATA_METALLIC (1007)
#define DEBUGVIEW_LIT_SURFACEDATA_COAT_MASK (1008)
#define DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR (1009)
#define DEBUGVIEW_LIT_SURFACEDATA_DIFFUSION_PROFILE (1010)
#define DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_MASK (1011)
#define DEBUGVIEW_LIT_SURFACEDATA_THICKNESS (1012)
#define DEBUGVIEW_LIT_SURFACEDATA_TANGENT (1013)
#define DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY (1014)
#define DEBUGVIEW_LIT_SURFACEDATA_IRIDESCENCE_LAYER_THICKNESS (1015)
#define DEBUGVIEW_LIT_SURFACEDATA_IRIDESCENCE_MASK (1016)
#define DEBUGVIEW_LIT_SURFACEDATA_INDEX_OF_REFRACTION (1017)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_COLOR (1018)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_ABSORPTION_DISTANCE (1019)
#define DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_MASK (1020)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Lit+BSDFData:  static fields
//
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR (1050)
#define DEBUGVIEW_LIT_BSDFDATA_FRESNEL0 (1051)
#define DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES (1052)
#define DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS (1053)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS (1054)
#define DEBUGVIEW_LIT_BSDFDATA_AMBIENT_OCCLUSION (1055)
#define DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION (1056)
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSION_PROFILE (1057)
#define DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_MASK (1058)
#define DEBUGVIEW_LIT_BSDFDATA_IOR (1059)
#define DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY (1060)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T (1061)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B (1062)
#define DEBUGVIEW_LIT_BSDFDATA_FLAGS (1063)
#define DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_THICKNESS (1064)
#define DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_MASK (1065)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK (1066)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_MASK (1067)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS (1068)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_VIEW_SPACE (1069)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE (1070)
#define DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS (1071)
#define DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS (1072)
#define DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT (1073)
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS (1074)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float specularOcclusion;
    float3 normalWS;
    float perceptualSmoothness;
    float ambientOcclusion;
    float metallic;
    float coatMask;
    float3 specularColor;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
    float3 tangentWS;
    float anisotropy;
    float iridescenceThickness;
    float iridescenceMask;
    float ior;
    float3 transmittanceColor;
    float atDistance;
    float transmittanceMask;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Lit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 diffuseColor;
    uint fresnel0;
    uint materialFeatures;
    uint roughnessesAndOcclusions;
    uint SSSData;
    uint anisoDataAndFlags;
    uint iridescenceAndMasks;
    float3 normalWS;
    float3 transmittance;
    uint tangentWS;
    uint bitangentWS;
    uint absorptionCoefficient;
    float thickness;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_LIT_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_OCCLUSION:
            result = surfacedata.specularOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SMOOTHNESS:
            result = surfacedata.perceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_METALLIC:
            result = surfacedata.metallic.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_COAT_MASK:
            result = surfacedata.coatMask.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(surfacedata.diffusionProfile);
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_SUBSURFACE_MASK:
            result = surfacedata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_THICKNESS:
            result = surfacedata.thickness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TANGENT:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_ANISOTROPY:
            result = surfacedata.anisotropy.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_IRIDESCENCE_LAYER_THICKNESS:
            result = surfacedata.iridescenceThickness.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_IRIDESCENCE_MASK:
            result = surfacedata.iridescenceMask.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_INDEX_OF_REFRACTION:
            result = surfacedata.ior.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_COLOR:
            result = surfacedata.transmittanceColor;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_ABSORPTION_DISTANCE:
            result = surfacedata.atDistance.xxx;
            break;
        case DEBUGVIEW_LIT_SURFACEDATA_TRANSMITTANCE_MASK:
            result = surfacedata.transmittanceMask.xxx;
            break;
    }
}

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
//
// Accessors for packed fields
//
float3 GetDiffuseColor(in BSDFData bsdfdata)
{
    return (bsdfdata.diffuseColor);
}
float3 GetFresnel0(in BSDFData bsdfdata)
{
    return UnpackFromR11G11B10f(bsdfdata.fresnel0);
}
uint GetMaterialFeatures(in BSDFData bsdfdata)
{
    return (bsdfdata.materialFeatures);
}
float GetPerceptualRoughness(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.roughnessesAndOcclusions, 24, 8);
}
float GetCoatRoughness(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.roughnessesAndOcclusions, 16, 8);
}
float GetAmbientOcclusion(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.roughnessesAndOcclusions, 8, 8);
}
float GetSpecularOcclusion(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.roughnessesAndOcclusions, 0, 8);
}
uint GetDiffusionProfile(in BSDFData bsdfdata)
{
    return BitFieldExtract(bsdfdata.SSSData, 24, 8);
}
float GetSubsurfaceMask(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.SSSData, 16, 8);
}
float GetIOR(in BSDFData bsdfdata)
{
    return ((UnpackUIntToFloat(bsdfdata.SSSData, 0, 16) * 1.5) + 1);
}
float GetAnisotropy(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.anisoDataAndFlags, 24, 8);
}
float GetRoughnessT(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.anisoDataAndFlags, 16, 8);
}
float GetRoughnessB(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.anisoDataAndFlags, 8, 8);
}
uint GetFlags(in BSDFData bsdfdata)
{
    return BitFieldExtract(bsdfdata.anisoDataAndFlags, 0, 8);
}
float GetIridescenceThickness(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.iridescenceAndMasks, 24, 8);
}
float GetIridescenceMask(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.iridescenceAndMasks, 16, 8);
}
float GetTransmittanceMask(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.iridescenceAndMasks, 8, 8);
}
float GetCoatMask(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.iridescenceAndMasks, 0, 8);
}
float3 GetNormalWS(in BSDFData bsdfdata)
{
    return (bsdfdata.normalWS);
}
float3 GetTransmittance(in BSDFData bsdfdata)
{
    return (bsdfdata.transmittance);
}
float3 GetTangentWS(in BSDFData bsdfdata)
{
    return UnpackFromR11G11B10f(bsdfdata.tangentWS);
}
float3 GetBitangentWS(in BSDFData bsdfdata)
{
    return UnpackFromR11G11B10f(bsdfdata.bitangentWS);
}
float3 GetAbsorptionCoefficient(in BSDFData bsdfdata)
{
    return UnpackFromR11G11B10f(bsdfdata.absorptionCoefficient);
}
float GetThickness(in BSDFData bsdfdata)
{
    return (bsdfdata.thickness);
}
//
// Setters for packed fields
//
void SetDiffuseColor(float3 newDiffuseColor, inout BSDFData bsdfdata)
{
    bsdfdata.diffuseColor = newDiffuseColor;
}
void SetFresnel0(float3 newFresnel0, inout BSDFData bsdfdata)
{
    bsdfdata.fresnel0 = PackToR11G11B10f(newFresnel0);
}
void SetMaterialFeatures(uint newMaterialFeatures, inout BSDFData bsdfdata)
{
    bsdfdata.materialFeatures = newMaterialFeatures;
}
void SetPerceptualRoughness(float newPerceptualRoughness, inout BSDFData bsdfdata)
{
    bsdfdata.roughnessesAndOcclusions = BitFieldInsert(255 << 24, UnpackInt(newPerceptualRoughness, 8) << 24, bsdfdata.roughnessesAndOcclusions);
}
void SetCoatRoughness(float newCoatRoughness, inout BSDFData bsdfdata)
{
    bsdfdata.roughnessesAndOcclusions = BitFieldInsert(255 << 16, UnpackInt(newCoatRoughness, 8) << 16, bsdfdata.roughnessesAndOcclusions);
}
void SetAmbientOcclusion(float newAmbientOcclusion, inout BSDFData bsdfdata)
{
    bsdfdata.roughnessesAndOcclusions = BitFieldInsert(255 << 8, UnpackInt(newAmbientOcclusion, 8) << 8, bsdfdata.roughnessesAndOcclusions);
}
void SetSpecularOcclusion(float newSpecularOcclusion, inout BSDFData bsdfdata)
{
    bsdfdata.roughnessesAndOcclusions = BitFieldInsert(255 , UnpackInt(newSpecularOcclusion, 8) , bsdfdata.roughnessesAndOcclusions);
}
void SetDiffusionProfile(uint newDiffusionProfile, inout BSDFData bsdfdata)
{
    bsdfdata.SSSData = BitFieldInsert(255 << 24, (newDiffusionProfile) << 24, bsdfdata.SSSData);
}
void SetSubsurfaceMask(float newSubsurfaceMask, inout BSDFData bsdfdata)
{
    bsdfdata.SSSData = BitFieldInsert(255 << 16, UnpackInt(newSubsurfaceMask, 8) << 16, bsdfdata.SSSData);
}
void SetIOR(float newIOR, inout BSDFData bsdfdata)
{
    bsdfdata.SSSData = BitFieldInsert(65535 , UnpackInt(((newIOR - 1) / 1.5), 16) , bsdfdata.SSSData);
}
void SetAnisotropy(float newAnisotropy, inout BSDFData bsdfdata)
{
    bsdfdata.anisoDataAndFlags = BitFieldInsert(255 << 24, UnpackInt(newAnisotropy, 8) << 24, bsdfdata.anisoDataAndFlags);
}
void SetRoughnessT(float newRoughnessT, inout BSDFData bsdfdata)
{
    bsdfdata.anisoDataAndFlags = BitFieldInsert(255 << 16, UnpackInt(newRoughnessT, 8) << 16, bsdfdata.anisoDataAndFlags);
}
void SetRoughnessB(float newRoughnessB, inout BSDFData bsdfdata)
{
    bsdfdata.anisoDataAndFlags = BitFieldInsert(255 << 8, UnpackInt(newRoughnessB, 8) << 8, bsdfdata.anisoDataAndFlags);
}
void SetFlags(uint newFlags, inout BSDFData bsdfdata)
{
    bsdfdata.anisoDataAndFlags = BitFieldInsert(255 , (newFlags) , bsdfdata.anisoDataAndFlags);
}
void SetIridescenceThickness(float newIridescenceThickness, inout BSDFData bsdfdata)
{
    bsdfdata.iridescenceAndMasks = BitFieldInsert(255 << 24, UnpackInt(newIridescenceThickness, 8) << 24, bsdfdata.iridescenceAndMasks);
}
void SetIridescenceMask(float newIridescenceMask, inout BSDFData bsdfdata)
{
    bsdfdata.iridescenceAndMasks = BitFieldInsert(255 << 16, UnpackInt(newIridescenceMask, 8) << 16, bsdfdata.iridescenceAndMasks);
}
void SetTransmittanceMask(float newTransmittanceMask, inout BSDFData bsdfdata)
{
    bsdfdata.iridescenceAndMasks = BitFieldInsert(255 << 8, UnpackInt(newTransmittanceMask, 8) << 8, bsdfdata.iridescenceAndMasks);
}
void SetCoatMask(float newCoatMask, inout BSDFData bsdfdata)
{
    bsdfdata.iridescenceAndMasks = BitFieldInsert(255 , UnpackInt(newCoatMask, 8) , bsdfdata.iridescenceAndMasks);
}
void SetNormalWS(float3 newNormalWS, inout BSDFData bsdfdata)
{
    bsdfdata.normalWS = newNormalWS;
}
void SetTransmittance(float3 newTransmittance, inout BSDFData bsdfdata)
{
    bsdfdata.transmittance = newTransmittance;
}
void SetTangentWS(float3 newTangentWS, inout BSDFData bsdfdata)
{
    bsdfdata.tangentWS = PackToR11G11B10f(newTangentWS);
}
void SetBitangentWS(float3 newBitangentWS, inout BSDFData bsdfdata)
{
    bsdfdata.bitangentWS = PackToR11G11B10f(newBitangentWS);
}
void SetAbsorptionCoefficient(float3 newAbsorptionCoefficient, inout BSDFData bsdfdata)
{
    bsdfdata.absorptionCoefficient = PackToR11G11B10f(newAbsorptionCoefficient);
}
void SetThickness(float newThickness, inout BSDFData bsdfdata)
{
    bsdfdata.thickness = newThickness;
}
//
// Init functions for packed fields.
// Important: Init functions assume the field is filled with 0s, use setters otherwise. 
//
void InitDiffuseColor(float3 newDiffuseColor, inout BSDFData bsdfdata)
{
    bsdfdata.diffuseColor = newDiffuseColor;
}
void InitFresnel0(float3 newFresnel0, inout BSDFData bsdfdata)
{
    bsdfdata.fresnel0 = PackToR11G11B10f(newFresnel0);
}
void InitMaterialFeatures(uint newMaterialFeatures, inout BSDFData bsdfdata)
{
    bsdfdata.materialFeatures = newMaterialFeatures;
}
void InitPerceptualRoughness(float newPerceptualRoughness, inout BSDFData bsdfdata)
{
    bsdfdata.roughnessesAndOcclusions |= UnpackInt(newPerceptualRoughness, 8) << 24; 
}
void InitCoatRoughness(float newCoatRoughness, inout BSDFData bsdfdata)
{
    bsdfdata.roughnessesAndOcclusions |= UnpackInt(newCoatRoughness, 8) << 16; 
}
void InitAmbientOcclusion(float newAmbientOcclusion, inout BSDFData bsdfdata)
{
    bsdfdata.roughnessesAndOcclusions |= UnpackInt(newAmbientOcclusion, 8) << 8; 
}
void InitSpecularOcclusion(float newSpecularOcclusion, inout BSDFData bsdfdata)
{
    bsdfdata.roughnessesAndOcclusions |= UnpackInt(newSpecularOcclusion, 8) ; 
}
void InitDiffusionProfile(uint newDiffusionProfile, inout BSDFData bsdfdata)
{
    bsdfdata.SSSData |= (newDiffusionProfile) << 24;
}
void InitSubsurfaceMask(float newSubsurfaceMask, inout BSDFData bsdfdata)
{
    bsdfdata.SSSData |= UnpackInt(newSubsurfaceMask, 8) << 16; 
}
void InitIOR(float newIOR, inout BSDFData bsdfdata)
{
    bsdfdata.SSSData |= UnpackInt(((newIOR - 1) / 1.5), 16) ; 
}
void InitAnisotropy(float newAnisotropy, inout BSDFData bsdfdata)
{
    bsdfdata.anisoDataAndFlags |= UnpackInt(newAnisotropy, 8) << 24; 
}
void InitRoughnessT(float newRoughnessT, inout BSDFData bsdfdata)
{
    bsdfdata.anisoDataAndFlags |= UnpackInt(newRoughnessT, 8) << 16; 
}
void InitRoughnessB(float newRoughnessB, inout BSDFData bsdfdata)
{
    bsdfdata.anisoDataAndFlags |= UnpackInt(newRoughnessB, 8) << 8; 
}
void InitFlags(uint newFlags, inout BSDFData bsdfdata)
{
    bsdfdata.anisoDataAndFlags |= (newFlags) ;
}
void InitIridescenceThickness(float newIridescenceThickness, inout BSDFData bsdfdata)
{
    bsdfdata.iridescenceAndMasks |= UnpackInt(newIridescenceThickness, 8) << 24; 
}
void InitIridescenceMask(float newIridescenceMask, inout BSDFData bsdfdata)
{
    bsdfdata.iridescenceAndMasks |= UnpackInt(newIridescenceMask, 8) << 16; 
}
void InitTransmittanceMask(float newTransmittanceMask, inout BSDFData bsdfdata)
{
    bsdfdata.iridescenceAndMasks |= UnpackInt(newTransmittanceMask, 8) << 8; 
}
void InitCoatMask(float newCoatMask, inout BSDFData bsdfdata)
{
    bsdfdata.iridescenceAndMasks |= UnpackInt(newCoatMask, 8) ; 
}
void InitNormalWS(float3 newNormalWS, inout BSDFData bsdfdata)
{
    bsdfdata.normalWS = newNormalWS;
}
void InitTransmittance(float3 newTransmittance, inout BSDFData bsdfdata)
{
    bsdfdata.transmittance = newTransmittance;
}
void InitTangentWS(float3 newTangentWS, inout BSDFData bsdfdata)
{
    bsdfdata.tangentWS = PackToR11G11B10f(newTangentWS);
}
void InitBitangentWS(float3 newBitangentWS, inout BSDFData bsdfdata)
{
    bsdfdata.bitangentWS = PackToR11G11B10f(newBitangentWS);
}
void InitAbsorptionCoefficient(float3 newAbsorptionCoefficient, inout BSDFData bsdfdata)
{
    bsdfdata.absorptionCoefficient = PackToR11G11B10f(newAbsorptionCoefficient);
}
void InitThickness(float newThickness, inout BSDFData bsdfdata)
{
    bsdfdata.thickness = newThickness;
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR:
            result = GetDiffuseColor(bsdfdata);
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_FRESNEL0:
            result = GetFresnel0(bsdfdata);
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(GetMaterialFeatures(bsdfdata));
            break;
        case DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS:
            result = GetPerceptualRoughness(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS:
            result = GetCoatRoughness(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_AMBIENT_OCCLUSION:
            result = GetAmbientOcclusion(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION:
            result = GetSpecularOcclusion(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(GetDiffusionProfile(bsdfdata));
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_MASK:
            result = GetSubsurfaceMask(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IOR:
            result = GetIOR(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY:
            result = GetAnisotropy(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T:
            result = GetRoughnessT(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B:
            result = GetRoughnessB(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_FLAGS:
            result = GetIndexColor(GetFlags(bsdfdata));
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_THICKNESS:
            result = GetIridescenceThickness(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_MASK:
            result = GetIridescenceMask(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK:
            result = GetTransmittanceMask(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_MASK:
            result = GetCoatMask(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS:
            result = GetNormalWS(bsdfdata) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_NORMAL_VIEW_SPACE:
            result = GetNormalWS(bsdfdata) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE:
            result = GetTransmittance(bsdfdata);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS:
            result = GetTangentWS(bsdfdata) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS:
            result = GetBitangentWS(bsdfdata) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT:
            result = GetAbsorptionCoefficient(bsdfdata);
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_THICKNESS:
            result = GetThickness(bsdfdata).xxx;
            break;
    }
}


#endif
