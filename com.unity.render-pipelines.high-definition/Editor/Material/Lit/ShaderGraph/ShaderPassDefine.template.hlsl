$Material.SubsurfaceScattering:     #define _MATERIAL_FEATURE_SUBSURFACE_SCATTERING 1
$Material.Transmission:             #define _MATERIAL_FEATURE_TRANSMISSION 1
$Material.Anisotropy:               #define _MATERIAL_FEATURE_ANISOTROPY 1
$Material.Iridescence:              #define _MATERIAL_FEATURE_IRIDESCENCE 1
$Material.SpecularColor:            #define _MATERIAL_FEATURE_SPECULAR_COLOR 1
$Material.ClearCoat:                #define _MATERIAL_FEATURE_CLEAR_COAT
$AmbientOcclusion:                  #define _AMBIENT_OCCLUSION 1
$SpecularOcclusionFromAO:           #define _SPECULAR_OCCLUSION_FROM_AO 1
$SpecularOcclusionFromAOBentNormal: #define _SPECULAR_OCCLUSION_FROM_AO_BENT_NORMAL 1
$SpecularOcclusionCustom:           #define _SPECULAR_OCCLUSION_CUSTOM 1
$Specular.EnergyConserving:         #define _ENERGY_CONSERVING_SPECULAR 1
$Specular.AA:                       #define _ENABLE_GEOMETRIC_SPECULAR_AA 1
$RefractionBox:                     #define _REFRACTION_PLANE 1
$RefractionSphere:                  #define _REFRACTION_SPHERE 1
$RefractionThin:                    #define _REFRACTION_THIN 1

// If we use subsurface scattering, enable output split lighting (for forward pass)
#if defined(_MATERIAL_FEATURE_SUBSURFACE_SCATTERING) && !defined(_SURFACE_TYPE_TRANSPARENT)
    #define OUTPUT_SPLIT_LIGHTING
#endif

// This shader support recursive rendering for raytracing
#define HAVE_RECURSIVE_RENDERING

// In Path Tracing, For all single-sided, refractive materials, we want to force a thin refraction model
#if (SHADERPASS == SHADERPASS_PATH_TRACING) && !defined(_DOUBLESIDED_ON) && (defined(_REFRACTION_PLANE) || defined(_REFRACTION_SPHERE))
    #undef  _REFRACTION_PLANE
    #undef  _REFRACTION_SPHERE
    #define _REFRACTION_THIN
#endif
