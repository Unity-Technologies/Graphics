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
#define DEBUGVIEW_LIT_BSDFDATA_FRESNEL0 (1050)
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR (1051)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS (1052)
#define DEBUGVIEW_LIT_BSDFDATA_NORMAL_VIEW_SPACE (1053)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK (1054)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_MASK (1055)
#define DEBUGVIEW_LIT_BSDFDATA_MATERIAL_FEATURES (1056)
#define DEBUGVIEW_LIT_BSDFDATA_PERCEPTUAL_ROUGHNESS (1057)
#define DEBUGVIEW_LIT_BSDFDATA_COAT_ROUGHNESS (1058)
#define DEBUGVIEW_LIT_BSDFDATA_AMBIENT_OCCLUSION (1059)
#define DEBUGVIEW_LIT_BSDFDATA_SPECULAR_OCCLUSION (1060)
#define DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE (1061)
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS (1062)
#define DEBUGVIEW_LIT_BSDFDATA_IOR (1063)
#define DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_MASK (1064)
#define DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_THICKNESS (1065)
#define DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_MASK (1066)
#define DEBUGVIEW_LIT_BSDFDATA_DIFFUSION_PROFILE (1067)
#define DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS (1068)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B (1069)
#define DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T (1070)
#define DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY (1071)
#define DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS (1072)
#define DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT (1073)

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
    float3 fresnel0;
    uint diffuseColor;
    float3 normalWS;
    uint materialFeatures;
    uint roughnessesAndOcclusions;
    uint transmittance;
    float thickness;
    uint IORMasksAndSSS;
    float3 tangentWS;
    uint anisoData;
    float3 bitangentWS;
    uint absorptionCoefficient;
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
float3 GetFresnel0(in BSDFData bsdfdata)
{
    return (bsdfdata.fresnel0);
}
float3 GetDiffuseColor(in BSDFData bsdfdata)
{
    return UnpackFromR11G11B10f(bsdfdata.diffuseColor);
}
float3 GetNormalWS(in BSDFData bsdfdata)
{
    return (bsdfdata.normalWS);
}
float GetTransmittanceMask(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.materialFeatures, 27, 5);
}
float GetCoatMask(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.materialFeatures, 22, 5);
}
uint GetMaterialFeatures(in BSDFData bsdfdata)
{
    return BitFieldExtract(bsdfdata.materialFeatures, 0, 22);
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
float3 GetTransmittance(in BSDFData bsdfdata)
{
    return UnpackFromR11G11B10f(bsdfdata.transmittance);
}
float GetThickness(in BSDFData bsdfdata)
{
    return (bsdfdata.thickness);
}
float GetIOR(in BSDFData bsdfdata)
{
    return ((UnpackUIntToFloat(bsdfdata.IORMasksAndSSS, 20, 12) * 1.5)  +1);
}
float GetIridescenceMask(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.IORMasksAndSSS, 16, 4);
}
float GetIridescenceThickness(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.IORMasksAndSSS, 8, 8);
}
float GetSubsurfaceMask(in BSDFData bsdfdata)
{
    return UnpackUIntToFloat(bsdfdata.IORMasksAndSSS, 4, 4);
}
uint GetDiffusionProfile(in BSDFData bsdfdata)
{
    return BitFieldExtract(bsdfdata.IORMasksAndSSS, 0, 4);
}
float3 GetTangentWS(in BSDFData bsdfdata)
{
    return (bsdfdata.tangentWS);
}
float GetRoughnessB(in BSDFData bsdfdata)
{
    return ((UnpackUIntToFloat(bsdfdata.anisoData, 20, 12) * 2)  -0);
}
float GetRoughnessT(in BSDFData bsdfdata)
{
    return ((UnpackUIntToFloat(bsdfdata.anisoData, 8, 12) * 2)  -0);
}
float GetAnisotropy(in BSDFData bsdfdata)
{
    return ((UnpackUIntToFloat(bsdfdata.anisoData, 0, 8) * 2)  -1);
}
float3 GetBitangentWS(in BSDFData bsdfdata)
{
    return (bsdfdata.bitangentWS);
}
float3 GetAbsorptionCoefficient(in BSDFData bsdfdata)
{
    return UnpackFromR11G11B10f(bsdfdata.absorptionCoefficient);
}
//
// Setters for packed fields
//
void SetFresnel0(float3 newFresnel0, inout BSDFData bsdfdata)
{
    bsdfdata.fresnel0 = newFresnel0;
}
void SetDiffuseColor(float3 newDiffuseColor, inout BSDFData bsdfdata)
{
    bsdfdata.diffuseColor = PackToR11G11B10f(newDiffuseColor);
}
void SetNormalWS(float3 newNormalWS, inout BSDFData bsdfdata)
{
    bsdfdata.normalWS = newNormalWS;
}
void SetTransmittanceMask(float newTransmittanceMask, inout BSDFData bsdfdata)
{
    bsdfdata.materialFeatures = BitFieldInsert(31 << 27, UnpackInt(newTransmittanceMask, 5) << 27, bsdfdata.materialFeatures);
}
void SetCoatMask(float newCoatMask, inout BSDFData bsdfdata)
{
    bsdfdata.materialFeatures = BitFieldInsert(31 << 22, UnpackInt(newCoatMask, 5) << 22, bsdfdata.materialFeatures);
}
void SetMaterialFeatures(uint newMaterialFeatures, inout BSDFData bsdfdata)
{
    bsdfdata.materialFeatures = BitFieldInsert(4194303 , (newMaterialFeatures) , bsdfdata.materialFeatures);
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
void SetTransmittance(float3 newTransmittance, inout BSDFData bsdfdata)
{
    bsdfdata.transmittance = PackToR11G11B10f(newTransmittance);
}
void SetThickness(float newThickness, inout BSDFData bsdfdata)
{
    bsdfdata.thickness = newThickness;
}
void SetIOR(float newIOR, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS = BitFieldInsert(4095 << 20, UnpackInt(((newIOR * 0.6666667)  - 0.6666667), 12) << 20, bsdfdata.IORMasksAndSSS);
}
void SetIridescenceMask(float newIridescenceMask, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS = BitFieldInsert(15 << 16, UnpackInt(newIridescenceMask, 4) << 16, bsdfdata.IORMasksAndSSS);
}
void SetIridescenceThickness(float newIridescenceThickness, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS = BitFieldInsert(255 << 8, UnpackInt(newIridescenceThickness, 8) << 8, bsdfdata.IORMasksAndSSS);
}
void SetSubsurfaceMask(float newSubsurfaceMask, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS = BitFieldInsert(15 << 4, UnpackInt(newSubsurfaceMask, 4) << 4, bsdfdata.IORMasksAndSSS);
}
void SetDiffusionProfile(uint newDiffusionProfile, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS = BitFieldInsert(15 , (newDiffusionProfile) , bsdfdata.IORMasksAndSSS);
}
void SetTangentWS(float3 newTangentWS, inout BSDFData bsdfdata)
{
    bsdfdata.tangentWS = newTangentWS;
}
void SetRoughnessB(float newRoughnessB, inout BSDFData bsdfdata)
{
    bsdfdata.anisoData = BitFieldInsert(4095 << 20, UnpackInt(((newRoughnessB * 0.5)  + 0), 12) << 20, bsdfdata.anisoData);
}
void SetRoughnessT(float newRoughnessT, inout BSDFData bsdfdata)
{
    bsdfdata.anisoData = BitFieldInsert(4095 << 8, UnpackInt(((newRoughnessT * 0.5)  + 0), 12) << 8, bsdfdata.anisoData);
}
void SetAnisotropy(float newAnisotropy, inout BSDFData bsdfdata)
{
    bsdfdata.anisoData = BitFieldInsert(255 , UnpackInt(((newAnisotropy * 0.5)  + 0.5), 8) , bsdfdata.anisoData);
}
void SetBitangentWS(float3 newBitangentWS, inout BSDFData bsdfdata)
{
    bsdfdata.bitangentWS = newBitangentWS;
}
void SetAbsorptionCoefficient(float3 newAbsorptionCoefficient, inout BSDFData bsdfdata)
{
    bsdfdata.absorptionCoefficient = PackToR11G11B10f(newAbsorptionCoefficient);
}
//
// Init functions for packed fields.
// Important: Init functions assume the field is filled with 0s, use setters otherwise. 
//
void InitFresnel0(float3 newFresnel0, inout BSDFData bsdfdata)
{
    bsdfdata.fresnel0 = newFresnel0;
}
void InitDiffuseColor(float3 newDiffuseColor, inout BSDFData bsdfdata)
{
    bsdfdata.diffuseColor = PackToR11G11B10f(newDiffuseColor);
}
void InitNormalWS(float3 newNormalWS, inout BSDFData bsdfdata)
{
    bsdfdata.normalWS = newNormalWS;
}
void InitTransmittanceMask(float newTransmittanceMask, inout BSDFData bsdfdata)
{
    bsdfdata.materialFeatures |= UnpackInt(newTransmittanceMask, 5) << 27; 
}
void InitCoatMask(float newCoatMask, inout BSDFData bsdfdata)
{
    bsdfdata.materialFeatures |= UnpackInt(newCoatMask, 5) << 22; 
}
void InitMaterialFeatures(uint newMaterialFeatures, inout BSDFData bsdfdata)
{
    bsdfdata.materialFeatures |= (newMaterialFeatures) ;
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
void InitTransmittance(float3 newTransmittance, inout BSDFData bsdfdata)
{
    bsdfdata.transmittance = PackToR11G11B10f(newTransmittance);
}
void InitThickness(float newThickness, inout BSDFData bsdfdata)
{
    bsdfdata.thickness = newThickness;
}
void InitIOR(float newIOR, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS |= UnpackInt(((newIOR * 0.6666667)  - 0.6666667), 12) << 20; 
}
void InitIridescenceMask(float newIridescenceMask, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS |= UnpackInt(newIridescenceMask, 4) << 16; 
}
void InitIridescenceThickness(float newIridescenceThickness, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS |= UnpackInt(newIridescenceThickness, 8) << 8; 
}
void InitSubsurfaceMask(float newSubsurfaceMask, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS |= UnpackInt(newSubsurfaceMask, 4) << 4; 
}
void InitDiffusionProfile(uint newDiffusionProfile, inout BSDFData bsdfdata)
{
    bsdfdata.IORMasksAndSSS |= (newDiffusionProfile) ;
}
void InitTangentWS(float3 newTangentWS, inout BSDFData bsdfdata)
{
    bsdfdata.tangentWS = newTangentWS;
}
void InitRoughnessB(float newRoughnessB, inout BSDFData bsdfdata)
{
    bsdfdata.anisoData |= UnpackInt(((newRoughnessB * 0.5)  + 0), 12) << 20; 
}
void InitRoughnessT(float newRoughnessT, inout BSDFData bsdfdata)
{
    bsdfdata.anisoData |= UnpackInt(((newRoughnessT * 0.5)  + 0), 12) << 8; 
}
void InitAnisotropy(float newAnisotropy, inout BSDFData bsdfdata)
{
    bsdfdata.anisoData |= UnpackInt(((newAnisotropy * 0.5)  + 0.5), 8) ; 
}
void InitBitangentWS(float3 newBitangentWS, inout BSDFData bsdfdata)
{
    bsdfdata.bitangentWS = newBitangentWS;
}
void InitAbsorptionCoefficient(float3 newAbsorptionCoefficient, inout BSDFData bsdfdata)
{
    bsdfdata.absorptionCoefficient = PackToR11G11B10f(newAbsorptionCoefficient);
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_LIT_BSDFDATA_FRESNEL0:
            result = GetFresnel0(bsdfdata);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSE_COLOR:
            result = GetDiffuseColor(bsdfdata);
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_NORMAL_WS:
            result = GetNormalWS(bsdfdata) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_NORMAL_VIEW_SPACE:
            result = GetNormalWS(bsdfdata) * 0.5 + 0.5;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE_MASK:
            result = GetTransmittanceMask(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_COAT_MASK:
            result = GetCoatMask(bsdfdata).xxx;
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
        case DEBUGVIEW_LIT_BSDFDATA_TRANSMITTANCE:
            result = GetTransmittance(bsdfdata);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_THICKNESS:
            result = GetThickness(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IOR:
            result = GetIOR(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_MASK:
            result = GetIridescenceMask(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_IRIDESCENCE_THICKNESS:
            result = GetIridescenceThickness(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_SUBSURFACE_MASK:
            result = GetSubsurfaceMask(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(GetDiffusionProfile(bsdfdata));
            break;
        case DEBUGVIEW_LIT_BSDFDATA_TANGENT_WS:
            result = GetTangentWS(bsdfdata);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_B:
            result = GetRoughnessB(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ROUGHNESS_T:
            result = GetRoughnessT(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ANISOTROPY:
            result = GetAnisotropy(bsdfdata).xxx;
            break;
        case DEBUGVIEW_LIT_BSDFDATA_BITANGENT_WS:
            result = GetBitangentWS(bsdfdata);
            break;
        case DEBUGVIEW_LIT_BSDFDATA_ABSORPTION_COEFFICIENT:
            result = GetAbsorptionCoefficient(bsdfdata);
            break;
    }
}


#endif
