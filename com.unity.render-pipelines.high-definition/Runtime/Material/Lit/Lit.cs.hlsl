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
#define DEBUGVIEW_LIT_BSDFDATA_THICKNESS (1059)
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
#define DEBUGVIEW_LIT_BSDFDATA_IOR (1074)

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
    float ior;
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
float3 GetDiffuseColor(in BSDFData bsdfData)
{
    return (bsdfData.diffuseColor);
}
float3 GetFresnel0(in BSDFData bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.fresnel0);
}
uint GetMaterialFeatures(in BSDFData bsdfData)
{
    return (bsdfData.materialFeatures);
}
float GetPerceptualRoughness(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 24, 8);
}
float GetCoatRoughness(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 16, 8);
}
float GetAmbientOcclusion(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 8, 8);
}
float GetSpecularOcclusion(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.roughnessesAndOcclusions, 0, 8);
}
uint GetDiffusionProfile(in BSDFData bsdfData)
{
    return BitFieldExtract(bsdfData.SSSData, 24, 8);
}
float GetSubsurfaceMask(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.SSSData, 16, 8);
}
float GetThickness(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.SSSData, 0, 16);
}
float GetAnisotropy(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 24, 8);
}
float GetRoughnessT(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 16, 8);
}
float GetRoughnessB(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.anisoDataAndFlags, 8, 8);
}
uint GetFlags(in BSDFData bsdfData)
{
    return BitFieldExtract(bsdfData.anisoDataAndFlags, 0, 8);
}
float GetIridescenceThickness(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 24, 8);
}
float GetIridescenceMask(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 16, 8);
}
float GetTransmittanceMask(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 8, 8);
}
float GetCoatMask(in BSDFData bsdfData)
{
    return UnpackUIntToFloat(bsdfData.iridescenceAndMasks, 0, 8);
}
float3 GetNormalWS(in BSDFData bsdfData)
{
    return (bsdfData.normalWS);
}
float3 GetTransmittance(in BSDFData bsdfData)
{
    return (bsdfData.transmittance);
}
float3 GetTangentWS(in BSDFData bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.tangentWS);
}
float3 GetBitangentWS(in BSDFData bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.bitangentWS);
}
float3 GetAbsorptionCoefficient(in BSDFData bsdfData)
{
    return UnpackFromR11G11B10f(bsdfData.absorptionCoefficient);
}
float GetIOR(in BSDFData bsdfData)
{
    return (bsdfData.ior);
}


 void SetDiffuseColor(float3 newDiffuseColor, inout BSDFData bsdfData)
{
    bsdfData.diffuseColor = newDiffuseColor;
}


 void SetFresnel0(float3 newFresnel0, inout BSDFData bsdfData)
{
    bsdfData.fresnel0 = PackToR11G11B10f(newFresnel0);
}


 void SetMaterialFeatures(uint newMaterialFeatures, inout BSDFData bsdfData)
{
    bsdfData.materialFeatures = newMaterialFeatures;
}


 void SetPerceptualRoughness(float newPerceptualRoughness, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 24, UnpackByte(newPerceptualRoughness) << 24, bsdfData.roughnessesAndOcclusions);
}


 void SetCoatRoughness(float newCoatRoughness, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 16, UnpackByte(newCoatRoughness) << 16, bsdfData.roughnessesAndOcclusions);
}


 void SetAmbientOcclusion(float newAmbientOcclusion, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 8, UnpackByte(newAmbientOcclusion) << 8, bsdfData.roughnessesAndOcclusions);
}


 void SetSpecularOcclusion(float newSpecularOcclusion, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 0, UnpackByte(newSpecularOcclusion) << 0, bsdfData.roughnessesAndOcclusions);
}


 void SetDiffusionProfile(uint newDiffusionProfile, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 24, (newDiffusionProfile) << 24, bsdfData.SSSData);
}


 void SetSubsurfaceMask(float newSubsurfaceMask, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 16, UnpackByte(newSubsurfaceMask) << 16, bsdfData.SSSData);
}


 void SetThickness(float newThickness, inout BSDFData bsdfData)
{
    BitFieldInsert(0xffff << 0, UnpackShort(newThickness) << 0, bsdfData.SSSData);
}


 void SetAnisotropy(float newAnisotropy, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 24, UnpackByte(newAnisotropy) << 24, bsdfData.anisoDataAndFlags);
}


 void SetRoughnessT(float newRoughnessT, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 16, UnpackByte(newRoughnessT) << 16, bsdfData.anisoDataAndFlags);
}


 void SetRoughnessB(float newRoughnessB, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 8, UnpackByte(newRoughnessB) << 8, bsdfData.anisoDataAndFlags);
}


 void SetFlags(uint newFlags, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 0, (newFlags) << 0, bsdfData.anisoDataAndFlags);
}


 void SetIridescenceThickness(float newIridescenceThickness, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 24, UnpackByte(newIridescenceThickness) << 24, bsdfData.iridescenceAndMasks);
}


 void SetIridescenceMask(float newIridescenceMask, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 16, UnpackByte(newIridescenceMask) << 16, bsdfData.iridescenceAndMasks);
}


 void SetTransmittanceMask(float newTransmittanceMask, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 8, UnpackByte(newTransmittanceMask) << 8, bsdfData.iridescenceAndMasks);
}


 void SetCoatMask(float newCoatMask, inout BSDFData bsdfData)
{
    BitFieldInsert(0xff << 0, UnpackByte(newCoatMask) << 0, bsdfData.iridescenceAndMasks);
}


 void SetNormalWS(float3 newNormalWS, inout BSDFData bsdfData)
{
    bsdfData.normalWS = newNormalWS;
}


 void SetTransmittance(float3 newTransmittance, inout BSDFData bsdfData)
{
    bsdfData.transmittance = newTransmittance;
}


 void SetTangentWS(float3 newTangentWS, inout BSDFData bsdfData)
{
    bsdfData.tangentWS = PackToR11G11B10f(newTangentWS);
}


 void SetBitangentWS(float3 newBitangentWS, inout BSDFData bsdfData)
{
    bsdfData.bitangentWS = PackToR11G11B10f(newBitangentWS);
}


 void SetAbsorptionCoefficient(float3 newAbsorptionCoefficient, inout BSDFData bsdfData)
{
    bsdfData.absorptionCoefficient = PackToR11G11B10f(newAbsorptionCoefficient);
}


 void SetIOR(float newIOR, inout BSDFData bsdfData)
{
    bsdfData.ior = newIOR;
}

 // Important: Init functions assume the field is filled with 0s, use setters otherwise. 
void InitDiffuseColor(float3 newDiffuseColor, inout BSDFData bsdfData)
{
    bsdfData.diffuseColor = newDiffuseColor;
}
void InitFresnel0(float3 newFresnel0, inout BSDFData bsdfData)
{
    bsdfData.fresnel0 = PackToR11G11B10f(newFresnel0);
}
void InitMaterialFeatures(uint newMaterialFeatures, inout BSDFData bsdfData)
{
    bsdfData.materialFeatures = newMaterialFeatures;
}
void InitPerceptualRoughness(float newPerceptualRoughness, inout BSDFData bsdfData)
{
    bsdfData.roughnessesAndOcclusions |= UnpackByte(newPerceptualRoughness) << 24;
}
void InitCoatRoughness(float newCoatRoughness, inout BSDFData bsdfData)
{
    bsdfData.roughnessesAndOcclusions |= UnpackByte(newCoatRoughness) << 16;
}
void InitAmbientOcclusion(float newAmbientOcclusion, inout BSDFData bsdfData)
{
    bsdfData.roughnessesAndOcclusions |= UnpackByte(newAmbientOcclusion) << 8;
}
void InitSpecularOcclusion(float newSpecularOcclusion, inout BSDFData bsdfData)
{
    bsdfData.roughnessesAndOcclusions |= UnpackByte(newSpecularOcclusion) << 0;
}
void InitDiffusionProfile(uint newDiffusionProfile, inout BSDFData bsdfData)
{
    bsdfData.SSSData |= (newDiffusionProfile) << 24;
}
void InitSubsurfaceMask(float newSubsurfaceMask, inout BSDFData bsdfData)
{
    bsdfData.SSSData |= UnpackByte(newSubsurfaceMask) << 16;
}
void InitThickness(float newThickness, inout BSDFData bsdfData)
{
    bsdfData.SSSData |= UnpackShort(newThickness) << 0;
}
void InitAnisotropy(float newAnisotropy, inout BSDFData bsdfData)
{
    bsdfData.anisoDataAndFlags |= UnpackByte(newAnisotropy) << 24;
}
void InitRoughnessT(float newRoughnessT, inout BSDFData bsdfData)
{
    bsdfData.anisoDataAndFlags |= UnpackByte(newRoughnessT) << 16;
}
void InitRoughnessB(float newRoughnessB, inout BSDFData bsdfData)
{
    bsdfData.anisoDataAndFlags |= UnpackByte(newRoughnessB) << 8;
}
void InitFlags(uint newFlags, inout BSDFData bsdfData)
{
    bsdfData.anisoDataAndFlags |= (newFlags) << 0;
}
void InitIridescenceThickness(float newIridescenceThickness, inout BSDFData bsdfData)
{
    bsdfData.iridescenceAndMasks |= UnpackByte(newIridescenceThickness) << 24;
}
void InitIridescenceMask(float newIridescenceMask, inout BSDFData bsdfData)
{
    bsdfData.iridescenceAndMasks |= UnpackByte(newIridescenceMask) << 16;
}
void InitTransmittanceMask(float newTransmittanceMask, inout BSDFData bsdfData)
{
    bsdfData.iridescenceAndMasks |= UnpackByte(newTransmittanceMask) << 8;
}
void InitCoatMask(float newCoatMask, inout BSDFData bsdfData)
{
    bsdfData.iridescenceAndMasks |= UnpackByte(newCoatMask) << 0;
}
void InitNormalWS(float3 newNormalWS, inout BSDFData bsdfData)
{
    bsdfData.normalWS = newNormalWS;
}
void InitTransmittance(float3 newTransmittance, inout BSDFData bsdfData)
{
    bsdfData.transmittance = newTransmittance;
}
void InitTangentWS(float3 newTangentWS, inout BSDFData bsdfData)
{
    bsdfData.tangentWS = PackToR11G11B10f(newTangentWS);
}
void InitBitangentWS(float3 newBitangentWS, inout BSDFData bsdfData)
{
    bsdfData.bitangentWS = PackToR11G11B10f(newBitangentWS);
}
void InitAbsorptionCoefficient(float3 newAbsorptionCoefficient, inout BSDFData bsdfData)
{
    bsdfData.absorptionCoefficient = PackToR11G11B10f(newAbsorptionCoefficient);
}
void InitIOR(float newIOR, inout BSDFData bsdfData)
{
    bsdfData.ior = newIOR;
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
        case DEBUGVIEW_LIT_BSDFDATA_THICKNESS:
            result = GetThickness(bsdfdata).xxx;
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
        case DEBUGVIEW_LIT_BSDFDATA_IOR:
            result = GetIOR(bsdfdata).xxx;
            break;
    }
}


#endif
