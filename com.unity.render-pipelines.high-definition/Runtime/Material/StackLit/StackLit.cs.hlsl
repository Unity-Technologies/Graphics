//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef STACKLIT_CS_HLSL
#define STACKLIT_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.StackLit+MaterialFeatureFlags:  static fields
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
// UnityEngine.Rendering.HighDefinition.StackLit+SurfaceData:  static fields
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
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_LAYER_THICKNESS (1124)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_MASK (1125)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_COAT_FIXUP_TIR (1126)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_COAT_FIXUP_TIR_CLAMP (1127)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_SMOOTHNESS (1128)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_MASK (1129)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_IOR (1130)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_THICKNESS (1131)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_EXTINCTION_COEFFICIENT (1132)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_DIFFUSION_PROFILE_HASH (1133)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SUBSURFACE_MASK (1134)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_THICKNESS (1135)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_OCCLUSION_FROM_CUSTOM_INPUT (1136)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_OCCLUSION_FIXUP_VISIBILITY_RATIO_THRESHOLD (1137)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_OCCLUSION_FIXUP_STRENGTH (1138)
#define DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_OCCLUSION_FIXUP_MAX_ADDED_ROUGHNESS (1139)

//
// UnityEngine.Rendering.HighDefinition.StackLit+BSDFData:  static fields
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
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_MASK (1175)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_IOR (1176)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_THICKNESS (1177)
#define DEBUGVIEW_STACKLIT_BSDFDATA_COAT_EXTINCTION (1178)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_IOR (1179)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_THICKNESS (1180)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_MASK (1181)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_COAT_FIXUP_TIR (1182)
#define DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_COAT_FIXUP_TIRCLAMP (1183)
#define DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSION_PROFILE_INDEX (1184)
#define DEBUGVIEW_STACKLIT_BSDFDATA_SUBSURFACE_MASK (1185)
#define DEBUGVIEW_STACKLIT_BSDFDATA_THICKNESS (1186)
#define DEBUGVIEW_STACKLIT_BSDFDATA_USE_THICK_OBJECT_MODE (1187)
#define DEBUGVIEW_STACKLIT_BSDFDATA_TRANSMITTANCE (1188)
#define DEBUGVIEW_STACKLIT_BSDFDATA_SPECULAR_OCCLUSION_CUSTOM_INPUT (1189)
#define DEBUGVIEW_STACKLIT_BSDFDATA_SO_FIXUP_VISIBILITY_RATIO_THRESHOLD (1190)
#define DEBUGVIEW_STACKLIT_BSDFDATA_SO_FIXUP_STRENGTH_FACTOR (1191)
#define DEBUGVIEW_STACKLIT_BSDFDATA_SO_FIXUP_MAX_ADDED_ROUGHNESS (1192)

// Generated from UnityEngine.Rendering.HighDefinition.StackLit+SurfaceData
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
    float iridescenceCoatFixupTIR;
    float iridescenceCoatFixupTIRClamp;
    float coatPerceptualSmoothness;
    float coatMask;
    float coatIor;
    float coatThickness;
    float3 coatExtinction;
    uint diffusionProfileHash;
    float subsurfaceMask;
    float thickness;
    float specularOcclusionCustomInput;
    float soFixupVisibilityRatioThreshold;
    float soFixupStrengthFactor;
    float soFixupMaxAddedRoughness;
};

// Generated from UnityEngine.Rendering.HighDefinition.StackLit+BSDFData
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
    float coatMask;
    float coatIor;
    float coatThickness;
    float3 coatExtinction;
    float iridescenceIor;
    float iridescenceThickness;
    float iridescenceMask;
    float iridescenceCoatFixupTIR;
    float iridescenceCoatFixupTIRClamp;
    uint diffusionProfileIndex;
    float subsurfaceMask;
    float thickness;
    bool useThickObjectMode;
    float3 transmittance;
    float specularOcclusionCustomInput;
    float soFixupVisibilityRatioThreshold;
    float soFixupStrengthFactor;
    float soFixupMaxAddedRoughness;
};

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
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_LAYER_THICKNESS:
            result = surfacedata.iridescenceThickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_MASK:
            result = surfacedata.iridescenceMask.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_COAT_FIXUP_TIR:
            result = surfacedata.iridescenceCoatFixupTIR.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_IRIDESCENCE_COAT_FIXUP_TIR_CLAMP:
            result = surfacedata.iridescenceCoatFixupTIRClamp.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_SMOOTHNESS:
            result = surfacedata.coatPerceptualSmoothness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_COAT_MASK:
            result = surfacedata.coatMask.xxx;
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
        case DEBUGVIEW_STACKLIT_SURFACEDATA_DIFFUSION_PROFILE_HASH:
            result = GetIndexColor(surfacedata.diffusionProfileHash);
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SUBSURFACE_MASK:
            result = surfacedata.subsurfaceMask.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_THICKNESS:
            result = surfacedata.thickness.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_OCCLUSION_FROM_CUSTOM_INPUT:
            result = surfacedata.specularOcclusionCustomInput.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_OCCLUSION_FIXUP_VISIBILITY_RATIO_THRESHOLD:
            result = surfacedata.soFixupVisibilityRatioThreshold.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_OCCLUSION_FIXUP_STRENGTH:
            result = surfacedata.soFixupStrengthFactor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_SURFACEDATA_SPECULAR_OCCLUSION_FIXUP_MAX_ADDED_ROUGHNESS:
            result = surfacedata.soFixupMaxAddedRoughness.xxx;
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_COAT_MASK:
            result = bsdfdata.coatMask.xxx;
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_COAT_FIXUP_TIR:
            result = bsdfdata.iridescenceCoatFixupTIR.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_IRIDESCENCE_COAT_FIXUP_TIRCLAMP:
            result = bsdfdata.iridescenceCoatFixupTIRClamp.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_DIFFUSION_PROFILE_INDEX:
            result = GetIndexColor(bsdfdata.diffusionProfileIndex);
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
        case DEBUGVIEW_STACKLIT_BSDFDATA_SPECULAR_OCCLUSION_CUSTOM_INPUT:
            result = bsdfdata.specularOcclusionCustomInput.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_SO_FIXUP_VISIBILITY_RATIO_THRESHOLD:
            result = bsdfdata.soFixupVisibilityRatioThreshold.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_SO_FIXUP_STRENGTH_FACTOR:
            result = bsdfdata.soFixupStrengthFactor.xxx;
            break;
        case DEBUGVIEW_STACKLIT_BSDFDATA_SO_FIXUP_MAX_ADDED_ROUGHNESS:
            result = bsdfdata.soFixupMaxAddedRoughness.xxx;
            break;
    }
}


#endif
