//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "../MaterialUtilities.hlsl"
#include "../SampleLayer.hlsl"

void GetBuiltinData(FragInputs input, SurfaceData surfaceData, float alpha, float depthOffset, out BuiltinData builtinData)
{
    // Builtin Data
    builtinData.opacity = alpha;

    // TODO: Sample lightmap/lightprobe/volume proxy
    // This should also handle projective lightmap
    // Note that data input above can be use to sample into lightmap (like normal)
    builtinData.bakeDiffuseLighting = SampleBakedGI(input.positionWS, surfaceData.normalWS, input.texCoord1, input.texCoord2);

    // Emissive Intensity is only use here, but is part of BuiltinData to enforce UI parameters as we want the users to fill one color and one intensity
    builtinData.emissiveIntensity = _EmissiveIntensity; // We still store intensity here so we can reuse it with debug code

    // If we chose an emissive color, we have a dedicated texture for it and don't use MaskMap
#ifdef _EMISSIVE_COLOR
#ifdef _EMISSIVE_COLOR_MAP
    builtinData.emissiveColor = SAMPLE_TEXTURE2D(_EmissiveColorMap, sampler_EmissiveColorMap, input.texCoord0).rgb * _EmissiveColor * builtinData.emissiveIntensity;
#else
    builtinData.emissiveColor = _EmissiveColor * builtinData.emissiveIntensity;
#endif
// If we have a MaskMap, use emissive slot as a mask on baseColor
#elif defined(_MASKMAP) && !defined(LAYERED_LIT_SHADER) // With layered lit we have no emissive mask option
    builtinData.emissiveColor = surfaceData.baseColor * (SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).b * builtinData.emissiveIntensity).xxx;
#else
    builtinData.emissiveColor = float3(0.0, 0.0, 0.0);
#endif

    builtinData.velocity = float2(0.0, 0.0);

#ifdef _DISTORTION_ON
    float3 distortion = SAMPLE_TEXTURE2D(_DistortionVectorMap, sampler_DistortionVectorMap, input.texCoord0).rgb;
    builtinData.distortion = distortion.rg;
    builtinData.distortionBlur = distortion.b;
#else
    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
#endif

    builtinData.depthOffset = depthOffset;
}

struct LayerTexCoord
{
#ifndef LAYERED_LIT_SHADER
    LayerUV base;
    LayerUV details;
#else
    // Regular texcoord
    LayerUV base0;
    LayerUV base1;
    LayerUV base2;
    LayerUV base3;

    LayerUV details0;
    LayerUV details1;
    LayerUV details2;
    LayerUV details3;
#endif

    // triplanar weight
    float3 weights;
};

#ifndef LAYERED_LIT_SHADER

// include LitDataInternal to define GetSurfaceData
#define LAYER_INDEX 0
#define ADD_IDX(Name) Name
#define ADD_ZERO_IDX(Name) Name
#include "LitDataInternal.hlsl"

void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 normalWS, out LayerTexCoord layerTexCoord)
{
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);

#ifdef _MAPPING_TRIPLANAR
    // one weight for each direction XYZ - Use vertex normal for triplanar
    layerTexCoord.weights = ComputeTriplanarWeights(normalWS);
#endif

    // Be sure that the compiler is aware that we don't touch UV1 to UV3 for base layer in case of non layer shader
    // so it can remove code
    _UVMappingMask.yzw = float3(0.0, 0.0, 0.0);
    bool isTriplanar = false;
#ifdef _MAPPING_TRIPLANAR
    isTriplanar = true;
#endif
    ComputeLayerTexCoord(   texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord);
}

void ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord)
{
#if defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT)

    // ref: https://www.gamedev.net/resources/_/technical/graphics-programming-and-theory/a-closer-look-at-parallax-occlusion-mapping-r3262
    float3 viewDirTS = TransformWorldToTangent(V, input.tangentToWorld);
    // Change the number of samples per ray depending on the viewing angle for the surface. 
    // Oblique angles require  smaller step sizes to achieve more accurate precision for computing displacement.
    int numSteps = (int)lerp(_PPDMaxSamples, _PPDMinSamples, viewDirTS.z);

    ParallaxOcclusionMappingLayer(layerTexCoord, numSteps, viewDirTS);

    // TODO: We are supposed to modify lightmaps coordinate (fetch in GetBuiltin), but this isn't the same uv mapping, so can't apply the offset here...
    // Let's assume it will be "fine" as indirect diffuse is often low frequency
#endif
}

// Calculate displacement for per vertex displacement mapping
float ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
{
    return SampleHeightmapLod(layerTexCoord, lod);
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    LayerTexCoord layerTexCoord;
    GetLayerTexCoord(input.texCoord0, input.texCoord1, input.texCoord2, input.texCoord3,
                     input.positionWS, input.tangentToWorld[2].xyz, layerTexCoord);


    ApplyPerPixelDisplacement(input, V, layerTexCoord);
    float depthOffset = 0.0;

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, depthOffset, posInput);
#endif

    // We perform the conversion to world of the normalTS outside of the GetSurfaceData
    // so it allow us to correctly deal with detail normal map and optimize the code for the layered shaders
    float3 normalTS;
    float alpha = GetSurfaceData(input, layerTexCoord, surfaceData, normalTS);
    GetNormalAndTangentWS(input, V, normalTS, surfaceData.normalWS, surfaceData.tangentWS);

    // Caution: surfaceData must be fully initialize before calling GetBuiltinData
    GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
}

#else

#define ADD_ZERO_IDX(Name) Name##0

// include LitDataInternal multiple time to define the variation of GetSurfaceData for each layer
#define LAYER_INDEX 0
#define ADD_IDX(Name) Name##0
#include "LitDataInternal.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX

#define LAYER_INDEX 1
#define ADD_IDX(Name) Name##1
#include "LitDataInternal.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX

#define LAYER_INDEX 2
#define ADD_IDX(Name) Name##2
#include "LitDataInternal.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX

#define LAYER_INDEX 3
#define ADD_IDX(Name) Name##3
#include "LitDataInternal.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX

void ComputeMaskWeights(float3 inputMasks, out float outWeights[_MAX_LAYER])
{
    float masks[_MAX_LAYER];
    masks[0] = 1.0; // Layer 0 is always full
    masks[1] = inputMasks.r;
    masks[2] = inputMasks.g;
    masks[3] = inputMasks.b;

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

#define SURFACEDATA_BLEND_VECTOR3(surfaceData, name, mask) BlendLayeredVector3(surfaceData##0.##name, surfaceData##1.##name, surfaceData##2.##name, surfaceData##3.##name, mask);
#define SURFACEDATA_BLEND_SCALAR(surfaceData, name, mask) BlendLayeredScalar(surfaceData##0.##name, surfaceData##1.##name, surfaceData##2.##name, surfaceData##3.##name, mask);
#define PROP_BLEND_SCALAR(name, mask) BlendLayeredScalar(name##0, name##1, name##2, name##3, mask);

float ApplyHeightBasedBlend(inout float inputFactor, float previousLayerHeight, float layerHeight, float heightOffset, float heightFactor, float edgeBlendStrength, float vertexColor)
{
    float finalLayerHeight = heightFactor * layerHeight + heightOffset + _VertexColorHeightFactor * (vertexColor * 2.0 - 1.0);

    edgeBlendStrength = max(0.00001, edgeBlendStrength);

    if (previousLayerHeight >= finalLayerHeight)
    {
        inputFactor = 0.0;
    }
    else if (finalLayerHeight > previousLayerHeight && finalLayerHeight < previousLayerHeight + edgeBlendStrength)
    {
        inputFactor = inputFactor * pow((finalLayerHeight - previousLayerHeight) / edgeBlendStrength, 0.5);
    }

    return max(finalLayerHeight, previousLayerHeight);
}

float3 ApplyHeightBasedBlendV2(float3 inputMask, float3 inputHeight, float3 blendUsingHeight)
{
    return saturate(lerp(inputMask * inputHeight * blendUsingHeight * 100, 1, inputMask * inputMask)); // 100 arbitrary scale to limit blendUsingHeight values.
}

void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 normalWS, out LayerTexCoord layerTexCoord)
{
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);

#if defined(_LAYER_MAPPING_TRIPLANAR_0) || defined(_LAYER_MAPPING_TRIPLANAR_1) || defined(_LAYER_MAPPING_TRIPLANAR_2) || defined(_LAYER_MAPPING_TRIPLANAR_3)
    // one weight for each direction XYZ - Use vertex normal for triplanar
    layerTexCoord.weights = ComputeTriplanarWeights(normalWS);
#endif

    bool isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_0
    isTriplanar = true;
#endif
    ComputeLayerTexCoord0(  texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord, _LayerTiling0);

    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_1
    isTriplanar = true;
#endif
    ComputeLayerTexCoord1(  texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord, _LayerTiling1);

    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_2
    isTriplanar = true;
#endif
    ComputeLayerTexCoord2(  texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord, _LayerTiling2);

    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_3
    isTriplanar = true;
#endif
    ComputeLayerTexCoord3(  texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord, _LayerTiling3);
}

void ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord)
{
#if defined(_HEIGHTMAP) && defined(_PER_PIXEL_DISPLACEMENT)
    float3 viewDirTS = TransformWorldToTangent(V, input.tangentToWorld);
    int numSteps = (int)lerp(_PPDMaxSamples, _PPDMinSamples, viewDirTS.z);

    ParallaxOcclusionMappingLayer0(layerTexCoord, numSteps, viewDirTS);
    ParallaxOcclusionMappingLayer1(layerTexCoord, numSteps, viewDirTS);
    ParallaxOcclusionMappingLayer2(layerTexCoord, numSteps, viewDirTS);
    ParallaxOcclusionMappingLayer3(layerTexCoord, numSteps, viewDirTS);
#endif
}

float3 ComputeInheritedNormalTS(FragInputs input, float3 normalTS0, float3 normalTS1, float3 normalTS2, float3 normalTS3, LayerTexCoord layerTexCoord, float weights[_MAX_LAYER])
{
    float3 normalTS;

    // Compute how much we want to inherit from base layer normal base on the mask. Base layer always fully inherit from "itself" if it's the visible layer.
    float inheritBaseNormal = BlendLayeredScalar(1.0, _InheritBaseNormal1, _InheritBaseNormal2, _InheritBaseNormal3, weights);
    // Based on this inheritance parameters, fetch a lower level of the base layer normal map so that the less we inherit the more this tends to be "vertex normal"
    float maxMipBias = log2(max(_NormalMap0_TexelSize.x, _NormalMap0_TexelSize.y)) + 1.0; // TODO: Use hardware instruction here (GetDimensions) that can retunr mipmaps num ? will this be faster
    float3 inheritedBaseNormalTS = GetNormalTS0(input, layerTexCoord, float3(0.0, 0.0, 0.0), 0.0, true, maxMipBias * (1.0 - inheritBaseNormal));

    // Blend all layers but the base one. This will then be added to the "inherited" normal of base layer (that's why base layer here is tangent space vertex normal so that if it's the visible layer we add nothing in term of normal map).
    float3 layersNormalTS = BlendLayeredVector3(float3(0.0, 0.0, 1.0), normalTS1, normalTS2, normalTS3, weights);
    // Add the inherited normal to the blended top layers.
    normalTS = BlendNormalRNM(inheritedBaseNormalTS, layersNormalTS);

    return normalTS;
}

float3 ComputeInheritedColor(float3 baseColor0, float3 baseColor1, float3 baseColor2, float3 baseColor3, float compoMask, LayerTexCoord layerTexCoord, float weights[_MAX_LAYER])
{
    float inheritBaseColor = BlendLayeredScalar(1.0, _InheritBaseColor1, _InheritBaseColor2, _InheritBaseColor3, weights);
    float inheritBaseColorThreshold = BlendLayeredScalar(1.0, _InheritBaseColorThreshold1, _InheritBaseColorThreshold2, _InheritBaseColorThreshold3, weights);

    inheritBaseColor = inheritBaseColor * (1.0 - saturate(compoMask / inheritBaseColorThreshold));

    // We want to calculate the mean color of the texture. For this we will sample a low mipmap
    float textureBias = 15.0; // Force a high number to be sure we get the lowest mip
    float3 baseMeanColor0 = SAMPLE_LAYER_TEXTURE2D_BIAS(_BaseColorMap0, sampler_BaseColorMap0, layerTexCoord.base0, textureBias).rgb * _BaseColor0.rgb;
    float3 baseMeanColor1 = SAMPLE_LAYER_TEXTURE2D_BIAS(_BaseColorMap1, sampler_BaseColorMap0, layerTexCoord.base1, textureBias).rgb * _BaseColor1.rgb;
    float3 baseMeanColor2 = SAMPLE_LAYER_TEXTURE2D_BIAS(_BaseColorMap2, sampler_BaseColorMap0, layerTexCoord.base2, textureBias).rgb * _BaseColor2.rgb;
    float3 baseMeanColor3 = SAMPLE_LAYER_TEXTURE2D_BIAS(_BaseColorMap3, sampler_BaseColorMap0, layerTexCoord.base3, textureBias).rgb * _BaseColor3.rgb;

    float3 meanColor = BlendLayeredVector3(baseMeanColor0, baseMeanColor1, baseMeanColor2, baseMeanColor3, weights);
    float3 baseColor = BlendLayeredVector3(baseColor0, baseColor1, baseColor2, baseColor3, weights);

    // If we inherit from base layer, we will add a bit of it
    return inheritBaseColor * (baseColor0 - meanColor) + baseColor;
}

// Calculate displacement for per vertex displacement mapping
float ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
{
    // Mask Values : Layer 1, 2, 3 are r, g, b. Always use layer0 parametrization for the mask
    float3 inputMaskValues = SAMPLE_LAYER_TEXTURE2D_LOD(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.base0, lod).rgb;

#if defined(_LAYER_MASK_VERTEX_COLOR_MUL)
    inputMaskValues *= vertexColor.rgb;
#elif defined(_LAYER_MASK_VERTEX_COLOR_ADD)
    inputMaskValues = saturate(inputMaskValues + vertexColor.rgb * 2.0 - 1.0);
#endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(inputMaskValues, weights);

    float height0 = SampleHeightmapLod0(layerTexCoord, lod);
    float height1 = SampleHeightmapLod1(layerTexCoord, lod, _HeightCenterOffset1, _HeightFactor1);
    float height2 = SampleHeightmapLod2(layerTexCoord, lod, _HeightCenterOffset2, _HeightFactor2);
    float height3 = SampleHeightmapLod3(layerTexCoord, lod, _HeightCenterOffset3, _HeightFactor3);
    float heightResult = BlendLayeredScalar(height0, height1, height2, height3, weights);

#if defined(_MAIN_LAYER_INFLUENCE_MODE)
    // Think that inheritbasedheight will be 0 if height0 is fully visible in weights. So there is no double contribution of height0
    float inheritBaseHeight = BlendLayeredScalar(0.0, _InheritBaseHeight1, _InheritBaseHeight2, _InheritBaseHeight3, weights);
    return heightResult + height0 * inheritBaseHeight;
#endif

    return heightResult;
}

// Calculate weights to apply to each layer
// Caution: This function must not be use for per vertex of per pixel displacement, there is a dedicated function for them.
// this function handle triplanar
void ComputeLayerWeights(FragInputs input, LayerTexCoord layerTexCoord, float4 inputAlphaMask, out float outWeights[_MAX_LAYER])
{
    // Mask Values : Layer 1, 2, 3 are r, g, b. Always use layer0 parametrization for the mask
    float3 inputMaskValues = SAMPLE_LAYER_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.base0).rgb;

#if defined(_LAYER_MASK_VERTEX_COLOR_MUL)
    inputMaskValues *= input.color.rgb;
#elif defined(_LAYER_MASK_VERTEX_COLOR_ADD)
    inputMaskValues = saturate(inputMaskValues + input.color.rgb * 2.0 - 1.0);
#endif

    float3 minOpaParam = float3(_MinimumOpacity1, _MinimumOpacity2, _MinimumOpacity3);
    float3 remapedOpacity = lerp(minOpaParam, float3(1.0, 1.0, 1.0), inputAlphaMask.yzw); // Remap opacity mask from [0..1] to [minOpa..1]
    float3 opacityAsDensity = saturate((inputAlphaMask.yzw - (float3(1.0, 1.0, 1.0) - inputMaskValues)) * 20.0);

    float3 useOpacityAsDensityParam = float3(_OpacityAsDensity1, _OpacityAsDensity2, _OpacityAsDensity3);
    inputMaskValues = lerp(inputMaskValues * remapedOpacity, opacityAsDensity, useOpacityAsDensityParam);

#if defined(_HEIGHT_BASED_BLEND)
    float height0 = SampleHeightmap0(layerTexCoord);
    float height1 = SampleHeightmap1(layerTexCoord, _HeightCenterOffset1, _HeightFactor1);
    float height2 = SampleHeightmap2(layerTexCoord, _HeightCenterOffset2, _HeightFactor2);
    float height3 = SampleHeightmap3(layerTexCoord, _HeightCenterOffset3, _HeightFactor3);

    float4 heights = float4(height0, height1, height2, height3);

    #if !defined(_HEIGHT_BASED_BLEND_V2)
        float baseLayerHeight = heights.x;
        baseLayerHeight = ApplyHeightBasedBlend(inputMaskValues.r, baseLayerHeight, heights.y, _HeightOffset1, _HeightFactor1, _BlendSize1, input.color.r);
        baseLayerHeight = ApplyHeightBasedBlend(inputMaskValues.g, baseLayerHeight, heights.z, _HeightOffset2 + _HeightOffset1, _HeightFactor2, _BlendSize2, input.color.g);
        ApplyHeightBasedBlend(inputMaskValues.b, baseLayerHeight, heights.w, _HeightOffset3 + _HeightOffset2 + _HeightOffset1, _HeightFactor3, _BlendSize3, input.color.b);
    #else

        // HACK, use height0 to avoid compiler error for unused sampler
        // To remove once we have POM
        heights.y += (heights.x * 0.0001);

        inputMaskValues = ApplyHeightBasedBlendV2(inputMaskValues, heights.yzw, float3(_BlendUsingHeight1, _BlendUsingHeight2, _BlendUsingHeight3));
    #endif
#endif

    ComputeMaskWeights(inputMaskValues, outWeights);
}

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    LayerTexCoord layerTexCoord;
    GetLayerTexCoord(input.texCoord0, input.texCoord1, input.texCoord2, input.texCoord3,
                     input.positionWS, input.tangentToWorld[2].xyz, layerTexCoord);

    ApplyPerPixelDisplacement(input, V, layerTexCoord);

    float depthOffset = 0.0;
#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, depthOffset, posInput);
#endif

    SurfaceData surfaceData0, surfaceData1, surfaceData2, surfaceData3;
    float3 normalTS0, normalTS1, normalTS2, normalTS3;
    float alpha0 = GetSurfaceData0(input, layerTexCoord, surfaceData0, normalTS0);
    float alpha1 = GetSurfaceData1(input, layerTexCoord, surfaceData1, normalTS1);
    float alpha2 = GetSurfaceData2(input, layerTexCoord, surfaceData2, normalTS2);
    float alpha3 = GetSurfaceData3(input, layerTexCoord, surfaceData3, normalTS3);

    float weights[_MAX_LAYER];
    ComputeLayerWeights(input, layerTexCoord, float4(alpha0, alpha1, alpha2, alpha3), weights);

    // For layered shader, alpha of base color is used as either an opacity mask, a composition mask for inheritance parameters or a density mask.
    float alpha = PROP_BLEND_SCALAR(alpha, weights);

#if defined(_MAIN_LAYER_INFLUENCE_MODE)
    surfaceData.baseColor = ComputeInheritedColor(surfaceData0.baseColor, surfaceData1.baseColor, surfaceData2.baseColor, surfaceData3.baseColor, alpha, layerTexCoord, weights);
    float3 normalTS = ComputeInheritedNormalTS(input, normalTS0, normalTS1, normalTS2, normalTS3, layerTexCoord, weights);
#else
    surfaceData.baseColor = SURFACEDATA_BLEND_VECTOR3(surfaceData, baseColor, weights);
    float3 normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);
#endif

    surfaceData.specularOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, specularOcclusion, weights);
    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);

    // Init other unused parameter
    surfaceData.tangentWS = normalize(input.tangentToWorld[0].xyz);
    surfaceData.materialId = 0;
    surfaceData.anisotropy = 0;
    surfaceData.specular = 0.04;
    surfaceData.subSurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subSurfaceProfile = 0;
    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

    GetNormalAndTangentWS(input, V, normalTS, surfaceData.normalWS, surfaceData.tangentWS);

    GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
}

#endif // #ifndef LAYERED_LIT_SHADER

#ifdef TESSELLATION_ON
#include "LitTessellation.hlsl" // Must be after GetLayerTexCoord() declaration
#endif
