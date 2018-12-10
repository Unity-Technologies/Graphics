//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef STACKLIT_CS_HLSL
#define STACKLIT_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+MaterialFeatureFlags:  static fields
//
#define MATERIALFEATUREFLAGS_STACK_LIT_STANDARD (1)
#define MATERIALFEATUREFLAGS_STACK_LIT_DUAL_SPECULAR_LOBE (2)
#define MATERIALFEATUREFLAGS_STACK_LIT_ANISOTROPY (4)
#define MATERIALFEATUREFLAGS_STACK_LIT_COAT (8)
#define MATERIALFEATUREFLAGS_STACK_LIT_IRIDESCENCE (16)
#define MATERIALFEATUREFLAGS_STACK_LIT_SUBSURFACE_SCATTERING (32)
#define MATERIALFEATUREFLAGS_STACK_LIT_TRANSMISSION (64)
#define MATERIALFEATUREFLAGS_STACK_LIT_COAT_NORMAL_MAP (128)
#define MATERIALFEATUREFLAGS_STACK_LIT_SPECULAR_COLOR (256)
#define MATERIALFEATUREFLAGS_STACK_LIT_HAZY_GLOSS (512)

//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+SurfaceData:  static fields
//
#define DEBUGVIEW_STACKLIT_SURFACEDATA_MATERIAL_FEATURES (1100)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_BASE_COLOR (1101)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_AMBIENT_OCCLUSION (1102)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_METALLIC (1103)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_DIELECTRIC_IOR (1104)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_COLOR (1105)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL (1106)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL_VIEW_SPACE (1107)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL (1108)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1109)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_NORMAL (1110)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_NORMAL_VIEW_SPACE (1111)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_BENT_NORMAL (1112)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_BENT_NORMAL_VIEW_SPACE (1113)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_A (1114)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_B (1115)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_LOBE_MIXING (1116)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_HAZINESS (1117)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_HAZE_EXTENT (1118)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_HAZY_GLOSS_MAX_DIELECTRIC_F0_WHEN_USING_METALLIC_INPUT (1119)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_TANGENT (1120)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_ANISOTROPY_A (1121)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_ANISOTROPY_B (1122)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_IOR (1123)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_THICKNESS (1124)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_MASK (1125)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_SMOOTHNESS (1126)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_IOR (1127)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_THICKNESS (1128)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_EXTINCTION_COEFFICIENT (1129)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_DIFFUSION_PROFILE (1130)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SUBSURFACE_MASK (1131)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_THICKNESS (1132)

//
// UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData:  static fields
//
#define DEBUGVIEW_STACKLIT_BSDFDATA_MATERIAL_FEATURES (1150)
#define DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSE_COLOR (1151)
#define DEBUGVIEW_STACKLIT_BSDFDATA_FRESNEL0 (1152)
#define DEBUGVIEW_STACKLIT_BSDFDATA_AMBIENT_OCCLUSION (1153)
#define DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_WS (1154)
#define DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE (1155)
#define DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL (1156)
#define DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE (1157)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_NORMAL (1158)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_NORMAL_VIEW_SPACE (1159)
#define DEBUGVIEW_STACKLIT_BSDFDATA_BENT_NORMAL (1160)
#define DEBUGVIEW_STACKLIT_BSDFDATA_BENT_NORMAL_VIEW_SPACE (1161)
#define DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_A (1162)
#define DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_B (1163)
#define DEBUGVIEW_STACKLIT_BSDFDATA_LOBE_MIX (1164)
#define DEBUGVIEW_STACKLIT_BSDFDATA_TANGENT_WS (1165)
#define DEBUGVIEW_STACKLIT_BSDFDATA_BITANGENT_WS (1166)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AT (1167)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AB (1168)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BT (1169)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BB (1170)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ANISOTROPY_A (1171)
#define DEBUGVIEW_STACKLIT_BSDFDATA_ANISOTROPY_B (1172)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_ROUGHNESS (1173)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_PERCEPTUAL_ROUGHNESS (1174)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_IOR (1175)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_THICKNESS (1176)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_EXTINCTION (1177)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_IOR (1178)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_THICKNESS (1179)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_MASK (1180)
#define DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSION_PROFILE (1181)
#define DEBUGVIEW_STACKLIT_BSDFDATA_SUBSURFACE_MASK (1182)
#define DEBUGVIEW_STACKLIT_BSDFDATA_THICKNESS (1183)
#define DEBUGVIEW_STACKLIT_BSDFDATA_USE_THICK_OBJECT_MODE (1184)
#define DEBUGVIEW_STACKLIT_BSDFDATA_TRANSMITTANCE (1185)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.StackLit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    uint materialFeatures;
    float3 baseColor;
    float ambientOcclusion;
    float metallic;
    float dielectricIor;
    float3 specularColor;
    float3 normalWS;
    float3 geomNormalWS;
    float3 coatNormalWS;
    float3 bentNormalWS;
    float perceptualSmoothnessA;
    float perceptualSmoothnessB;
    float lobeMix;
    float haziness;
    float hazeExtent;
    float hazyGlossMaxDielectricF0;
    float3 tangentWS;
    float anisotropyA;
    float anisotropyB;
    float iridescenceIor;
    float iridescenceThickness;
    float iridescenceMask;
    float coatPerceptualSmoothness;
    float coatIor;
    float coatThickness;
    float3 coatExtinction;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    uint materialFeatures;
    float3 diffuseColor;
    float3 fresnel0;
    float ambientOcclusion;
    float3 normalWS;
    float3 geomNormalWS;
    float3 coatNormalWS;
    float3 bentNormalWS;
    float perceptualRoughnessA;
    float perceptualRoughnessB;
    float lobeMix;
    float3 tangentWS;
    float3 bitangentWS;
    float roughnessAT;
    float roughnessAB;
    float roughnessBT;
    float roughnessBB;
    float anisotropyA;
    float anisotropyB;
    float coatRoughness;
    float coatPerceptualRoughness;
    float coatIor;
    float coatThickness;
    float3 coatExtinction;
    float iridescenceIor;
    float iridescenceThickness;
    float iridescenceMask;
    uint diffusionProfile;
    float subsurfaceMask;
    float thickness;
    bool useThickObjectMode;
    float3 transmittance;
};

//
// Accessors for UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData
//
uint GetMaterialFeatures(BSDFData value)
{
    return value.materialFeatures;
}
float3 GetDiffuseColor(BSDFData value)
{
    return value.diffuseColor;
}
float3 GetFresnel0(BSDFData value)
{
    return value.fresnel0;
}
float GetAmbientOcclusion(BSDFData value)
{
    return value.ambientOcclusion;
}
float3 GetNormalWS(BSDFData value)
{
    return value.normalWS;
}
float3 GetGeomNormalWS(BSDFData value)
{
    return value.geomNormalWS;
}
float3 GetCoatNormalWS(BSDFData value)
{
    return value.coatNormalWS;
}
float3 GetBentNormalWS(BSDFData value)
{
    return value.bentNormalWS;
}
float GetPerceptualRoughnessA(BSDFData value)
{
    return value.perceptualRoughnessA;
}
float GetPerceptualRoughnessB(BSDFData value)
{
    return value.perceptualRoughnessB;
}
float GetLobeMix(BSDFData value)
{
    return value.lobeMix;
}
float3 GetTangentWS(BSDFData value)
{
    return value.tangentWS;
}
float3 GetBitangentWS(BSDFData value)
{
    return value.bitangentWS;
}
float GetRoughnessAT(BSDFData value)
{
    return value.roughnessAT;
}
float GetRoughnessAB(BSDFData value)
{
    return value.roughnessAB;
}
float GetRoughnessBT(BSDFData value)
{
    return value.roughnessBT;
}
float GetRoughnessBB(BSDFData value)
{
    return value.roughnessBB;
}
float GetAnisotropyA(BSDFData value)
{
    return value.anisotropyA;
}
float GetAnisotropyB(BSDFData value)
{
    return value.anisotropyB;
}
float GetCoatRoughness(BSDFData value)
{
    return value.coatRoughness;
}
float GetCoatPerceptualRoughness(BSDFData value)
{
    return value.coatPerceptualRoughness;
}
float GetCoatIor(BSDFData value)
{
    return value.coatIor;
}
float GetCoatThickness(BSDFData value)
{
    return value.coatThickness;
}
float3 GetCoatExtinction(BSDFData value)
{
    return value.coatExtinction;
}
float GetIridescenceIor(BSDFData value)
{
    return value.iridescenceIor;
}
float GetIridescenceThickness(BSDFData value)
{
    return value.iridescenceThickness;
}
float GetIridescenceMask(BSDFData value)
{
    return value.iridescenceMask;
}
uint GetDiffusionProfile(BSDFData value)
{
    return value.diffusionProfile;
}
float GetSubsurfaceMask(BSDFData value)
{
    return value.subsurfaceMask;
}
float GetThickness(BSDFData value)
{
    return value.thickness;
}
bool GetUseThickObjectMode(BSDFData value)
{
    return value.useThickObjectMode;
}
float3 GetTransmittance(BSDFData value)
{
    return value.transmittance;
}
//
// Setters for UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData
//
void SetMaterialFeatures(uint newValue, inout BSDFData dest )
{
    dest.materialFeatures = newValue;
}
void SetDiffuseColor(float3 newValue, inout BSDFData dest )
{
    dest.diffuseColor = newValue;
}
void SetFresnel0(float3 newValue, inout BSDFData dest )
{
    dest.fresnel0 = newValue;
}
void SetAmbientOcclusion(float newValue, inout BSDFData dest )
{
    dest.ambientOcclusion = newValue;
}
void SetNormalWS(float3 newValue, inout BSDFData dest )
{
    dest.normalWS = newValue;
}
void SetGeomNormalWS(float3 newValue, inout BSDFData dest )
{
    dest.geomNormalWS = newValue;
}
void SetCoatNormalWS(float3 newValue, inout BSDFData dest )
{
    dest.coatNormalWS = newValue;
}
void SetBentNormalWS(float3 newValue, inout BSDFData dest )
{
    dest.bentNormalWS = newValue;
}
void SetPerceptualRoughnessA(float newValue, inout BSDFData dest )
{
    dest.perceptualRoughnessA = newValue;
}
void SetPerceptualRoughnessB(float newValue, inout BSDFData dest )
{
    dest.perceptualRoughnessB = newValue;
}
void SetLobeMix(float newValue, inout BSDFData dest )
{
    dest.lobeMix = newValue;
}
void SetTangentWS(float3 newValue, inout BSDFData dest )
{
    dest.tangentWS = newValue;
}
void SetBitangentWS(float3 newValue, inout BSDFData dest )
{
    dest.bitangentWS = newValue;
}
void SetRoughnessAT(float newValue, inout BSDFData dest )
{
    dest.roughnessAT = newValue;
}
void SetRoughnessAB(float newValue, inout BSDFData dest )
{
    dest.roughnessAB = newValue;
}
void SetRoughnessBT(float newValue, inout BSDFData dest )
{
    dest.roughnessBT = newValue;
}
void SetRoughnessBB(float newValue, inout BSDFData dest )
{
    dest.roughnessBB = newValue;
}
void SetAnisotropyA(float newValue, inout BSDFData dest )
{
    dest.anisotropyA = newValue;
}
void SetAnisotropyB(float newValue, inout BSDFData dest )
{
    dest.anisotropyB = newValue;
}
void SetCoatRoughness(float newValue, inout BSDFData dest )
{
    dest.coatRoughness = newValue;
}
void SetCoatPerceptualRoughness(float newValue, inout BSDFData dest )
{
    dest.coatPerceptualRoughness = newValue;
}
void SetCoatIor(float newValue, inout BSDFData dest )
{
    dest.coatIor = newValue;
}
void SetCoatThickness(float newValue, inout BSDFData dest )
{
    dest.coatThickness = newValue;
}
void SetCoatExtinction(float3 newValue, inout BSDFData dest )
{
    dest.coatExtinction = newValue;
}
void SetIridescenceIor(float newValue, inout BSDFData dest )
{
    dest.iridescenceIor = newValue;
}
void SetIridescenceThickness(float newValue, inout BSDFData dest )
{
    dest.iridescenceThickness = newValue;
}
void SetIridescenceMask(float newValue, inout BSDFData dest )
{
    dest.iridescenceMask = newValue;
}
void SetDiffusionProfile(uint newValue, inout BSDFData dest )
{
    dest.diffusionProfile = newValue;
}
void SetSubsurfaceMask(float newValue, inout BSDFData dest )
{
    dest.subsurfaceMask = newValue;
}
void SetThickness(float newValue, inout BSDFData dest )
{
    dest.thickness = newValue;
}
void SetUseThickObjectMode(bool newValue, inout BSDFData dest )
{
    dest.useThickObjectMode = newValue;
}
void SetTransmittance(float3 newValue, inout BSDFData dest )
{
    dest.transmittance = newValue;
}
//
// Setters for UnityEngine.Experimental.Rendering.HDPipeline.StackLit+BSDFData
//
void InitMaterialFeatures(uint newValue, inout BSDFData dest )
{
    dest.materialFeatures = newValue;
}
void InitDiffuseColor(float3 newValue, inout BSDFData dest )
{
    dest.diffuseColor = newValue;
}
void InitFresnel0(float3 newValue, inout BSDFData dest )
{
    dest.fresnel0 = newValue;
}
void InitAmbientOcclusion(float newValue, inout BSDFData dest )
{
    dest.ambientOcclusion = newValue;
}
void InitNormalWS(float3 newValue, inout BSDFData dest )
{
    dest.normalWS = newValue;
}
void InitGeomNormalWS(float3 newValue, inout BSDFData dest )
{
    dest.geomNormalWS = newValue;
}
void InitCoatNormalWS(float3 newValue, inout BSDFData dest )
{
    dest.coatNormalWS = newValue;
}
void InitBentNormalWS(float3 newValue, inout BSDFData dest )
{
    dest.bentNormalWS = newValue;
}
void InitPerceptualRoughnessA(float newValue, inout BSDFData dest )
{
    dest.perceptualRoughnessA = newValue;
}
void InitPerceptualRoughnessB(float newValue, inout BSDFData dest )
{
    dest.perceptualRoughnessB = newValue;
}
void InitLobeMix(float newValue, inout BSDFData dest )
{
    dest.lobeMix = newValue;
}
void InitTangentWS(float3 newValue, inout BSDFData dest )
{
    dest.tangentWS = newValue;
}
void InitBitangentWS(float3 newValue, inout BSDFData dest )
{
    dest.bitangentWS = newValue;
}
void InitRoughnessAT(float newValue, inout BSDFData dest )
{
    dest.roughnessAT = newValue;
}
void InitRoughnessAB(float newValue, inout BSDFData dest )
{
    dest.roughnessAB = newValue;
}
void InitRoughnessBT(float newValue, inout BSDFData dest )
{
    dest.roughnessBT = newValue;
}
void InitRoughnessBB(float newValue, inout BSDFData dest )
{
    dest.roughnessBB = newValue;
}
void InitAnisotropyA(float newValue, inout BSDFData dest )
{
    dest.anisotropyA = newValue;
}
void InitAnisotropyB(float newValue, inout BSDFData dest )
{
    dest.anisotropyB = newValue;
}
void InitCoatRoughness(float newValue, inout BSDFData dest )
{
    dest.coatRoughness = newValue;
}
void InitCoatPerceptualRoughness(float newValue, inout BSDFData dest )
{
    dest.coatPerceptualRoughness = newValue;
}
void InitCoatIor(float newValue, inout BSDFData dest )
{
    dest.coatIor = newValue;
}
void InitCoatThickness(float newValue, inout BSDFData dest )
{
    dest.coatThickness = newValue;
}
void InitCoatExtinction(float3 newValue, inout BSDFData dest )
{
    dest.coatExtinction = newValue;
}
void InitIridescenceIor(float newValue, inout BSDFData dest )
{
    dest.iridescenceIor = newValue;
}
void InitIridescenceThickness(float newValue, inout BSDFData dest )
{
    dest.iridescenceThickness = newValue;
}
void InitIridescenceMask(float newValue, inout BSDFData dest )
{
    dest.iridescenceMask = newValue;
}
void InitDiffusionProfile(uint newValue, inout BSDFData dest )
{
    dest.diffusionProfile = newValue;
}
void InitSubsurfaceMask(float newValue, inout BSDFData dest )
{
    dest.subsurfaceMask = newValue;
}
void InitThickness(float newValue, inout BSDFData dest )
{
    dest.thickness = newValue;
}
void InitUseThickObjectMode(bool newValue, inout BSDFData dest )
{
    dest.useThickObjectMode = newValue;
}
void InitTransmittance(float3 newValue, inout BSDFData dest )
{
    dest.transmittance = newValue;
}
//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_STACKLIT_SURFACEDATA_MATERIAL_FEATURES:
            result = GetIndexColor(surfacedata.materialFeatures);
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_AMBIENT_OCCLUSION:
            result = surfacedata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_METALLIC:
            result = surfacedata.metallic.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_DIELECTRIC_IOR:
            result = surfacedata.dielectricIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_COLOR:
            result = surfacedata.specularColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = surfacedata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_NORMAL:
            result = surfacedata.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_NORMAL_VIEW_SPACE:
            result = surfacedata.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_BENT_NORMAL:
            result = surfacedata.bentNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_BENT_NORMAL_VIEW_SPACE:
            result = surfacedata.bentNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_A:
            result = surfacedata.perceptualSmoothnessA.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SMOOTHNESS_B:
            result = surfacedata.perceptualSmoothnessB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_LOBE_MIXING:
            result = surfacedata.lobeMix.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_HAZINESS:
            result = surfacedata.haziness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_HAZE_EXTENT:
            result = surfacedata.hazeExtent.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_HAZY_GLOSS_MAX_DIELECTRIC_F0_WHEN_USING_METALLIC_INPUT:
            result = surfacedata.hazyGlossMaxDielectricF0.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_TANGENT:
            result = surfacedata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_ANISOTROPY_A:
            result = surfacedata.anisotropyA.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_ANISOTROPY_B:
            result = surfacedata.anisotropyB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_IOR:
            result = surfacedata.iridescenceIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_THICKNESS:
            result = surfacedata.iridescenceThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_MASK:
            result = surfacedata.iridescenceMask.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_SMOOTHNESS:
            result = surfacedata.coatPerceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_IOR:
            result = surfacedata.coatIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_THICKNESS:
            result = surfacedata.coatThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_EXTINCTION_COEFFICIENT:
            result = surfacedata.coatExtinction;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(surfacedata.diffusionProfile);
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SUBSURFACE_MASK:
            result = surfacedata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_THICKNESS:
            result = surfacedata.thickness.xxx;
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_MATERIAL_FEATURES:
            result = GetIndexColor(bsdfdata.materialFeatures);
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_FRESNEL0:
            result = bsdfdata.fresnel0;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_AMBIENT_OCCLUSION:
            result = bsdfdata.ambientOcclusion.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_WS:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_NORMAL_VIEW_SPACE:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL:
            result = bsdfdata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_GEOMETRIC_NORMAL_VIEW_SPACE:
            result = bsdfdata.geomNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_NORMAL:
            result = bsdfdata.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_NORMAL_VIEW_SPACE:
            result = bsdfdata.coatNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_BENT_NORMAL:
            result = bsdfdata.bentNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_BENT_NORMAL_VIEW_SPACE:
            result = bsdfdata.bentNormalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_A:
            result = bsdfdata.perceptualRoughnessA.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_PERCEPTUAL_ROUGHNESS_B:
            result = bsdfdata.perceptualRoughnessB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_LOBE_MIX:
            result = bsdfdata.lobeMix.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_TANGENT_WS:
            result = bsdfdata.tangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_BITANGENT_WS:
            result = bsdfdata.bitangentWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AT:
            result = bsdfdata.roughnessAT.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_AB:
            result = bsdfdata.roughnessAB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BT:
            result = bsdfdata.roughnessBT.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ROUGHNESS_BB:
            result = bsdfdata.roughnessBB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ANISOTROPY_A:
            result = bsdfdata.anisotropyA.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_ANISOTROPY_B:
            result = bsdfdata.anisotropyB.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_ROUGHNESS:
            result = bsdfdata.coatRoughness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_PERCEPTUAL_ROUGHNESS:
            result = bsdfdata.coatPerceptualRoughness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_IOR:
            result = bsdfdata.coatIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_THICKNESS:
            result = bsdfdata.coatThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_EXTINCTION:
            result = bsdfdata.coatExtinction;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_IOR:
            result = bsdfdata.iridescenceIor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_THICKNESS:
            result = bsdfdata.iridescenceThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_MASK:
            result = bsdfdata.iridescenceMask.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSION_PROFILE:
            result = GetIndexColor(bsdfdata.diffusionProfile);
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_SUBSURFACE_MASK:
            result = bsdfdata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_THICKNESS:
            result = bsdfdata.thickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_USE_THICK_OBJECT_MODE:
            result = (bsdfdata.useThickObjectMode) ? float3(1.0, 1.0, 1.0) : float3(0.0, 0.0, 0.0);
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_TRANSMITTANCE:
            result = bsdfdata.transmittance;
            break;
    }
}


#endif
