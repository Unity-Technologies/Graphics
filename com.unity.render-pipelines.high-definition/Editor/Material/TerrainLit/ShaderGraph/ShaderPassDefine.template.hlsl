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

// This shader support recursive rendering for raytracing
//#define HAVE_RECURSIVE_RENDERING

#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
#define ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif

#define DECLARE_SPLAT_PREREQUISITES \
    float4 albedo[_LAYER_COUNT];    \
    float3 normal[_LAYER_COUNT];    \
    float4 masks[_LAYER_COUNT];     \
    float2 dxuv = ddx(IN.uv0.xy);   \
    float2 dyuv = ddy(IN.uv0.xy);   \

#define DECLARE_AND_FETCH_SPLAT_ATTRIBUTES(i)                                   \
    float2 splat##i##uv = IN.uv0.xy * _Splat##i##_ST.xy + _Splat##i##_ST.zw;    \
    float2 splat##i##dxuv = dxuv * _Splat##i##_ST.x;                            \
    float2 splat##i##dyuv = dyuv * _Splat##i##_ST.x;                            \
    albedo[i] = SampleLayerAlbedo(i);                                           \
    normal[i] = SampleLayerNormal(i);                                           \
    masks[i] = SampleLayerMasks(i);                                             \
