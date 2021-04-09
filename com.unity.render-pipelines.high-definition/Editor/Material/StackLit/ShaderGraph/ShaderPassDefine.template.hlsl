// StackLit shader_features:
$BaseParametrization.SpecularColor:                  #define _MATERIAL_FEATURE_SPECULAR_COLOR
$Material.Anisotropy:                                #define _MATERIAL_FEATURE_ANISOTROPY
$Material.Coat:                                      #define _MATERIAL_FEATURE_COAT
$Material.CoatNormal:                                #define _MATERIAL_FEATURE_COAT_NORMALMAP
$Material.DualSpecularLobe:                          #define _MATERIAL_FEATURE_DUAL_SPECULAR_LOBE
$DualSpecularLobeParametrization.HazyGloss:          #define _MATERIAL_FEATURE_HAZY_GLOSS
$Material.Iridescence:                               #define _MATERIAL_FEATURE_IRIDESCENCE
$Material.SubsurfaceScattering:                      #define _MATERIAL_FEATURE_SUBSURFACE_SCATTERING
$Material.Transmission:                              #define _MATERIAL_FEATURE_TRANSMISSION
$AmbientOcclusion:                                   #define _AMBIENT_OCCLUSION 1
$SpecularOcclusion:                                  #define _ENABLESPECULAROCCLUSION // main enable
// Performance vs appearance options
$AnisotropyForAreaLights:                            #define _ANISOTROPY_FOR_AREA_LIGHTS
$RecomputeStackPerLight:                             #define _VLAYERED_RECOMPUTE_PERLIGHT
$ShadeBaseUsingRefractedAngles:                      #define _VLAYERED_USE_REFRACTED_ANGLES_FOR_BASE
$HonorPerLightMinRoughness:                          #define _STACK_LIT_HONORS_LIGHT_MIN_ROUGHNESS
$StackLitDebug:                                      #define _STACKLIT_DEBUG

// StackLit.hlsl config defines (direct, no feature keywords):
// _SCREENSPACE_SPECULAROCCLUSION_METHOD, _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT, _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR
// _DATABASED_SPECULAROCCLUSION_METHOD, _DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT
$ScreenSpaceSpecularOcclusionBaseMode.Off:                          #define _SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_DISABLED
$ScreenSpaceSpecularOcclusionBaseMode.DirectFromAO:                 #define _SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO
$ScreenSpaceSpecularOcclusionBaseMode.ConeConeFromBentAO:           #define _SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CONECONE
$ScreenSpaceSpecularOcclusionBaseMode.SPTDIntegrationOfBentAO:      #define _SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_SPTD
// This enum case isn't handled, and normally not valided in the input UI, but if set, fallback to default:
$ScreenSpaceSpecularOcclusionBaseMode.Custom:                       #define _SCREENSPACE_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO

$ScreenSpaceSpecularOcclusionAOConeSize.UniformAO:                  #define _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_UNIFORM
$ScreenSpaceSpecularOcclusionAOConeSize.CosWeightedAO:              #define _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS
$ScreenSpaceSpecularOcclusionAOConeSize.CosWeightedBentCorrectAO:   #define _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION

$ScreenSpaceSpecularOcclusionAOConeDir.GeomNormal:                  #define _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_GEOM_NORMAL
$ScreenSpaceSpecularOcclusionAOConeDir.BentNormal:                  #define _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_BENT_NORMAL
$ScreenSpaceSpecularOcclusionAOConeDir.ShadingNormal:               #define _SCREENSPACE_SPECULAROCCLUSION_VISIBILITY_DIR BENT_VISIBILITY_DIR_SHADING_NORMAL

$DataBasedSpecularOcclusionBaseMode.Off:                            #define _DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_DISABLED
$DataBasedSpecularOcclusionBaseMode.DirectFromAO:                   #define _DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_FROM_AO
$DataBasedSpecularOcclusionBaseMode.ConeConeFromBentAO:             #define _DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CONECONE
$DataBasedSpecularOcclusionBaseMode.SPTDIntegrationOfBentAO:        #define _DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_SPTD
// TODO: Normally, we would need a per lobe specular occlusion value, or at least per interface in dual normal mode
//       (Main rationale is that roughness can change IBL fetch direction and not only BSDF lobe width, and interface normal changes shading reference frame
//       hence it also changes the directional relation between the visibility cone and the BSDF lobe)
$DataBasedSpecularOcclusionBaseMode.Custom:                         #define _DATABASED_SPECULAROCCLUSION_METHOD SPECULAR_OCCLUSION_CUSTOM_EXT_INPUT

$DataBasedSpecularOcclusionAOConeSize.UniformAO:                    #define _DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_UNIFORM
$DataBasedSpecularOcclusionAOConeSize.CosWeightedAO:                #define _DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS
$DataBasedSpecularOcclusionAOConeSize.CosWeightedBentCorrectAO:     #define _DATABASED_SPECULAROCCLUSION_VISIBILITY_FROM_AO_WEIGHT BENT_VISIBILITY_FROM_AO_COS_BENT_CORRECTION

// Cone fixup is only for cone methods and only for data based SO:
$SpecularOcclusionConeFixupMethod.Off:                              #define _BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_NONE
$SpecularOcclusionConeFixupMethod.BoostBSDFRoughness:               #define _BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_BOOST_BSDF_ROUGHNESS
$SpecularOcclusionConeFixupMethod.TiltDirectionToGeomNormal:        #define _BENT_VISIBILITY_FIXUP_FLAGS BENT_VISIBILITY_FIXUP_FLAGS_TILT_BENTNORMAL_TO_GEOM
$SpecularOcclusionConeFixupMethod.BoostAndTilt:                     #define _BENT_VISIBILITY_FIXUP_FLAGS (BENT_VISIBILITY_FIXUP_FLAGS_BOOST_BSDF_ROUGHNESS|BENT_VISIBILITY_FIXUP_FLAGS_TILT_BENTNORMAL_TO_GEOM)

// If we use subsurface scattering, enable output split lighting
#if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) && !defined(_SURFACE_TYPE_TRANSPARENT)
#define OUTPUT_SPLIT_LIGHTING
#endif

#if !( (SHADERPASS == SHADERPASS_FORWARD) || (SHADERPASS == SHADERPASS_LIGHT_TRANSPORT) \
       || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT)\
       || (SHADERPASS == SHADERPASS_PATH_TRACING) || (SHADERPASS == SHADERPASS_RAYTRACING_SUB_SURFACE) \
       || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) )
// StackLit.hlsl hooks the callback from PostInitBuiltinData() via #define MODIFY_BAKED_DIFFUSE_LIGHTING
// but in ShaderGraph, we don't evaluate/set all input ports when the values are not used by the pass.
// (In the material with the inspector UI, unused values were still normally set for all passes, here we
// don't, this saves compilation time, but these should always be pruned by compilation anyways).
// To prevent warnings, we disable our ModifyBakedDiffuseLighting() callback, while still leaving the
// call to PostInitBuiltinData() here along with avoiding putting SHADERPASS dependencies directly in
// StackLit.hlsl.
#define DISABLE_MODIFY_BAKED_DIFFUSE_LIGHTING
#endif
