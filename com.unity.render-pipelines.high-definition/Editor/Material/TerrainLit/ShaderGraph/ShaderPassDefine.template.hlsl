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

#define SHADERPASS_MAINTEX     (27)
#define SHADERPASS_METALLICTEX (28)

#if SHADERPASS == SHADERPASS_METALLICTEX
#define sampler_Splat0 sampler_Mask0
#define sampler_Splat1 sampler_Mask1
#define sampler_Splat2 sampler_Mask2
#define sampler_Splat3 sampler_Mask3
#define sampler_Splat4 sampler_Mask4
#define sampler_Splat5 sampler_Mask5
#define sampler_Splat6 sampler_Mask6
#define sampler_Splat7 sampler_Mask7
#endif

#if defined(UNITY_INSTANCING_ENABLED) && defined(_TERRAIN_INSTANCED_PERPIXEL_NORMAL)
#define ENABLE_TERRAIN_PERPIXEL_NORMAL
#endif

SAMPLER(sampler_Mask0);
SAMPLER(sampler_Splat1);
SAMPLER(sampler_Splat2);
SAMPLER(sampler_Splat3);
SAMPLER(sampler_Splat4);
SAMPLER(sampler_Splat5);
SAMPLER(sampler_Splat6);
SAMPLER(sampler_Splat7);

SAMPLER(sampler_Control1);

#define SampleLayerAlbedo(i) SampleLayerAlbedoGrad(_Splat##i, sampler_Splat##i, splatuv, splat##i##dxuv, splat##i##dyuv, _DiffuseRemapScale##i.xyz)

#ifdef _NORMALMAP
    #define SampleLayerNormal(i) SampleLayerNormalGrad(_Normal##i, sampler_Splat##i, splatuv, splat##i##dxuv, splat##i##dyuv, _NormalScale##i)
#else
    #define SampleLayerNormal(i) float3(0, 0, 0)
#endif

float4 RemapMasks(float4 masks, float4 remapOffset, float4 remapScale)
{
    return masks * remapScale + remapOffset;
}

#ifdef _MASKMAP
    #define LayerMaskMode(i)    RemapMasks(SAMPLE_TEXTURE2D_GRAD(_Mask##i, sampler_Splat##i, splatuv, splat##i##dxuv, splat##i##dyuv), _MaskMapRemapOffset##i, _MaskMapRemapScale##i)
    #define SampleLayerMasks(i) lerp(DefaultMask(i), LayerMaskMode(i), _LayerHasMask##i)
    #define NullLayerMask(i)    float4(0, 1, _MaskMapRemapOffset##i.z, 0) // only height matters when weight is zero.
#else
    #define SampleLayerMasks(i) DefaultMask(i)
    #define NullLayerMask(i)    float4(0, 1, 0, 0)
#endif

#define DECLARE_LAYER_PREREQUISITES \
    float4 albedo[_LAYER_COUNT];    \
    float3 normal[_LAYER_COUNT];    \
    float4 masks[_LAYER_COUNT];     \
    float2 dxuv = ddx(IN.uv0.xy);   \
    float2 dyuv = ddy(IN.uv0.xy);   \
    float2 splatuv;

#define DECLARE_AND_FETCH_LAYER_ATTRIBUTES(i)                       \
    float2 splat##i##dxuv = dxuv * _Splat##i##_ST.x;                \
    float2 splat##i##dyuv = dyuv * _Splat##i##_ST.x;                \
    splatuv = IN.uv0.xy * _Splat##i##_ST.xy + _Splat##i##_ST.zw;    \
    albedo[i] = SampleLayerAlbedo(i);                               \
    normal[i] = SampleLayerNormal(i);                               \
    masks[i] = SampleLayerMasks(i);

#ifdef _TERRAIN_8_LAYERS
#define DECLARE_AND_FETCH_LAYER_ATTRIBUTES_8LAYERS(i, j) DECLARE_AND_FETCH_LAYER_ATTRIBUTES(i)
#else
#define DECLARE_AND_FETCH_LAYER_ATTRIBUTES_8LAYERS(i, j)
#endif

#define FetchLayerAlbedo0 albedo[0].rgb
#define FetchLayerAlbedo1 albedo[1].rgb
#define FetchLayerAlbedo2 albedo[2].rgb
#define FetchLayerAlbedo3 albedo[3].rgb

#define FetchLayerNormal0 normal[0]
#define FetchLayerNormal1 normal[1]
#define FetchLayerNormal2 normal[2]
#define FetchLayerNormal3 normal[3]

#define FetchLayerMetallic0 masks[0].r
#define FetchLayerMetallic1 masks[1].r
#define FetchLayerMetallic2 masks[2].r
#define FetchLayerMetallic3 masks[3].r

#define FetchLayerSmoothness0 masks[0].a
#define FetchLayerSmoothness1 masks[1].a
#define FetchLayerSmoothness2 masks[2].a
#define FetchLayerSmoothness3 masks[3].a

#define FetchLayerOcclusion0 masks[0].g
#define FetchLayerOcclusion1 masks[1].g
#define FetchLayerOcclusion2 masks[2].g
#define FetchLayerOcclusion3 masks[3].g

#ifdef _TERRAIN_8_LAYERS
    #define FetchLayerAlbedo4 albedo[4].rgb
    #define FetchLayerAlbedo5 albedo[5].rgb
    #define FetchLayerAlbedo6 albedo[6].rgb
    #define FetchLayerAlbedo7 albedo[7].rgb

    #define FetchLayerNormal4 normal[4]
    #define FetchLayerNormal5 normal[5]
    #define FetchLayerNormal6 normal[6]
    #define FetchLayerNormal7 normal[7]

    #define FetchLayerMetallic4 masks[4].r
    #define FetchLayerMetallic5 masks[5].r
    #define FetchLayerMetallic6 masks[6].r
    #define FetchLayerMetallic7 masks[7].r

    #define FetchLayerSmoothness4 masks[4].a
    #define FetchLayerSmoothness5 masks[5].a
    #define FetchLayerSmoothness6 masks[6].a
    #define FetchLayerSmoothness7 masks[7].a

    #define FetchLayerOcclusion4 masks[4].g
    #define FetchLayerOcclusion5 masks[5].g
    #define FetchLayerOcclusion6 masks[6].g
    #define FetchLayerOcclusion7 masks[7].g
#else
    #define FetchLayerAlbedo4 0.0
    #define FetchLayerAlbedo5 0.0
    #define FetchLayerAlbedo6 0.0
    #define FetchLayerAlbedo7 0.0

    #define FetchLayerNormal4 0.0
    #define FetchLayerNormal5 0.0
    #define FetchLayerNormal6 0.0
    #define FetchLayerNormal7 0.0

    #define FetchLayerMetallic4 0.0
    #define FetchLayerMetallic5 0.0
    #define FetchLayerMetallic6 0.0
    #define FetchLayerMetallic7 0.0

    #define FetchLayerSmoothness4 0.0
    #define FetchLayerSmoothness5 0.0
    #define FetchLayerSmoothness6 0.0
    #define FetchLayerSmoothness7 0.0

    #define FetchLayerOcclusion4 0.0
    #define FetchLayerOcclusion5 0.0
    #define FetchLayerOcclusion6 0.0
    #define FetchLayerOcclusion7 0.0
#endif

#define FetchLayerAlbedo(i, control) (FetchLayerAlbedo##i * control)
#define FetchLayerNormal(i, control) (FetchLayerNormal##i * control)
#define FetchLayerMetallic(i, control) (FetchLayerMetallic##i * control)
#define FetchLayerSmoothness(i, control) (FetchLayerSmoothness##i * control)
#define FetchLayerOcclusion(i, control) (FetchLayerSmoothness##i * control)
