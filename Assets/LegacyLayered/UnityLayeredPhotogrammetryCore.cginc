#ifndef UNITY_STANDARD_CORE_INCLUDED
#define UNITY_STANDARD_CORE_INCLUDED

#include "UnityCG.cginc"
#include "UnityShaderVariables.cginc"
#include "UnityInstancing.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityPBSLighting.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityGBuffer.cginc"
#include "UnityStandardBRDF.cginc"

#include "UnityLayeredPhotogrammetryInput.cginc"
#include "LayeredPhotogrammetrySampleUVMapping.cginc"

#include "AutoLight.cginc"

//-------------------------------------------------------------------------------------
// counterpart for NormalizePerPixelNormal
// skips normalization per-vertex and expects normalization to happen per-pixel
half3 NormalizePerVertexNormal (float3 n) // takes float to avoid overflow
{
    #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        return normalize(n);
    #else
        return n; // will normalize per-pixel instead
    #endif
}

half3 NormalizePerPixelNormal (half3 n)
{
    #if (SHADER_TARGET < 30) || UNITY_STANDARD_SIMPLE
        return n;
    #else
        return normalize(n);
    #endif
}

//-------------------------------------------------------------------------------------
UnityLight MainLight ()
{
    UnityLight l;

    l.color = _LightColor0.rgb;
    l.dir = _WorldSpaceLightPos0.xyz;
    return l;
}

UnityLight AdditiveLight (half3 lightDir, half atten)
{
    UnityLight l;

    l.color = _LightColor0.rgb;
    l.dir = lightDir;
    #ifndef USING_DIRECTIONAL_LIGHT
        l.dir = NormalizePerPixelNormal(l.dir);
    #endif

    // shadow the light
    l.color *= atten;
    return l;
}

UnityLight DummyLight ()
{
    UnityLight l;
    l.color = 0;
    l.dir = half3 (0,1,0);
    return l;
}

UnityIndirect ZeroIndirect ()
{
    UnityIndirect ind;
    ind.diffuse = 0;
    ind.specular = 0;
    return ind;
}

//-------------------------------------------------------------------------------------
// Common fragment setup

// deprecated
half3 WorldNormal(half4 tan2world[3])
{
    return normalize(tan2world[2].xyz);
}
//
//// deprecated
//#ifdef _TANGENT_TO_WORLD
//    half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
//    {
//        half3 t = tan2world[0].xyz;
//        half3 b = tan2world[1].xyz;
//        half3 n = tan2world[2].xyz;
//
//    #if UNITY_TANGENT_ORTHONORMALIZE
//        n = NormalizePerPixelNormal(n);
//
//        // ortho-normalize Tangent
//        t = normalize (t - n * dot(t, n));
//
//        // recalculate Binormal
//        half3 newB = cross(n, t);
//        b = newB * sign (dot (newB, b));
//    #endif
//
//        return half3x3(t, b, n);
//    }
//#else
//    half3x3 ExtractTangentToWorldPerPixel(half4 tan2world[3])
//    {
//        return half3x3(0,0,0,0,0,0,0,0,0);
//    }
//#endif
//
//half3 PerPixelWorldNormal(float4 i_tex, half4 tangentToWorld[3])
//{
//#ifdef _NORMALMAP
//    half3 tangent = tangentToWorld[0].xyz;
//    half3 binormal = tangentToWorld[1].xyz;
//    half3 normal = tangentToWorld[2].xyz;
//
//    #if UNITY_TANGENT_ORTHONORMALIZE
//        normal = NormalizePerPixelNormal(normal);
//
//        // ortho-normalize Tangent
//        tangent = normalize (tangent - normal * dot(tangent, normal));
//
//        // recalculate Binormal
//        half3 newB = cross(normal, tangent);
//        binormal = newB * sign (dot (newB, binormal));
//    #endif
//
//    half3 normalTangent = NormalInTangentSpace(i_tex);
//    half3 normalWorld = NormalizePerPixelNormal(tangent * normalTangent.x + binormal * normalTangent.y + normal * normalTangent.z); // @TODO: see if we can squeeze this normalize on SM2.0 as well
//#else
//    half3 normalWorld = normalize(tangentToWorld[2].xyz);
//#endif
//    return normalWorld;
//}

#ifdef _PARALLAXMAP
    #define IN_VIEWDIR4PARALLAX(i) NormalizePerPixelNormal(half3(i.tangentToWorldAndPackedData[0].w,i.tangentToWorldAndPackedData[1].w,i.tangentToWorldAndPackedData[2].w))
    #define IN_VIEWDIR4PARALLAX_FWDADD(i) NormalizePerPixelNormal(i.viewDirForParallax.xyz)
#else
    #define IN_VIEWDIR4PARALLAX(i) half3(0,0,0)
    #define IN_VIEWDIR4PARALLAX_FWDADD(i) half3(0,0,0)
#endif

#if UNITY_REQUIRE_FRAG_WORLDPOS
    #if UNITY_PACK_WORLDPOS_WITH_TANGENT
        #define IN_WORLDPOS(i) half3(i.tangentToWorldAndPackedData[0].w,i.tangentToWorldAndPackedData[1].w,i.tangentToWorldAndPackedData[2].w)
    #else
        #define IN_WORLDPOS(i) i.posWorld
    #endif
    #define IN_WORLDPOS_FWDADD(i) i.posWorld
#else
    #define IN_WORLDPOS(i) half3(0,0,0)
    #define IN_WORLDPOS_FWDADD(i) half3(0,0,0)
#endif

#define IN_LIGHTDIR_FWDADD(i) half3(i.tangentToWorldAndLightDir[0].w, i.tangentToWorldAndLightDir[1].w, i.tangentToWorldAndLightDir[2].w)

// BEGIN LAYERED_PHOTOGRAMMETRY
#define FRAGMENT_SETUP(x) FragmentCommonData x = \
    FragmentSetupLayeredPhotogrammetry(i.pos, i.tex, float4(i.ambientOrLightmapUV.xy, i.texUV3), i.color, i.eyeVec, i.tangentToWorldAndPackedData, IN_WORLDPOS(i));

// JIG CHECK
#define FRAGMENT_SETUP_FWDADD(x) FragmentCommonData x = \
    FragmentSetupLayeredPhotogrammetry(i.pos, i.tex, float4(float(0.0).xx, i.texUV3), i.color, i.eyeVec, i.tangentToWorldAndLightDir, IN_WORLDPOS_FWDADD(i));
// END LAYERED_PHOTOGRAMMETRY

struct FragmentCommonData
{
    half3 diffColor, specColor;
    // Note: smoothness & oneMinusReflectivity for optimization purposes, mostly for DX9 SM2.0 level.
    // Most of the math is being done on these (1-x) values, and that saves a few precious ALU slots.
    half oneMinusReflectivity, smoothness;
    half3 normalWorld, eyeVec, posWorld;
    half alpha;

#if UNITY_STANDARD_SIMPLE
    half3 reflUVW;
#endif

#if UNITY_STANDARD_SIMPLE
    half3 tangentSpaceNormal;
#endif
};

#ifndef UNITY_SETUP_BRDF_INPUT
    #define UNITY_SETUP_BRDF_INPUT SpecularSetup
#endif

//inline FragmentCommonData SpecularSetup (float4 i_tex)
//{
//    half4 specGloss = SpecularGloss(i_tex.xy);
//    half3 specColor = specGloss.rgb;
//    half smoothness = specGloss.a;
//
//    half oneMinusReflectivity;
//    half3 diffColor = EnergyConservationBetweenDiffuseAndSpecular (Albedo(i_tex), specColor, /*out*/ oneMinusReflectivity);
//
//    FragmentCommonData o = (FragmentCommonData)0;
//    o.diffColor = diffColor;
//    o.specColor = specColor;
//    o.oneMinusReflectivity = oneMinusReflectivity;
//    o.smoothness = smoothness;
//    return o;
//}
//
//inline FragmentCommonData MetallicSetup (float4 i_tex)
//{
//    half2 metallicGloss = MetallicGloss(i_tex.xy);
//    half metallic = metallicGloss.x;
//    half smoothness = metallicGloss.y; // this is 1 minus the square root of real roughness m.
//
//    half oneMinusReflectivity;
//    half3 specColor;
//    half3 diffColor = DiffuseAndSpecularFromMetallic (Albedo(i_tex), metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);
//
//    FragmentCommonData o = (FragmentCommonData)0;
//    o.diffColor = diffColor;
//    o.specColor = specColor;
//    o.oneMinusReflectivity = oneMinusReflectivity;
//    o.smoothness = smoothness;
//    return o;
//}


// BEGIN LAYERED_PHOTOGRAMMETRY
struct LayerTexCoord
{
    UVMapping base;
    UVMapping details;

    // Regular texcoord
    UVMapping base0;
    UVMapping base1;
    UVMapping base2;
    UVMapping base3;

    UVMapping details0;
    UVMapping details1;
    UVMapping details2;
    UVMapping details3;

    // Dedicated for blend mask
    UVMapping blendMask;

    // Store information that will be share by all UVMapping
    float3 vertexNormalWS; // TODO: store also object normal map for object triplanar
    float3 triplanarWeights;
};

struct FragInputs
{
    // Contain value return by SV_POSITION (That is name positionCS in PackedVarying).
    // xy: unormalized screen position (offset by 0.5), z: device depth, w: depth in view space
    // Note: SV_POSITION is the result of the clip space position provide to the vertex shaders that is transform by the viewport
    float4 unPositionSS; // In case depth offset is use, positionWS.w is equal to depth offset
    float3 positionWS;
    float2 texCoord0;
    float2 texCoord1;
    float2 texCoord2;
    float2 texCoord3;
    float4 color; // vertex color

    // TODO: confirm with Morten following statement
    // Our TBN is orthogonal but is maybe not orthonormal in order to be compliant with external bakers (Like xnormal that use mikktspace).
    // (xnormal for example take into account the interpolation when baking the normal and normalizing the tangent basis could cause distortion).
    // When using worldToTangent with surface gradient, it doesn't normalize the tangent/bitangent vector (We instead use exact same scale as applied to interpolated vertex normal to avoid breaking compliance).
    // this mean that any usage of worldToTangent[1] or worldToTangent[2] outside of the context of normal map (like for POM) must normalize the TBN (TCHECK if this make any difference ?)
    // When not using surface gradient, each vector of worldToTangent are normalize (TODO: Maybe they should not even in case of no surface gradient ? Ask Morten)
    float3x3 worldToTangent;

    // For two sided lighting
    bool isFrontFace;
};

struct SurfaceData
{
    float3 baseColor;
    float specularOcclusion;
    float3 normalWS;
    float perceptualSmoothness;
    int materialId;
    float ambientOcclusion;
    float3 tangentWS;
    float anisotropy;
    float metallic;
    float specular;
    float subsurfaceRadius;
    float thickness;
    int subsurfaceProfile;
    float3 specularColor;
};

// Number of sampler are limited, we need to share sampler as much as possible with lit material
// for this we put the constraint that the sampler are the same in a layered material for all textures of the same type
// then we take the sampler matching the first textures use of this type
#if defined(_NORMALMAP0)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap0
#elif defined(_NORMALMAP1)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap1
#elif defined(_NORMALMAP2)
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap2
#else
    #define SAMPLER_NORMALMAP_IDX sampler_NormalMap3
#endif

#if defined(_DETAIL_MAP0)
#define SAMPLER_DETAILMASK_IDX sampler_DetailMask0
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap0
#elif defined(_DETAIL_MAP1)
#define SAMPLER_DETAILMASK_IDX sampler_DetailMask1
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap1
#elif defined(_DETAIL_MAP2)
#define SAMPLER_DETAILMASK_IDX sampler_DetailMask2
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap2
#else
#define SAMPLER_DETAILMASK_IDX sampler_DetailMask3
#define SAMPLER_DETAILMAP_IDX sampler_DetailMap3
#endif

#if defined(_MASKMAP0)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap0
#elif defined(_MASKMAP1)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap1
#elif defined(_MASKMAP2)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap2
#else
#define SAMPLER_MASKMAP_IDX sampler_MaskMap3
#endif

#if defined(_SPECULAROCCLUSIONMAP0)
#define SAMPLER_SPECULAROCCLUSIONMAP_IDX sampler_SpecularOcclusionMap0
#elif defined(_SPECULAROCCLUSIONMAP1)
#define SAMPLER_SPECULAROCCLUSIONMAP_IDX sampler_SpecularOcclusionMap1
#elif defined(_SPECULAROCCLUSIONMAP2)
#define SAMPLER_SPECULAROCCLUSIONMAP_IDX sampler_SpecularOcclusionMap2
#else
#define SAMPLER_SPECULAROCCLUSIONMAP_IDX sampler_SpecularOcclusionMap3
#endif

#if defined(_HEIGHTMAP0)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap0
#elif defined(_HEIGHTMAP1)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap1
#elif defined(_HEIGHTMAP2)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap2
#elif defined(_HEIGHTMAP3)
#define SAMPLER_HEIGHTMAP_IDX sampler_HeightMap3
#endif

// Define a helper macro


#define ADD_ZERO_IDX(Name) Name##0

// include LitDataInternal multiple time to define the variation of GetSurfaceData for each layer
#define LAYER_INDEX 0
#define ADD_IDX(Name) Name##0
#ifdef _NORMALMAP0
#define _NORMALMAP_IDX
#endif
#ifdef _DETAIL_MAP0
#define _DETAIL_MAP_IDX
#endif
#ifdef _MASKMAP0
#define _MASKMAP_IDX
#endif
#ifdef _SPECULAROCCLUSIONMAP0
#define _SPECULAROCCLUSIONMAP_IDX
#endif
#include "LayeredPhotogrammetryDataInternal.cginc"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _DETAIL_MAP_IDX
#undef _MASKMAP_IDX
#undef _SPECULAROCCLUSIONMAP_IDX

#define LAYER_INDEX 1
#define ADD_IDX(Name) Name##1
#ifdef _NORMALMAP1
#define _NORMALMAP_IDX
#endif
#ifdef _DETAIL_MAP1
#define _DETAIL_MAP_IDX
#endif
#ifdef _MASKMAP1
#define _MASKMAP_IDX
#endif
#ifdef _SPECULAROCCLUSIONMAP1
#define _SPECULAROCCLUSIONMAP_IDX
#endif
#include "LayeredPhotogrammetryDataInternal.cginc"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _DETAIL_MAP_IDX
#undef _MASKMAP_IDX
#undef _SPECULAROCCLUSIONMAP_IDX

#define LAYER_INDEX 2
#define ADD_IDX(Name) Name##2
#ifdef _NORMALMAP2
#define _NORMALMAP_IDX
#endif
#ifdef _DETAIL_MAP2
#define _DETAIL_MAP_IDX
#endif
#ifdef _MASKMAP2
#define _MASKMAP_IDX
#endif
#ifdef _SPECULAROCCLUSIONMAP2
#define _SPECULAROCCLUSIONMAP_IDX
#endif
#include "LayeredPhotogrammetryDataInternal.cginc"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _DETAIL_MAP_IDX
#undef _MASKMAP_IDX
#undef _SPECULAROCCLUSIONMAP_IDX

#define LAYER_INDEX 3
#define ADD_IDX(Name) Name##3
#ifdef _NORMALMAP3
#define _NORMALMAP_IDX
#endif
#ifdef _DETAIL_MAP3
#define _DETAIL_MAP_IDX
#endif
#ifdef _MASKMAP3
#define _MASKMAP_IDX
#endif
#ifdef _SPECULAROCCLUSIONMAP3
#define _SPECULAROCCLUSIONMAP_IDX
#endif
#include "LayeredPhotogrammetryDataInternal.cginc"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _DETAIL_MAP_IDX
#undef _MASKMAP_IDX
#undef _SPECULAROCCLUSIONMAP_IDX

float3 BlendLayeredVector3(float3 x0, float3 x1, float3 x2, float3 x3, float weight[4])
{
    float3 result = float3(0.0, 0.0, 0.0);

    result = x0 * weight[0] + x1 * weight[1];
#if _LAYER_COUNT >= 3
    result += (x2 * weight[2]);
#endif
#if _LAYER_COUNT >= 4
    result += x3 * weight[3];
#endif

    return result;
}

float BlendLayeredScalar(float x0, float x1, float x2, float x3, float weight[4])
{
    float result = 0.0;

    result = x0 * weight[0] + x1 * weight[1];
#if _LAYER_COUNT >= 3
    result += x2 * weight[2];
#endif
#if _LAYER_COUNT >= 4
    result += x3 * weight[3];
#endif

    return result;
}

#define SURFACEDATA_BLEND_VECTOR3(surfaceData, name, mask) BlendLayeredVector3(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define SURFACEDATA_BLEND_SCALAR(surfaceData, name, mask) BlendLayeredScalar(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define PROP_BLEND_SCALAR(name, mask) BlendLayeredScalar(name##0, name##1, name##2, name##3, mask);

void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 vertexNormalWS, inout LayerTexCoord layerTexCoord)
{
    layerTexCoord.vertexNormalWS = vertexNormalWS;

    int mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR_BLENDMASK)
    mappingType = UV_MAPPING_PLANAR;
#endif

    // Be sure that the compiler is aware that we don't use UV1 to UV3 for main layer and blend mask so it can optimize code
    // Note: Blend mask have its dedicated mapping and tiling. And as Main layer it only use UV0
    _UVMappingMask0 = float4(1.0, 0.0, 0.0, 0.0);

    // To share code, we simply call the regular code from the main layer for it then save the result, then do regular call for all layers.
    ComputeLayerTexCoord0(  texCoord0, float2(0.0, 0.0), float2(0.0, 0.0), float2(0.0, 0.0),
                            positionWS, mappingType, _TexWorldScaleBlendMask, layerTexCoord, _LayerTilingBlendMask);

    layerTexCoord.blendMask = layerTexCoord.base0;

    // On all layers (but not on blend mask) we can scale the tiling with object scale (only uniform supported)
    // Note: the object scale doesn't affect planar/triplanar mapping as they already handle the object scale.
    float tileObjectScale = 1.0;
#ifdef _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
    // Extract scaling from world transform
    float4x4 worldTransform = GetObjectToWorldMatrix();
    // assuming uniform scaling, take only the first column
    tileObjectScale = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
#endif

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR0)
    mappingType = UV_MAPPING_PLANAR;
#endif

    ComputeLayerTexCoord0(  texCoord0, float2(0.0, 0.0), float2(0.0, 0.0), float2(0.0, 0.0),
                            positionWS, mappingType, _TexWorldScale0, layerTexCoord, _LayerTiling0
                            #if !defined(_MAIN_LAYER_INFLUENCE_MODE)
                            * tileObjectScale  // We only affect layer0 in case we are not in influence mode (i.e we should not change the base object)
                            #endif
                            );

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR1)
    mappingType = UV_MAPPING_PLANAR;
#endif
    ComputeLayerTexCoord1(  texCoord0, texCoord1, texCoord2, texCoord3,
                            positionWS, mappingType, _TexWorldScale1, layerTexCoord, _LayerTiling1 * tileObjectScale);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR2)
    mappingType = UV_MAPPING_PLANAR;
#endif
    ComputeLayerTexCoord2(  texCoord0, texCoord1, texCoord2, texCoord3,
                            positionWS, mappingType, _TexWorldScale2, layerTexCoord, _LayerTiling2 * tileObjectScale);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR3)
    mappingType = UV_MAPPING_PLANAR;
#endif
    ComputeLayerTexCoord3(  texCoord0, texCoord1, texCoord2, texCoord3,
                            positionWS, mappingType, _TexWorldScale3, layerTexCoord, _LayerTiling3 * tileObjectScale);
}

// This is call only in this file
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(FragInputs input, inout LayerTexCoord layerTexCoord)
{
    GetLayerTexCoord(   input.texCoord0, input.texCoord1, input.texCoord2, input.texCoord3,
                        input.positionWS, input.worldToTangent[2].xyz, layerTexCoord);
}
//
//void ApplyTessellationTileScale(inout float height0, inout float height1, inout float height2, inout float height3)
//{
//    // When we change the tiling, we have want to conserve the ratio with the displacement (and this is consistent with per pixel displacement)
//#ifdef _TESSELLATION_TILING_SCALE
//    float tileObjectScale = 1.0;
//    #ifdef _LAYER_TILING_COUPLED_WITH_UNIFORM_OBJECT_SCALE
//    // Extract scaling from world transform
//    float4x4 worldTransform = GetObjectToWorldMatrix();
//    // assuming uniform scaling, take only the first column
//    tileObjectScale = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
//    #endif
//
//    height0 /= _LayerTiling0 * max(_BaseColorMap0_ST.x, _BaseColorMap0_ST.y);
//    #if !defined(_MAIN_LAYER_INFLUENCE_MODE)
//    height0 *= tileObjectScale;  // We only affect layer0 in case we are not in influence mode (i.e we should not change the base object)
//    #endif
//    height1 /= tileObjectScale * _LayerTiling1 * max(_BaseColorMap1_ST.x, _BaseColorMap1_ST.y);
//    height2 /= tileObjectScale * _LayerTiling2 * max(_BaseColorMap2_ST.x, _BaseColorMap2_ST.y);
//    height3 /= tileObjectScale * _LayerTiling3 * max(_BaseColorMap3_ST.x, _BaseColorMap3_ST.y);
//#endif
//}

// This function is just syntaxic sugar to nullify height not used based on heightmap avaibility and layer
void SetEnabledHeightByLayer(inout float height0, inout float height1, inout float height2, inout float height3)
{
#ifndef _HEIGHTMAP0
    height0 = 0.0;
#endif
#ifndef _HEIGHTMAP1
    height1 = 0.0;
#endif
#ifndef _HEIGHTMAP2
    height2 = 0.0;
#endif
#ifndef _HEIGHTMAP3
    height3 = 0.0;
#endif

#if _LAYER_COUNT < 4
    height3 = 0.0;
#endif
#if _LAYER_COUNT < 3
    height2 = 0.0;
#endif
}

void ComputeMaskWeights(float4 inputMasks, out float outWeights[_MAX_LAYER])
{
    float masks[_MAX_LAYER];
#if defined(_DENSITY_MODE)
    masks[0] = inputMasks.a;
#else
    masks[0] = 1.0;
#endif
    masks[1] = inputMasks.r;
#if _LAYER_COUNT > 2
    masks[2] = inputMasks.g;
#else
    masks[2] = 0.0;
#endif
#if _LAYER_COUNT > 3
    masks[3] = inputMasks.b;
#else
    masks[3] = 0.0;
#endif

    // calculate weight of each layers
    // Algorithm is like this:
    // Top layer have priority on others layers
    // If a top layer doesn't use the full weight, the remaining can be use by the following layer.
    float weightsSum = 0.0;

    [unroll]
    for (int i = _LAYER_COUNT - 1; i >= 0; --i)
    {
        outWeights[i] = min(masks[i], (1.0 - weightsSum));
        weightsSum = saturate(weightsSum + masks[i]);
    }
}

// Caution: Blend mask are Layer 1 R - Layer 2 G - Layer 3 B - Main Layer A
float4 GetBlendMask(LayerTexCoord layerTexCoord, float4 vertexColor, bool useLodSampling = false, float lod = 0)
{
    // Caution:
    // Blend mask are Main Layer A - Layer 1 R - Layer 2 G - Layer 3 B
    // Value for main layer is not use for blending itself but for alternate weighting like density.
    // Settings this specific Main layer blend mask in alpha allow to be transparent in case we don't use it and 1 is provide by default.
    float4 blendMasks = useLodSampling ? SAMPLE_UVMAPPING_TEXTURE2D_LOD(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.blendMask, lod) : SAMPLE_UVMAPPING_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.blendMask);

#if defined(_LAYER_MASK_VERTEX_COLOR_MUL)
    blendMasks *= vertexColor;
#elif defined(_LAYER_MASK_VERTEX_COLOR_ADD)
    blendMasks = saturate(blendMasks + vertexColor * 2.0 - 1.0);
#endif

    return blendMasks;
}
//
//// Return the maximun amplitude use by all enabled heightmap
//// use for tessellation culling and per pixel displacement
//// TODO: For vertex displacement this should take into account the modification in ApplyTessellationTileScale but it should be conservative here (as long as tiling is not negative)
//float GetMaxDisplacement()
//{
//    float maxDisplacement = 0.0;
//
//#if defined(_HEIGHTMAP0)
//    maxDisplacement = max(  _LayerHeightAmplitude0, maxDisplacement);
//#endif
//
//#if defined(_HEIGHTMAP1)
//    maxDisplacement = max(  _LayerHeightAmplitude1
//                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
//                            +_LayerHeightAmplitude0 * _InheritBaseHeight1
//                            #endif
//                            , maxDisplacement);
//#endif
//
//#if _LAYER_COUNT >= 3
//#if defined(_HEIGHTMAP2)
//    maxDisplacement = max(  _LayerHeightAmplitude2
//                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
//                            +_LayerHeightAmplitude0 * _InheritBaseHeight2
//                            #endif
//                            , maxDisplacement);
//#endif
//#endif
//
//#if _LAYER_COUNT >= 4
//#if defined(_HEIGHTMAP3)
//    maxDisplacement = max(  _LayerHeightAmplitude3
//                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
//                            +_LayerHeightAmplitude0 * _InheritBaseHeight3
//                            #endif
//                            , maxDisplacement);
//#endif
//#endif
//
//    return maxDisplacement;
//}

#define FLT_MAX         3.402823466e+38 // Maximum representable floating-point number

// Return the minimun uv size for all layers including triplanar
float2 GetMinUvSize(LayerTexCoord layerTexCoord)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

#if defined(_HEIGHTMAP0)
    minUvSize = min(layerTexCoord.base0.uv * _HeightMap0_TexelSize.zw, minUvSize);
#endif

#if defined(_HEIGHTMAP1)
    minUvSize = min(layerTexCoord.base1.uv * _HeightMap1_TexelSize.zw, minUvSize);
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    minUvSize = min(layerTexCoord.base2.uv * _HeightMap2_TexelSize.zw, minUvSize);
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    minUvSize = min(layerTexCoord.base3.uv * _HeightMap3_TexelSize.zw, minUvSize);
#endif
#endif

    return minUvSize;
}
//
//struct PerPixelHeightDisplacementParam
//{
//    float weights[_MAX_LAYER];
//    float2 uv[_MAX_LAYER];
//    float mainHeightInfluence;
//};
//
//// Calculate displacement for per vertex displacement mapping
//float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
//{
//#if defined(_HEIGHTMAP0) || defined(_HEIGHTMAP1) || defined(_HEIGHTMAP2) || defined(_HEIGHTMAP3)
//    // Note: No multiply by amplitude here, this is bake into the weights and apply in BlendLayeredScalar
//    // The amplitude is normalize to be able to work with POM algorithm
//    // Tiling is automatically handled correctly here as we use 4 differents uv even if they come from the same UVSet (they include the tiling)
//    float height0 = SAMPLE_TEXTURE2D_LOD(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, param.uv[0] + texOffsetCurrent, lod).r;
//    float height1 = SAMPLE_TEXTURE2D_LOD(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, param.uv[1] + texOffsetCurrent, lod).r;
//    float height2 = SAMPLE_TEXTURE2D_LOD(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, param.uv[2] + texOffsetCurrent, lod).r;
//    float height3 = SAMPLE_TEXTURE2D_LOD(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, param.uv[3] + texOffsetCurrent, lod).r;
//    SetEnabledHeightByLayer(height0, height1, height2, height3);  // Not needed as already put in weights but paranoid mode
//    return BlendLayeredScalar(height0, height1, height2, height3, param.weights) + height0 * param.mainHeightInfluence;
//#else
//    return 0.0;
//#endif
//}
//
//#include "ShaderLibrary/PerPixelDisplacement.hlsl"
//
//// PPD is affecting only one mapping at the same time, mean we need to execute it for each mapping (UV0, UV1, 3 times for triplanar etc..)
//// We chose to not support all this case that are extremely hard to manage (for example mixing different mapping, mean it also require different tangent space that is not supported in Unity)
//// For these reasons we put the following rules
//// Rules:
//// - Mapping is the same for all layers that use an Heightmap (i.e all are UV, planar or triplanar)
//// - Mapping UV is UV0 only because we need to convert view vector in texture space and this is only available for UV0
//// - Heightmap can be enabled per layer
//// - Blend Mask use same mapping as main layer (UVO, Planar, Triplanar)
//// From these rules it mean that PPD is enable only if the user 1) ask for it, 2) if there is one heightmap enabled on active layer, 3) if mapping is the same for all layer respecting 2), 4) if mapping is UV0, planar or triplanar mapping
//// Most contraint are handled by the inspector (i.e the UI) like the mapping constraint and is assumed in the shader.
//float ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord)
//{
//    bool ppdEnable = false;
//    bool isPlanar = false;
//    bool isTriplanar = false;
//
//#ifdef _PER_PIXEL_DISPLACEMENT
//
//    // To know if we are planar or triplanar just need to check if any of the active heightmap layer is true as they are enforce to be the same mapping
//#if defined(_HEIGHTMAP0)
//    ppdEnable = true;
//    isPlanar = layerTexCoord.base0.mappingType == UV_MAPPING_PLANAR;
//    isTriplanar = layerTexCoord.base0.mappingType == UV_MAPPING_TRIPLANAR;
//#endif
//
//#if defined(_HEIGHTMAP1)
//    ppdEnable = true;
//    isPlanar = layerTexCoord.base1.mappingType == UV_MAPPING_PLANAR;
//    isTriplanar = layerTexCoord.base1.mappingType == UV_MAPPING_TRIPLANAR;
//#endif
//
//#if _LAYER_COUNT >= 3
//#if defined(_HEIGHTMAP2)
//    ppdEnable = true;
//    isPlanar = layerTexCoord.base2.mappingType == UV_MAPPING_PLANAR;
//    isTriplanar = layerTexCoord.base2.mappingType == UV_MAPPING_TRIPLANAR;
//#endif
//#endif
//
//#if _LAYER_COUNT >= 4
//#if defined(_HEIGHTMAP3)
//    ppdEnable = true;
//    isPlanar = layerTexCoord.base3.mappingType == UV_MAPPING_PLANAR;
//    isTriplanar = layerTexCoord.base3.mappingType == UV_MAPPING_TRIPLANAR;
//#endif
//#endif
//
//#endif // _PER_PIXEL_DISPLACEMENT
//
//    if (ppdEnable)
//    {
//        // Even if we use same mapping we can have different tiling. For per pixel displacement we will perform the ray marching with already tiled uv
//        float maxHeight = GetMaxDisplacement();
//        // Compute lod as we will sample inside a loop(so can't use regular sampling)
//        // Note: It appear that CALCULATE_TEXTURE2D_LOD only return interger lod. We want to use float lod to have smoother transition and fading, so do our own calculation.
//        // Approximation of lod to used. Be conservative here, we will take the highest mip of all layers.
//        // Remember, we assume that we used the same mapping for all layer, so only size matter.
//        float2 minUvSize = GetMinUvSize(layerTexCoord);
//        float lod = ComputeTextureLOD(minUvSize);
//
//        // Calculate blend weights
//        float4 blendMasks = GetBlendMask(layerTexCoord, input.color);
//
//        float weights[_MAX_LAYER];
//        ComputeMaskWeights(blendMasks, weights);
//
//        // Be sure we are not considering weight here were there is no heightmap
//        SetEnabledHeightByLayer(weights[0], weights[1], weights[2], weights[3]);
//
//        PerPixelHeightDisplacementParam ppdParam;
//#if defined(_MAIN_LAYER_INFLUENCE_MODE)
//        // For per pixel displacement we need to have normalized height scale to calculate the interesection (required by the algorithm we use)
//        // mean that we will normalize by the highest amplitude.
//        // We store this normalization factor with the weights as it will be multiply by the readed height.
//        ppdParam.weights[0] = weights[0] * (_LayerHeightAmplitude0) / maxHeight;
//        ppdParam.weights[1] = weights[1] * (_LayerHeightAmplitude1 + _LayerHeightAmplitude0 * _InheritBaseHeight1) / maxHeight;
//        ppdParam.weights[2] = weights[2] * (_LayerHeightAmplitude2 + _LayerHeightAmplitude0 * _InheritBaseHeight2) / maxHeight;
//        ppdParam.weights[3] = weights[3] * (_LayerHeightAmplitude3 + _LayerHeightAmplitude0 * _InheritBaseHeight3) / maxHeight;
//
//        // Think that inheritbasedheight will be 0 if height0 is fully visible in weights. So there is no double contribution of height0
//        float mainHeightInfluence = BlendLayeredScalar(0.0, _InheritBaseHeight1, _InheritBaseHeight2, _InheritBaseHeight3, weights);
//        ppdParam.mainHeightInfluence = mainHeightInfluence;
//#else
//        [unroll]
//        for (int i = 0; i < _MAX_LAYER; ++i)
//        {
//            ppdParam.weights[i] = weights[i];
//        }
//        ppdParam.mainHeightInfluence = 0.0;
//#endif
//
//        float height; // final height processed
//        float NdotV;
//
//        // We need to calculate the texture space direction. It depends on the mapping.
//        if (isTriplanar)
//        {
//            // TODO: implement. Require 3 call to POM + dedicated viewDirTS based on triplanar convention
//            // apply the 3 offset on all layers
//            /*
//
//            ppdParam.uv[0] = layerTexCoord.base0.uvZY;
//            ppdParam.uv[1] = layerTexCoord.base1.uvYZ;
//            ppdParam.uv[2] = layerTexCoord.base2.uvYZ;
//            ppdParam.uv[3] = layerTexCoord.base3.uvYZ;
//
//            float3 viewDirTS = ;
//            int numSteps = (int)lerp(_PPDMaxSamples, _PPDMinSamples, abs(viewDirTS.z));
//            ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirTS, maxHeight, ppdParam);
//
//            // Apply to all uvZY
//
//            // Repeat for uvXZ
//
//            // Repeat for uvXY
//
//            // Apply to all layer that used triplanar
//            */
//            height = 1;
//            NdotV  = 1;
//        }
//        else
//        {
//            ppdParam.uv[0] = layerTexCoord.base0.uv;
//            ppdParam.uv[1] = layerTexCoord.base1.uv;
//            ppdParam.uv[2] = layerTexCoord.base2.uv;
//            ppdParam.uv[3] = layerTexCoord.base3.uv;
//
//            float3x3 worldToTangent = input.worldToTangent;
//
//            // Note: The TBN is not normalize as it is based on mikkt. We should normalize it, but POM is always use on simple enough surfarce that mean it is not required (save 2 normalize). Tag: SURFACE_GRADIENT
//            // For planar the view vector is the world view vector (unless we want to support object triplanar ? and in this case used TransformWorldToObject)
//            // TODO: do we support object triplanar ? See ComputeLayerTexCoord
//            float3 viewDirTS = isPlanar ? float3(-V.xz, V.y) : TransformWorldToTangent(V, worldToTangent);
//            NdotV = viewDirTS.z;
//
//            int numSteps = (int)lerp(_PPDMaxSamples, _PPDMinSamples, viewDirTS.z);
//
//            float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirTS, maxHeight, ppdParam, height);
//
//            // Apply offset to all planar UV if applicable
//            float4 planarWeight = float4(   layerTexCoord.base0.mappingType == UV_MAPPING_PLANAR ? 1.0 : 0.0,
//                                            layerTexCoord.base1.mappingType == UV_MAPPING_PLANAR ? 1.0 : 0.0,
//                                            layerTexCoord.base2.mappingType == UV_MAPPING_PLANAR ? 1.0 : 0.0,
//                                            layerTexCoord.base3.mappingType == UV_MAPPING_PLANAR ? 1.0 : 0.0);
//
//            // _UVMappingMask0.x will be 1.0 is UVSet0 is used;
//            float4 offsetWeights = isPlanar ? planarWeight : float4(_UVMappingMask0.x, _UVMappingMask1.x, _UVMappingMask2.x, _UVMappingMask3.x);
//
//            layerTexCoord.base0.uv += offsetWeights.x * offset;
//            layerTexCoord.base1.uv += offsetWeights.y * offset;
//            layerTexCoord.base2.uv += offsetWeights.z * offset;
//            layerTexCoord.base3.uv += offsetWeights.w * offset;
//
//            offsetWeights = isPlanar ? planarWeight : float4(_UVDetailsMappingMask0.x, _UVDetailsMappingMask1.x, _UVDetailsMappingMask2.x, _UVDetailsMappingMask3.x);
//
//            layerTexCoord.details0.uv += offsetWeights.x * offset;
//            layerTexCoord.details1.uv += offsetWeights.y * offset;
//            layerTexCoord.details2.uv += offsetWeights.z * offset;
//            layerTexCoord.details3.uv += offsetWeights.w * offset;
//        }
//
//        // Since POM "pushes" geometry inwards (rather than extrude it), { height = height - 1 }.
//        // Since the result is used as a 'depthOffsetVS', it needs to be positive, so we flip the sign.
//        float verticalDisplacement = maxHeight - height * maxHeight;
//        // IDEA: precompute the tiling scale? MOV-MUL vs MOV-MOV-MAX-RCP-MUL.
//        float tilingScale = rcp(max(_BaseColorMap0_ST.x, _BaseColorMap0_ST.y));
//        return tilingScale * verticalDisplacement / NdotV;
//    }
//
//    return 0.0;
//}
//
//// Calculate displacement for per vertex displacement mapping
//float ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
//{
//    float4 blendMasks = GetBlendMask(layerTexCoord, vertexColor, true, lod);
//
//    float weights[_MAX_LAYER];
//    ComputeMaskWeights(blendMasks, weights);
//
//#if defined(_HEIGHTMAP0) || defined(_HEIGHTMAP1) || defined(_HEIGHTMAP2) || defined(_HEIGHTMAP3)
//    float height0 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base0, lod).r - _LayerCenterOffset0) * _LayerHeightAmplitude0;
//    float height1 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base1, lod).r - _LayerCenterOffset1) * _LayerHeightAmplitude1;
//    float height2 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base2, lod).r - _LayerCenterOffset2) * _LayerHeightAmplitude2;
//    float height3 = (SAMPLE_UVMAPPING_TEXTURE2D_LOD(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base3, lod).r - _LayerCenterOffset3) * _LayerHeightAmplitude3;
//    ApplyTessellationTileScale(height0, height1, height2, height3); // Only apply with per vertex displacement
//    SetEnabledHeightByLayer(height0, height1, height2, height3);
//    float heightResult = BlendLayeredScalar(height0, height1, height2, height3, weights);
//
//#if defined(_MAIN_LAYER_INFLUENCE_MODE)
//    // Think that inheritbasedheight will be 0 if height0 is fully visible in weights. So there is no double contribution of height0
//    float inheritBaseHeight = BlendLayeredScalar(0.0, _InheritBaseHeight1, _InheritBaseHeight2, _InheritBaseHeight3, weights);
//    return heightResult + height0 * inheritBaseHeight;
//#endif
//
//#else
//    float heightResult = 0.0;
//#endif
//    return heightResult;
//}

float3 ApplyHeightBasedBlend(float3 inputMask, float3 inputHeight, float3 blendUsingHeight)
{
    return saturate(lerp(inputMask * inputHeight * blendUsingHeight * 100, 1, inputMask * inputMask)); // 100 arbitrary scale to limit blendUsingHeight values.
}

// Calculate weights to apply to each layer
// Caution: This function must not be use for per vertex/pixel displacement, there is a dedicated function for them.
// This function handle triplanar
void ComputeLayerWeights(FragInputs input, LayerTexCoord layerTexCoord, float4 inputAlphaMask, out float outWeights[_MAX_LAYER])
{
    float4 blendMasks = GetBlendMask(layerTexCoord, input.color);

#if defined(_DENSITY_MODE)
    // Note: blendMasks.argb because a is main layer
    float4 minOpaParam = float4(_MinimumOpacity0, _MinimumOpacity1, _MinimumOpacity2, _MinimumOpacity3);
    float4 remapedOpacity = lerp(minOpaParam, float4(1.0, 1.0, 1.0, 1.0), inputAlphaMask); // Remap opacity mask from [0..1] to [minOpa..1]
    float4 opacityAsDensity = saturate((inputAlphaMask - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks.argb)) * 20.0);

    float4 useOpacityAsDensityParam = float4(_OpacityAsDensity0, _OpacityAsDensity1, _OpacityAsDensity2, _OpacityAsDensity3);
    blendMasks.argb = lerp(blendMasks.argb * remapedOpacity, opacityAsDensity, useOpacityAsDensityParam);
#endif

#if defined(_HEIGHT_BASED_BLEND)

#if defined(_HEIGHTMAP0) || defined(_HEIGHTMAP1) || defined(_HEIGHTMAP2) || defined(_HEIGHTMAP3)
    float height0 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base0).r - _LayerCenterOffset0) * _LayerHeightAmplitude0;
    float height1 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base1).r - _LayerCenterOffset1) * _LayerHeightAmplitude1;
    float height2 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base2).r - _LayerCenterOffset2) * _LayerHeightAmplitude2;
    float height3 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base3).r - _LayerCenterOffset3) * _LayerHeightAmplitude3;
    SetEnabledHeightByLayer(height0, height1, height2, height3);
    float4 heights = float4(height0, height1, height2, height3);

    // HACK: use height0 to avoid compiler error for unused sampler - To remove when we can have a sampler without a textures
    #if !defined(_PER_PIXEL_DISPLACEMENT)
    // We don't use height 0 for the height blend based mode
    heights.y += (heights.x * 0.0001);
    #endif
#else
    float4 heights = float4(0.0, 0.0, 0.0, 0.0);
#endif

    // don't apply on main layer
    blendMasks.rgb = ApplyHeightBasedBlend(blendMasks.rgb, heights.yzw, float3(_BlendUsingHeight1, _BlendUsingHeight2, _BlendUsingHeight3));
#endif

    ComputeMaskWeights(blendMasks, outWeights);
}

float3 ComputeMainNormalInfluence(FragInputs input, float3 normalTS0, float3 normalTS1, float3 normalTS2, float3 normalTS3, LayerTexCoord layerTexCoord, float weights[_MAX_LAYER])
{
    // Get our regular normal from regular layering
    float3 normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);

    // THen get Main Layer Normal influence factor. Main layer is 0 because it can't be influence. In this case the final lerp return normalTS.
    float influenceFactor = BlendLayeredScalar(0.0, _InheritBaseNormal1, _InheritBaseNormal2, _InheritBaseNormal3, weights);
    // We will add smoothly the contribution of the normal map by using lower mips with help of bias sampling. InfluenceFactor must be [0..numMips] // Caution it cause banding...
    // Note: that we don't take details map into account here.
    float maxMipBias = log2(max(_NormalMap0_TexelSize.z, _NormalMap0_TexelSize.w)); // don't do + 1 as it is for bias, not lod
    float3 mainNormalTS = GetNormalTS0(input, layerTexCoord, float3(0.0, 0.0, 1.0), 0.0, true, maxMipBias * (1.0 - influenceFactor));

    // Add on our regular normal a bit of Main Layer normal base on influence factor. Note that this affect only the "visible" normal.
    return lerp(normalTS, BlendNormalRNM(normalTS, mainNormalTS), influenceFactor);
}


float3 ComputeMainBaseColorInfluence(float3 baseColor0, float3 baseColor1, float3 baseColor2, float3 baseColor3, float compoMask, LayerTexCoord layerTexCoord, float weights[_MAX_LAYER])
{
    float3 baseColor = BlendLayeredVector3(baseColor0, baseColor1, baseColor2, baseColor3, weights);

    float influenceFactor = BlendLayeredScalar(0.0, _InheritBaseColor1, _InheritBaseColor2, _InheritBaseColor3, weights);
    float influenceThreshold = BlendLayeredScalar(1.0, _InheritBaseColorThreshold1, _InheritBaseColorThreshold2, _InheritBaseColorThreshold3, weights);

    influenceFactor = influenceFactor * (1.0 - saturate(compoMask / influenceThreshold));

    // We want to calculate the mean color of the texture. For this we will sample a low mipmap
    float textureBias = 15.0; // Use maximum bias
    float3 baseMeanColor0 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap0, sampler_BaseColorMap0, layerTexCoord.base0, textureBias).rgb *_BaseColor0.rgb;
    float3 baseMeanColor1 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap1, sampler_BaseColorMap0, layerTexCoord.base1, textureBias).rgb *_BaseColor1.rgb;
    float3 baseMeanColor2 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap2, sampler_BaseColorMap0, layerTexCoord.base2, textureBias).rgb *_BaseColor2.rgb;
    float3 baseMeanColor3 = SAMPLE_UVMAPPING_TEXTURE2D_BIAS(_BaseColorMap3, sampler_BaseColorMap0, layerTexCoord.base3, textureBias).rgb *_BaseColor3.rgb;

    float3 meanColor = BlendLayeredVector3(baseMeanColor0, baseMeanColor1, baseMeanColor2, baseMeanColor3, weights);

    // If we inherit from base layer, we will add a bit of it
    // We add variance of current visible level and the base color 0 or mean (to retrieve initial color) depends on influence
    // (baseColor - meanColor) + lerp(meanColor, baseColor0, inheritBaseColor) simplify to
    // saturate(influenceFactor * (baseColor0 - meanColor) + baseColor);
    // There is a special case when baseColor < meanColor to avoid getting negative values.
    float3 factor = baseColor > meanColor ? (baseColor0 - meanColor) : (baseColor0 * baseColor / meanColor - baseColor);
    return influenceFactor * factor + baseColor;
}

#define ZERO_INITIALIZE(type, name) name = (type)0;

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, out SurfaceData surfaceData)
{
#ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
    LODDitheringTransition(input.unPositionSS, unity_LODFade.y); // Note that we pass the quantized value of LOD fade
#endif

    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

//    float depthOffset = ApplyPerPixelDisplacement(input, V, layerTexCoord);

//#ifdef _DEPTHOFFSET_ON
//    ApplyDepthOffsetPositionInput(V, depthOffset, GetWorldToHClipMatrix(), posInput);
//#endif

    SurfaceData surfaceData0, surfaceData1, surfaceData2, surfaceData3;
    float3 normalTS0, normalTS1, normalTS2, normalTS3;
    float alpha0 = GetSurfaceData0(input, layerTexCoord, surfaceData0, normalTS0);
    float alpha1 = GetSurfaceData1(input, layerTexCoord, surfaceData1, normalTS1);
    float alpha2 = GetSurfaceData2(input, layerTexCoord, surfaceData2, normalTS2);
    float alpha3 = GetSurfaceData3(input, layerTexCoord, surfaceData3, normalTS3);

    // Note: If per pixel displacement is enabled it mean we will fetch again the various heightmaps at the intersection location. Not sure the compiler can optimize.
    float weights[_MAX_LAYER];
    ComputeLayerWeights(input, layerTexCoord, float4(alpha0, alpha1, alpha2, alpha3), weights);

    // For layered shader, alpha of base color is used as either an opacity mask, a composition mask for inheritance parameters or a density mask.
    float alpha = PROP_BLEND_SCALAR(alpha, weights);

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

#if defined(_MAIN_LAYER_INFLUENCE_MODE)
    surfaceData.baseColor = ComputeMainBaseColorInfluence(surfaceData0.baseColor, surfaceData1.baseColor, surfaceData2.baseColor, surfaceData3.baseColor, alpha0, layerTexCoord, weights);
    float3 normalTS = ComputeMainNormalInfluence(input, normalTS0, normalTS1, normalTS2, normalTS3, layerTexCoord, weights);
#else
    surfaceData.baseColor = SURFACEDATA_BLEND_VECTOR3(surfaceData, baseColor, weights);
    float3 normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);
#endif

    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT
    // Init other parameters
    surfaceData.materialId = 1; // MaterialId.LitStandard
    surfaceData.anisotropy = 0;
    surfaceData.specular = 0.04;
    surfaceData.subsurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subsurfaceProfile = 0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

    surfaceData.normalWS = normalize(mul(input.worldToTangent, normalTS));

    // Done one time for all layered - cumulate with spec occ alpha for now
    surfaceData.specularOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, specularOcclusion, weights);

    //GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
}

//#ifdef TESSELLATION_ON
//#include "LitTessellation.hlsl" // Must be after GetLayerTexCoord() declaration
//#endif
//
//struct SurfaceData
//{
//    float3 baseColor;
//    float specularOcclusion;
//    float3 normalWS;
//    float perceptualSmoothness;
//    int materialId;
//    float ambientOcclusion;
//    float3 tangentWS;
//    float anisotropy;
//    float metallic;
//    float specular;
//    float subsurfaceRadius;
//    float thickness;
//    int subsurfaceProfile;
//    float3 specularColor;
//};


// parallax transformed texcoord is used to sample occlusion
inline FragmentCommonData FragmentSetupLayeredPhotogrammetry (float4 posCS, inout float4 i_tex, float4 i_tex2, float4 i_color, half3 i_eyeVec, half4 tangentToWorld[3], half3 i_posWorld)
{
    FragInputs fragInputs;
    fragInputs.unPositionSS = posCS;
    fragInputs.positionWS = i_posWorld;
    fragInputs.texCoord0 = i_tex.xy;
    fragInputs.texCoord1 = i_tex.zw;
    fragInputs.texCoord2 = i_tex2.xy;
    fragInputs.texCoord3 = i_tex2.zw;
    fragInputs.color = i_color;
    fragInputs.worldToTangent = float3x3(
                                            float3(tangentToWorld[0].x, tangentToWorld[1].x, tangentToWorld[2].x),
                                            float3(tangentToWorld[0].y, tangentToWorld[1].y, tangentToWorld[2].y),
                                            float3(tangentToWorld[0].z, tangentToWorld[1].z, tangentToWorld[2].z)
                                        );

    fragInputs.isFrontFace = true;

    SurfaceData surfaceData;
    GetSurfaceAndBuiltinData(fragInputs, i_eyeVec, surfaceData);

    float3 diffColor = surfaceData.baseColor * (1.0 - surfaceData.metallic);
    float3 specColor = lerp(surfaceData.specular.xxx, surfaceData.baseColor, surfaceData.metallic);

    half oneMinusReflectivity;
    diffColor = EnergyConservationBetweenDiffuseAndSpecular (diffColor, specColor, /*out*/ oneMinusReflectivity);

    FragmentCommonData o;
    o.diffColor = diffColor;
    o.specColor = specColor;
    o.oneMinusReflectivity = oneMinusReflectivity;
    o.smoothness = surfaceData.perceptualSmoothness;
    o.normalWorld = surfaceData.normalWS;
    o.eyeVec = NormalizePerPixelNormal(i_eyeVec);
    o.posWorld = i_posWorld;

    //// NOTE: shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
    //o.diffColor = PreMultiplyAlpha (o.diffColor, alpha, o.oneMinusReflectivity, /*out*/ o.alpha);
    return o;
}
// END LAYERED_PHOTOGRAMMETRY

inline UnityGI FragmentGI (FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light, bool reflections)
{
    UnityGIInput d;
    d.light = light;
    d.worldPos = s.posWorld;
    d.worldViewDir = -s.eyeVec;
    d.atten = atten;
    #if defined(LIGHTMAP_ON) || defined(DYNAMICLIGHTMAP_ON)
        d.ambient = 0;
        d.lightmapUV = i_ambientOrLightmapUV;
    #else
        d.ambient = i_ambientOrLightmapUV.rgb;
        d.lightmapUV = 0;
    #endif

    d.probeHDR[0] = unity_SpecCube0_HDR;
    d.probeHDR[1] = unity_SpecCube1_HDR;
    #if defined(UNITY_SPECCUBE_BLENDING) || defined(UNITY_SPECCUBE_BOX_PROJECTION)
      d.boxMin[0] = unity_SpecCube0_BoxMin; // .w holds lerp value for blending
    #endif
    #ifdef UNITY_SPECCUBE_BOX_PROJECTION
      d.boxMax[0] = unity_SpecCube0_BoxMax;
      d.probePosition[0] = unity_SpecCube0_ProbePosition;
      d.boxMax[1] = unity_SpecCube1_BoxMax;
      d.boxMin[1] = unity_SpecCube1_BoxMin;
      d.probePosition[1] = unity_SpecCube1_ProbePosition;
    #endif

    if(reflections)
    {
        Unity_GlossyEnvironmentData g = UnityGlossyEnvironmentSetup(s.smoothness, -s.eyeVec, s.normalWorld, s.specColor);
        // Replace the reflUVW if it has been compute in Vertex shader. Note: the compiler will optimize the calcul in UnityGlossyEnvironmentSetup itself
        #if UNITY_STANDARD_SIMPLE
            g.reflUVW = s.reflUVW;
        #endif

        return UnityGlobalIllumination (d, occlusion, s.normalWorld, g);
    }
    else
    {
        return UnityGlobalIllumination (d, occlusion, s.normalWorld);
    }
}

inline UnityGI FragmentGI (FragmentCommonData s, half occlusion, half4 i_ambientOrLightmapUV, half atten, UnityLight light)
{
    return FragmentGI(s, occlusion, i_ambientOrLightmapUV, atten, light, true);
}


//-------------------------------------------------------------------------------------
half4 OutputForward (half4 output, half alphaFromSurface)
{
    #if defined(_ALPHABLEND_ON) || defined(_ALPHAPREMULTIPLY_ON)
        output.a = alphaFromSurface;
    #else
        UNITY_OPAQUE_ALPHA(output.a);
    #endif
    return output;
}

inline half4 VertexGIForward(VertexInput v, float3 posWorld, half3 normalWorld)
{
    half4 ambientOrLightmapUV = 0;
    // Static lightmaps
    #ifdef LIGHTMAP_ON
        ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
        ambientOrLightmapUV.zw = 0;
    // Sample light probe for Dynamic objects only (no static or dynamic lightmaps)
    #elif UNITY_SHOULD_SAMPLE_SH
        #ifdef VERTEXLIGHT_ON
            // Approximated illumination from non-important point lights
            ambientOrLightmapUV.rgb = Shade4PointLights (
                unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
                unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
                unity_4LightAtten0, posWorld, normalWorld);
        #endif

        ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, ambientOrLightmapUV.rgb);
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    return ambientOrLightmapUV;
}

// ------------------------------------------------------------------
//  Base forward pass (directional light, emission, lightmaps, ...)

struct VertexOutputForwardBase
{
    UNITY_POSITION(pos);
    float4 tex                          : TEXCOORD0;
    half3 eyeVec                        : TEXCOORD1;
    half4 tangentToWorldAndPackedData[3]    : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax or worldPos]
    half4 ambientOrLightmapUV           : TEXCOORD5;    // SH or Lightmap UV

    UNITY_SHADOW_COORDS(6)
    UNITY_FOG_COORDS(7)

    // next ones would not fit into SM2.0 limits, but they are always for SM3.0+
    #if UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT
        float3 posWorld                 : TEXCOORD8;
    #endif

    // BEGIN LAYERED_PHOTOGRAMMETRY
    float2 texUV3                       : TEXCOORD9;
    float4 color                        : TEXCOORD10;
    // END LAYERED_PHOTOGRAMMETRY

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardBase vertForwardBase (VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputForwardBase o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardBase, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    #if UNITY_REQUIRE_FRAG_WORLDPOS
        #if UNITY_PACK_WORLDPOS_WITH_TANGENT
            o.tangentToWorldAndPackedData[0].w = posWorld.x;
            o.tangentToWorldAndPackedData[1].w = posWorld.y;
            o.tangentToWorldAndPackedData[2].w = posWorld.z;
        #else
            o.posWorld = posWorld.xyz;
        #endif
    #endif
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = TexCoords(v);
    o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndPackedData[0].xyz = 0;
        o.tangentToWorldAndPackedData[1].xyz = 0;
        o.tangentToWorldAndPackedData[2].xyz = normalWorld;
    #endif

    //We need this for shadow receving
    UNITY_TRANSFER_SHADOW(o, v.uv1);

    o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

    #ifdef _PARALLAXMAP
        TANGENT_SPACE_ROTATION;
        half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
        o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
        o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
        o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
    #endif

    UNITY_TRANSFER_FOG(o,o.pos);

    // BEGIN LAYERED_PHOTOGRAMMETRY
    o.texUV3 = v.uv3;
    o.color = v.color;
    // END LAYERED_PHOTOGRAMMETRY
    return o;
}

half4 fragForwardBaseInternal (VertexOutputForwardBase i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FRAGMENT_SETUP(s)

    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

    UnityLight mainLight = MainLight ();
    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);

    // JIG CHECK
    half occlusion = 1.0;// Occlusion(i.tex.xy);
    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

    half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
    //c.rgb += Emission(i.tex.xy);

    UNITY_APPLY_FOG(i.fogCoord, c.rgb);

    return OutputForward (c, s.alpha);
}

half4 fragForwardBase (VertexOutputForwardBase i) : SV_Target   // backward compatibility (this used to be the fragment entry function)
{
    return fragForwardBaseInternal(i);
}

// ------------------------------------------------------------------
//  Additive forward pass (one light per pass)

struct VertexOutputForwardAdd
{
    UNITY_POSITION(pos);
    float4 tex                          : TEXCOORD0;
    half3 eyeVec                        : TEXCOORD1;
    half4 tangentToWorldAndLightDir[3]  : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:lightDir]
    float3 posWorld                     : TEXCOORD5;
    UNITY_SHADOW_COORDS(6)
    UNITY_FOG_COORDS(7)

    // next ones would not fit into SM2.0 limits, but they are always for SM3.0+
#if defined(_PARALLAXMAP)
    half3 viewDirForParallax            : TEXCOORD8;
#endif

    // BEGIN LAYERED_PHOTOGRAMMETRY
    float2 texUV3                       : TEXCOORD9;
    float4 color                        : TEXCOORD10;
    // END LAYERED_PHOTOGRAMMETRY

    UNITY_VERTEX_OUTPUT_STEREO
};

VertexOutputForwardAdd vertForwardAdd (VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputForwardAdd o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardAdd, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = TexCoords(v);
    o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    o.posWorld = posWorld.xyz;
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndLightDir[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndLightDir[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndLightDir[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndLightDir[0].xyz = 0;
        o.tangentToWorldAndLightDir[1].xyz = 0;
        o.tangentToWorldAndLightDir[2].xyz = normalWorld;
    #endif
    //We need this for shadow receiving
    UNITY_TRANSFER_SHADOW(o, v.uv1);

    float3 lightDir = _WorldSpaceLightPos0.xyz - posWorld.xyz * _WorldSpaceLightPos0.w;
    #ifndef USING_DIRECTIONAL_LIGHT
        lightDir = NormalizePerVertexNormal(lightDir);
    #endif
    o.tangentToWorldAndLightDir[0].w = lightDir.x;
    o.tangentToWorldAndLightDir[1].w = lightDir.y;
    o.tangentToWorldAndLightDir[2].w = lightDir.z;

    #ifdef _PARALLAXMAP
        TANGENT_SPACE_ROTATION;
        o.viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
    #endif

    UNITY_TRANSFER_FOG(o,o.pos);

    // BEGIN LAYERED_PHOTOGRAMMETRY
    o.texUV3 = v.uv3;
    o.color = v.color;
    // END LAYERED_PHOTOGRAMMETRY

    return o;
}

half4 fragForwardAddInternal (VertexOutputForwardAdd i)
{
    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FRAGMENT_SETUP_FWDADD(s)

    UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
    UnityLight light = AdditiveLight (IN_LIGHTDIR_FWDADD(i), atten);
    UnityIndirect noIndirect = ZeroIndirect ();

    half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, noIndirect);

    UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
    return OutputForward (c, s.alpha);
}

half4 fragForwardAdd (VertexOutputForwardAdd i) : SV_Target     // backward compatibility (this used to be the fragment entry function)
{
    return fragForwardAddInternal(i);
}

// ------------------------------------------------------------------
//  Deferred pass

struct VertexOutputDeferred
{
    UNITY_POSITION(pos);
    float4 tex                          : TEXCOORD0;
    half3 eyeVec                        : TEXCOORD1;
    half4 tangentToWorldAndPackedData[3]: TEXCOORD2;    // [3x3:tangentToWorld | 1x3:viewDirForParallax or worldPos]
    half4 ambientOrLightmapUV           : TEXCOORD5;    // SH or Lightmap UVs

    #if UNITY_REQUIRE_FRAG_WORLDPOS && !UNITY_PACK_WORLDPOS_WITH_TANGENT
        float3 posWorld                     : TEXCOORD6;
    #endif

    // BEGIN LAYERED_PHOTOGRAMMETRY
    float2 texUV3                       : TEXCOORD7;
    float4 color                        : TEXCOORD8;
    // END LAYERED_PHOTOGRAMMETRY

    UNITY_VERTEX_OUTPUT_STEREO
};


VertexOutputDeferred vertDeferred (VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputDeferred o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputDeferred, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    #if UNITY_REQUIRE_FRAG_WORLDPOS
        #if UNITY_PACK_WORLDPOS_WITH_TANGENT
            o.tangentToWorldAndPackedData[0].w = posWorld.x;
            o.tangentToWorldAndPackedData[1].w = posWorld.y;
            o.tangentToWorldAndPackedData[2].w = posWorld.z;
        #else
            o.posWorld = posWorld.xyz;
        #endif
    #endif
    o.pos = UnityObjectToClipPos(v.vertex);

    o.tex = TexCoords(v);
    o.eyeVec = NormalizePerVertexNormal(posWorld.xyz - _WorldSpaceCameraPos);
    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndPackedData[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndPackedData[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndPackedData[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndPackedData[0].xyz = 0;
        o.tangentToWorldAndPackedData[1].xyz = 0;
        o.tangentToWorldAndPackedData[2].xyz = normalWorld;
    #endif

    o.ambientOrLightmapUV = 0;
    #ifdef LIGHTMAP_ON
        o.ambientOrLightmapUV.xy = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #elif UNITY_SHOULD_SAMPLE_SH
        o.ambientOrLightmapUV.rgb = ShadeSHPerVertex (normalWorld, o.ambientOrLightmapUV.rgb);
    #endif
    #ifdef DYNAMICLIGHTMAP_ON
        o.ambientOrLightmapUV.zw = v.uv2.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
    #endif

    #ifdef _PARALLAXMAP
        TANGENT_SPACE_ROTATION;
        half3 viewDirForParallax = mul (rotation, ObjSpaceViewDir(v.vertex));
        o.tangentToWorldAndPackedData[0].w = viewDirForParallax.x;
        o.tangentToWorldAndPackedData[1].w = viewDirForParallax.y;
        o.tangentToWorldAndPackedData[2].w = viewDirForParallax.z;
    #endif

    // BEGIN LAYERED_PHOTOGRAMMETRY
    o.texUV3 = v.uv3;
    o.color = v.color;
    // END LAYERED_PHOTOGRAMMETRY

    return o;
}

void fragDeferred (
    VertexOutputDeferred i,
    out half4 outGBuffer0 : SV_Target0,
    out half4 outGBuffer1 : SV_Target1,
    out half4 outGBuffer2 : SV_Target2,
    out half4 outEmission : SV_Target3          // RT3: emission (rgb), --unused-- (a)
#if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
    ,out half4 outShadowMask : SV_Target4       // RT4: shadowmask (rgba)
#endif
)
{
    #if (SHADER_TARGET < 30)
        outGBuffer0 = 1;
        outGBuffer1 = 1;
        outGBuffer2 = 0;
        outEmission = 0;
        #if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
            outShadowMask = 1;
        #endif
        return;
    #endif

    UNITY_APPLY_DITHER_CROSSFADE(i.pos.xy);

    FRAGMENT_SETUP(s)

    // no analytic lights in this pass
    UnityLight dummyLight = DummyLight ();
    half atten = 1;

    // only GI
    // JIG CHECK
    half occlusion = 1.0f; // Occlusion(i.tex.xy);
#if UNITY_ENABLE_REFLECTION_BUFFERS
    bool sampleReflectionsInDeferred = false;
#else
    bool sampleReflectionsInDeferred = true;
#endif

    UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

    half3 emissiveColor = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;

    #ifdef _EMISSION
        emissiveColor += Emission (i.tex.xy);
    #endif

    #ifndef UNITY_HDR_ON
        emissiveColor.rgb = exp2(-emissiveColor.rgb);
    #endif

    UnityStandardData data;
    data.diffuseColor   = s.diffColor;
    data.occlusion      = occlusion;
    data.specularColor  = s.specColor;
    data.smoothness     = s.smoothness;
    data.normalWorld    = s.normalWorld;

    UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);

    // Emissive lighting buffer
    outEmission = half4(emissiveColor, 1);

    // Baked direct lighting occlusion if any
    #if defined(SHADOWS_SHADOWMASK) && (UNITY_ALLOWED_MRT_COUNT > 4)
        outShadowMask = UnityGetRawBakedOcclusions(i.ambientOrLightmapUV.xy, IN_WORLDPOS(i));
    #endif
}


//
// Old FragmentGI signature. Kept only for backward compatibility and will be removed soon
//

inline UnityGI FragmentGI(
    float3 posWorld,
    half occlusion, half4 i_ambientOrLightmapUV, half atten, half smoothness, half3 normalWorld, half3 eyeVec,
    UnityLight light,
    bool reflections)
{
    // we init only fields actually used
    FragmentCommonData s = (FragmentCommonData)0;
    s.smoothness = smoothness;
    s.normalWorld = normalWorld;
    s.eyeVec = eyeVec;
    s.posWorld = posWorld;
    return FragmentGI(s, occlusion, i_ambientOrLightmapUV, atten, light, reflections);
}
inline UnityGI FragmentGI (
    float3 posWorld,
    half occlusion, half4 i_ambientOrLightmapUV, half atten, half smoothness, half3 normalWorld, half3 eyeVec,
    UnityLight light)
{
    return FragmentGI (posWorld, occlusion, i_ambientOrLightmapUV, atten, smoothness, normalWorld, eyeVec, light, true);
}

#endif // UNITY_STANDARD_CORE_INCLUDED
