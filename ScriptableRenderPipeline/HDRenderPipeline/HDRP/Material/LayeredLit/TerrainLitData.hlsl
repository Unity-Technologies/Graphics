//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

#include "../Lit/LitData.hlsl"

#if defined(_TERRAINLIT_4_LAYERS)
    #define _LAYER_COUNT 4
#elif defined(_TERRAINLIT_3_LAYERS)
    #define _LAYER_COUNT 3
#elif defined(_TERRAINLIT_2_LAYERS)
    #define _LAYER_COUNT 2
#else
    #define _LAYER_COUNT 1
#endif

#define LAYERS_HEIGHTMAP_ENABLE (defined(_HEIGHTMAP0) || defined(_HEIGHTMAP1) || (_LAYER_COUNT > 2 && defined(_HEIGHTMAP2)) || (_LAYER_COUNT > 3 && defined(_HEIGHTMAP3)))

TEXTURE2D(_Splat0);
TEXTURE2D(_Normal0);
float4 _Splat0_ST;
TEXTURE2D(_Splat1);
TEXTURE2D(_Normal1);
float4 _Splat1_ST;
TEXTURE2D(_Splat2);
TEXTURE2D(_Normal2);
float4 _Splat2_ST;
TEXTURE2D(_Splat3);
TEXTURE2D(_Normal3);
float4 _Splat3_ST;
SAMPLER(sampler_Splat0);

#define _BaseColor0 float4(1,1,1,1)
#define _BaseColor1 float4(1,1,1,1)
#define _BaseColor2 float4(1,1,1,1)
#define _BaseColor3 float4(1,1,1,1)
#define _NormalScale0 1
#define _NormalScale1 1
#define _NormalScale2 1
#define _NormalScale3 1

// Number of sampler are limited, we need to share sampler as much as possible with lit material
// for this we put the constraint that the sampler are the same in a layered material for all textures of the same type
// then we take the sampler matching the first textures use of this type
#define SAMPLER_NORMALMAP_IDX sampler_Splat0

#if defined(_MASKMAP0)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap0
#elif defined(_MASKMAP1)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap1
#elif defined(_MASKMAP2)
#define SAMPLER_MASKMAP_IDX sampler_MaskMap2
#else
#define SAMPLER_MASKMAP_IDX sampler_MaskMap3
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
#define _BaseColorMap _Splat
#define sampler_BaseColorMap sampler_Splat
#define _NormalMap _Normal
#define ALPHA_USED_AS_SMOOTHNESS
#define _NORMALMAP_TANGENT_SPACE_IDX

// include LitDataInternal multiple time to define the variation of GetSurfaceData for each layer
#define LAYER_INDEX 0
#define ADD_IDX(Name) Name##0
#ifdef _NORMALMAP0
#define _NORMALMAP_IDX
#endif
#ifdef _MASKMAP0
#define _MASKMAP_IDX
#endif
#include "../Lit/LitDataIndividualLayer.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _MASKMAP_IDX

#define LAYER_INDEX 1
#define ADD_IDX(Name) Name##1
#ifdef _NORMALMAP1
#define _NORMALMAP_IDX
#endif
#ifdef _MASKMAP1
#define _MASKMAP_IDX
#endif
#include "../Lit/LitDataIndividualLayer.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _MASKMAP_IDX

#define LAYER_INDEX 2
#define ADD_IDX(Name) Name##2
#ifdef _NORMALMAP2
#define _NORMALMAP_IDX
#endif
#ifdef _MASKMAP2
#define _MASKMAP_IDX
#endif
#include "../Lit/LitDataIndividualLayer.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _MASKMAP_IDX

#define LAYER_INDEX 3
#define ADD_IDX(Name) Name##3
#ifdef _NORMALMAP3
#define _NORMALMAP_IDX
#endif
#ifdef _MASKMAP3
#define _MASKMAP_IDX
#endif
#include "../Lit/LitDataIndividualLayer.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX
#undef _NORMALMAP_IDX
#undef _MASKMAP_IDX

#undef ADD_ZERO_IDX
#undef _BaseColorMap
#undef sampler_BaseColorMap
#undef _NormalMap
#undef ALPHA_USED_AS_SMOOTHNESS
#undef _NORMALMAP_TANGENT_SPACE_IDX

float3 BlendLayeredVector3(float3 x0, float3 x1, float3 x2, float3 x3, float weight[4])
{
    float3 result = x0;
#if _LAYER_COUNT >= 2
    result = result * weight[0] + x1 * weight[1];
#endif
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
    float result = x0;
#if _LAYER_COUNT >= 2
    result = result * weight[0] + x1 * weight[1];
#endif
#if _LAYER_COUNT >= 3
    result += x2 * weight[2];
#endif
#if _LAYER_COUNT >= 4
    result += x3 * weight[3];
#endif

    return result;
}

TEXTURE2D(_Control);
SAMPLER(sampler_Control);

#define SURFACEDATA_BLEND_VECTOR3(surfaceData, name, mask) BlendLayeredVector3(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define SURFACEDATA_BLEND_SCALAR(surfaceData, name, mask) BlendLayeredScalar(MERGE_NAME(surfaceData, 0) MERGE_NAME(., name), MERGE_NAME(surfaceData, 1) MERGE_NAME(., name), MERGE_NAME(surfaceData, 2) MERGE_NAME(., name), MERGE_NAME(surfaceData, 3) MERGE_NAME(., name), mask);
#define PROP_BLEND_SCALAR(name, mask) BlendLayeredScalar(name##0, name##1, name##2, name##3, mask);

void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 vertexNormalWS, inout LayerTexCoord layerTexCoord)
{
    layerTexCoord.vertexNormalWS = vertexNormalWS;
    layerTexCoord.triplanarWeights = ComputeTriplanarWeights(vertexNormalWS);

    // Note: Blend mask have its dedicated mapping and tiling.
    // To share code, we simply call the regular code from the main layer for it then save the result, then do regular call for all layers.
    ComputeLayerTexCoord0(  texCoord0, texCoord1, texCoord2, texCoord3, float4(1, 0, 0, 0), float4(0, 0, 0, 0),
                            float2(1, 1), float2(0, 0), float2(0, 0), float2(0, 0), 1.0, false,
                            positionWS, 1,
                            UV_MAPPING_UVSET, layerTexCoord);

    layerTexCoord.blendMask = layerTexCoord.base0;

    // On all layers (but not on blend mask) we can scale the tiling with object scale (only uniform supported)
    // Note: the object scale doesn't affect planar/triplanar mapping as they already handle the object scale.
    float tileObjectScale = 1.0;

    int mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR0)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR0)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif

    ComputeLayerTexCoord0(  texCoord0, texCoord1, texCoord2, texCoord3, float4(1, 0, 0, 0), float4(0, 0, 0, 0),
                            _Splat0_ST.xy, _Splat0_ST.zw, float2(0, 0), float2(0, 0), tileObjectScale, 0,
                            positionWS, _TexWorldScale0,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR1)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR1)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord1(  texCoord0, texCoord1, texCoord2, texCoord3, float4(1, 0, 0, 0), float4(0, 0, 0, 0),
                            _Splat1_ST.xy, _Splat1_ST.zw, float2(0, 0), float2(0, 0), tileObjectScale, 0,
                            positionWS, _TexWorldScale1,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR2)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR2)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord2(  texCoord0, texCoord1, texCoord2, texCoord3, float4(1, 0, 0, 0), float4(0, 0, 0, 0),
                            _Splat2_ST.xy, _Splat2_ST.zw, float2(0, 0), float2(0, 0), tileObjectScale, 0,
                            positionWS, _TexWorldScale2,
                            mappingType, layerTexCoord);

    mappingType = UV_MAPPING_UVSET;
#if defined(_LAYER_MAPPING_PLANAR3)
    mappingType = UV_MAPPING_PLANAR;
#elif defined(_LAYER_MAPPING_TRIPLANAR3)
    mappingType = UV_MAPPING_TRIPLANAR;
#endif
    ComputeLayerTexCoord3(  texCoord0, texCoord1, texCoord2, texCoord3, float4(1, 0, 0, 0), float4(0, 0, 0, 0),
                            _Splat3_ST.xy, _Splat3_ST.zw, float2(0, 0), float2(0, 0), tileObjectScale, 0,
                            positionWS, _TexWorldScale3,
                            mappingType, layerTexCoord);
}

// This is call only in this file
// layerTexCoord must have been initialize to 0 outside of this function
void GetLayerTexCoord(FragInputs input, inout LayerTexCoord layerTexCoord)
{
#ifdef SURFACE_GRADIENT
    GenerateLayerTexCoordBasisTB(input, layerTexCoord);
#endif

    GetLayerTexCoord(   input.texCoord0, input.texCoord1, input.texCoord2, input.texCoord3,
                        input.positionWS, input.worldToTangent[2].xyz, layerTexCoord);
}

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
#if _LAYER_COUNT < 2
    height1 = 0.0;
#endif
}

void ComputeMaskWeights(float4 inputMasks, out float outWeights[_MAX_LAYER])
{
    ZERO_INITIALIZE_ARRAY(float, outWeights, _MAX_LAYER);

    outWeights[0] = inputMasks.r;
#if _LAYER_COUNT >= 2
    outWeights[1] = inputMasks.g;
#endif
#if _LAYER_COUNT >= 3
    outWeights[2] = inputMasks.b;
#endif
#if _LAYER_COUNT >= 4
    outWeights[3] = inputMasks.a;
#endif
}

float4 GetBlendMask(LayerTexCoord layerTexCoord, float4 vertexColor)
{
    return SAMPLE_UVMAPPING_TEXTURE2D(_Control, sampler_Control, layerTexCoord.blendMask);
}

float GetInfluenceMask(LayerTexCoord layerTexCoord, bool useLodSampling = false, float lod = 0)
{
    // Sample influence mask with same mapping as Main layer
    return useLodSampling ? SAMPLE_UVMAPPING_TEXTURE2D_LOD(_LayerInfluenceMaskMap, sampler_LayerInfluenceMaskMap, layerTexCoord.base0, lod).r : SAMPLE_UVMAPPING_TEXTURE2D(_LayerInfluenceMaskMap, sampler_LayerInfluenceMaskMap, layerTexCoord.base0).r;
}

float GetMaxHeight(float4 heights)
{
    float maxHeight;
#if _LAYER_COUNT >= 4
    maxHeight = max(Max3(heights.r, heights.g, heights.b), heights.a);
#elif _LAYER_COUNT >= 3
    maxHeight = Max3(heights.r, heights.g, heights.b);
#elif _LAYER_COUNT >= 2
    maxHeight = max(heights.r, heights.g);
#else
    maxHeight = heights.r;
#endif
    return maxHeight;
}

// Returns layering blend mask after application of height based blend.
float4 ApplyHeightBlend(float4 heights, float4 blendMask)
{
    // We need to mask out inactive layers so that their height does not impact the result.
    float4 maskedHeights = heights * blendMask.argb;

    float maxHeight = GetMaxHeight(maskedHeights);
    // Make sure that transition is not zero otherwise the next computation will be wrong.
    // The epsilon here also has to be bigger than the epsilon in the next computation.
    float transition = max(_HeightTransition, 1e-5);

    // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
    // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
    maskedHeights = maskedHeights - maxHeight.xxxx;
    // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
    maskedHeights = (max(0, maskedHeights + transition) + 1e-6) * blendMask.argb;

    // Normalize
    maxHeight = GetMaxHeight(maskedHeights);
    maskedHeights = maskedHeights / maxHeight.xxxx;

    return maskedHeights.yzwx;
}

// Calculate weights to apply to each layer
// Caution: This function must not be use for per vertex/pixel displacement, there is a dedicated function for them.
// This function handle triplanar
void ComputeLayerWeights(FragInputs input, LayerTexCoord layerTexCoord, float4 inputAlphaMask, float4 blendMasks, out float outWeights[_MAX_LAYER])
{
#if defined(_DENSITY_MODE)
    float4 opacityAsDensity = saturate((inputAlphaMask - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
    float4 useOpacityAsDensityParam = float4(_OpacityAsDensity0, _OpacityAsDensity1, _OpacityAsDensity2, _OpacityAsDensity3);
    blendMasks = lerp(blendMasks, opacityAsDensity, useOpacityAsDensityParam);
#endif

#if LAYERS_HEIGHTMAP_ENABLE
    float height0 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap0, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base0).r - _HeightCenter0) * _HeightAmplitude0;
    float height1 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap1, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base1).r - _HeightCenter1) * _HeightAmplitude1;
    float height2 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap2, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base2).r - _HeightCenter2) * _HeightAmplitude2;
    float height3 = (SAMPLE_UVMAPPING_TEXTURE2D(_HeightMap3, SAMPLER_HEIGHTMAP_IDX, layerTexCoord.base3).r - _HeightCenter3) * _HeightAmplitude3;
    // Height is affected by tiling property and by object scale (depends on option).
    // Apply scaling from tiling properties (TexWorldScale and tiling from BaseColor)
    ApplyDisplacementTileScale(height0, height1, height2, height3);
    // Nullify height that are not used, so compiler can remove unused case
    SetEnabledHeightByLayer(height0, height1, height2, height3);

    // Reminder: _MAIN_LAYER_INFLUENCE_MODE is a purely visual mode, it is not take into account for the blendMasks
    // As it is purely visual, it is not apply in ComputeLayerWeights

    #if defined(_HEIGHT_BASED_BLEND)
    // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
    blendMasks = ApplyHeightBlend(float4(height0, height1, height2, height3), blendMasks);
    #endif
#endif

    ComputeMaskWeights(blendMasks, outWeights);
}

#include "LayeredLitDataDisplacement.hlsl"
#include "../Lit/LitBuiltinData.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    // terrain lightmap uvs are always taken from uv0
    input.texCoord1 = input.texCoord2 = input.texCoord0;

    ApplyDoubleSidedFlipOrMirror(input); // Apply double sided flip on the vertex normal

    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);
    GetLayerTexCoord(input, layerTexCoord);

    float4 blendMasks = GetBlendMask(layerTexCoord, input.color);
    float depthOffset = ApplyPerPixelDisplacement(input, V, layerTexCoord, blendMasks);

    SurfaceData surfaceData0, surfaceData1, surfaceData2, surfaceData3;
    float3 normalTS0, normalTS1, normalTS2, normalTS3;
    float3 bentNormalTS0, bentNormalTS1, bentNormalTS2, bentNormalTS3;
    float alpha0 = GetSurfaceData0(input, layerTexCoord, surfaceData0, normalTS0, bentNormalTS0);
    float alpha1 = GetSurfaceData1(input, layerTexCoord, surfaceData1, normalTS1, bentNormalTS1);
    float alpha2 = GetSurfaceData2(input, layerTexCoord, surfaceData2, normalTS2, bentNormalTS2);
    float alpha3 = GetSurfaceData3(input, layerTexCoord, surfaceData3, normalTS3, bentNormalTS3);

    // Note: If per pixel displacement is enabled it mean we will fetch again the various heightmaps at the intersection location. Not sure the compiler can optimize.
    float weights[_MAX_LAYER];
    ComputeLayerWeights(input, layerTexCoord, float4(alpha0, alpha1, alpha2, alpha3), blendMasks, weights);

    // For layered shader, alpha of base color is used as either an opacity mask, a composition mask for inheritance parameters or a density mask.
    float alpha = PROP_BLEND_SCALAR(alpha, weights);

    float3 normalTS;
    float3 bentNormalWS;
    surfaceData.baseColor = SURFACEDATA_BLEND_VECTOR3(surfaceData, baseColor, weights);
    normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);

    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);
    surfaceData.tangentWS = normalize(input.worldToTangent[0].xyz); // The tangent is not normalize in worldToTangent for mikkt. Tag: SURFACE_GRADIENT
    surfaceData.subsurfaceMask = 0;
    surfaceData.thickness = 1;
    surfaceData.diffusionProfile = 0;

    surfaceData.materialFeatures = MATERIALFEATUREFLAGS_LIT_STANDARD;

    // Init other parameters
    surfaceData.anisotropy = 0.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);
    surfaceData.coatMask = 0.0;
    surfaceData.iridescenceThickness = 0.0;
    surfaceData.iridescenceMask = 0.0;

    // Transparency parameters
    // Use thickness from SSS
    surfaceData.ior = 1.0;
    surfaceData.transmittanceColor = float3(1.0, 1.0, 1.0);
    surfaceData.atDistance = 1000000.0;
    surfaceData.transmittanceMask = 0.0;

    GetNormalWS(input, V, normalTS, surfaceData.normalWS);
    bentNormalWS = surfaceData.normalWS;

    // By default we use the ambient occlusion with Tri-ace trick (apply outside) for specular occlusion.
#if defined(_MASKMAP0) || defined(_MASKMAP1) || defined(_MASKMAP2) || defined(_MASKMAP3)
    surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(dot(surfaceData.normalWS, V), surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
#else
    surfaceData.specularOcclusion = 1.0;
#endif

#ifndef _DISABLE_DBUFFER
    AddDecalContribution(posInput, surfaceData, alpha);
#endif

#if defined(DEBUG_DISPLAY)
    if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
    {
        surfaceData.baseColor = GetTextureDataDebug(_DebugMipMapMode, layerTexCoord.base0.uv, _BaseColorMap0, _BaseColorMap0_TexelSize, _BaseColorMap0_MipInfo, surfaceData.baseColor);
        surfaceData.metallic = 0;
    }
#endif

    GetBuiltinData(input, surfaceData, alpha, bentNormalWS, depthOffset, builtinData);
}

#include "TerrainLitDataMeshModification.hlsl"
