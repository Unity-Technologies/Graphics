//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------
#include "../MaterialUtilities.hlsl"

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

// Gather all kind of mapping in one struct, allow to improve code readability
struct LayerUV
{
    float2 uv;
    // triplanar
    bool isTriplanar;
    float2 uvYZ;
    float2 uvZX;
    float2 uvXY;
};

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

float4 SampleLayer(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights)
{
    if (layerUV.isTriplanar)
    {
        float4 val = float4(0.0, 0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvYZ);
        if (weights.y > 0.0)
            val += weights.y * SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvZX);
        if (weights.z > 0.0)
            val += weights.z * SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvXY);

        return val;
    }
    else
    {
        return SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uv);
    }
}

float4 SampleLayerLod(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights, float lod)
{
    if (layerUV.isTriplanar)
    {
        float4 val = float4(0.0, 0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * SAMPLE_TEXTURE2D_LOD(layerTex, layerSampler, layerUV.uvYZ, lod);
        if (weights.y > 0.0)
            val += weights.y * SAMPLE_TEXTURE2D_LOD(layerTex, layerSampler, layerUV.uvZX, lod);
        if (weights.z > 0.0)
            val += weights.z * SAMPLE_TEXTURE2D_LOD(layerTex, layerSampler, layerUV.uvXY, lod);

        return val;
    }
    else
    {
        return SAMPLE_TEXTURE2D_LOD(layerTex, layerSampler, layerUV.uv, lod);
    }
}

#define ADD_FUNC_SUFFIX(Name) Name
#define NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV, bias) SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV)
#include "LayeredLitNormalSampling.hlsl"
#undef ADD_FUNC_SUFFIX
#undef NORMAL_SAMPLE_FUNC

#define ADD_FUNC_SUFFIX(Name) Name##_Bias
#define NORMAL_SAMPLE_FUNC(layerTex, layerSampler, layerUV, bias) SAMPLE_TEXTURE2D_BIAS(layerTex, layerSampler, layerUV, bias)
#include "LayeredLitNormalSampling.hlsl"
#undef ADD_FUNC_SUFFIX
#undef NORMAL_SAMPLE_FUNC

// Macro to improve readibility of surface data
#define SAMPLE_LAYER_TEXTURE2D(textureName, samplerName, coord) SampleLayer(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights)
#define SAMPLE_LAYER_TEXTURE2D_LOD(textureName, samplerName, coord, lod) SampleLayerLod(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, lod)
#define SAMPLE_LAYER_NORMALMAP(textureName, samplerName, coord, scale, useBias, bias) useBias ? SampleLayerNormal_Bias(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias) : SampleLayerNormal(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias)
#define SAMPLE_LAYER_NORMALMAP_AG(textureName, samplerName, coord, scale, useBias, bias) useBias ? SampleLayerNormalAG_Bias(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias) : SampleLayerNormalAG(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias)
#define SAMPLE_LAYER_NORMALMAP_RGB(textureName, samplerName, coord, scale, useBias, bias) useBias ? SampleLayerNormalRGB_Bias(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias) : SampleLayerNormalRGB(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights, scale, bias)


#ifndef LAYERED_LIT_SHADER

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
    surfaceData.normalWS = TransformTangentToWorld(normalTS, input.tangentToWorld);
    surfaceData.tangentWS = input.tangentToWorld[0].xyz;

    // NdotV should not be negative for visible pixels, but it can happen due to the
    // perspective projection and the normal mapping + decals. In that case, the normal
    // should be modified to become valid (i.e facing the camera) to avoid weird artifacts.
    // Note: certain applications (e.g. SpeedTree) make use of double-sided lighting.
    // This will  potentially reduce the length of the normal at edges of geometry.
    bool twoSided = false;
    GetShiftedNdotV(surfaceData.normalWS, V, twoSided);

    // Orthonormalize the basis vectors using the Gram-Schmidt process.
    // We assume that the length of the surface normal is sufficiently close to 1.
    surfaceData.tangentWS = normalize(surfaceData.tangentWS - dot(surfaceData.tangentWS, surfaceData.normalWS));

    // Caution: surfaceData must be fully initialize before calling GetBuiltinData
    GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
}

#else

#define ADD_ZERO_IDX(Name) Name##0

// Generate function for all layer
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
    masks[0] = 1.0f; // Layer 0 is always full
    masks[1] = inputMasks.r;
    masks[2] = inputMasks.g;
    masks[3] = inputMasks.b;

    // calculate weight of each layers
    float left = 1.0f;

    [unroll]
    for (int i = _LAYER_COUNT - 1; i > 0; --i)
    {
        outWeights[i] = masks[i] * left;
        left -= outWeights[i];
    }
    outWeights[0] = left;
}

float3 BlendLayeredFloat3(float3 x0, float3 x1, float3 x2, float3 x3, float weight[4])
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


#define SURFACEDATA_BLEND_COLOR(surfaceData, name, mask) BlendLayeredFloat3(surfaceData##0.##name, surfaceData##1.##name, surfaceData##2.##name, surfaceData##3.##name, mask);
#define SURFACEDATA_BLEND_SCALAR(surfaceData, name, mask) BlendLayeredScalar(surfaceData##0.##name, surfaceData##1.##name, surfaceData##2.##name, surfaceData##3.##name, mask);
#define PROP_BLEND_SCALAR(name, mask) BlendLayeredScalar(name##0, name##1, name##2, name##3, mask);

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
                            positionWS, normalWS, isTriplanar, layerTexCoord);

    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_1
    isTriplanar = true;
#endif
    ComputeLayerTexCoord1(  texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord);

    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_2
    isTriplanar = true;
#endif
    ComputeLayerTexCoord2(  texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord);

    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_3
    isTriplanar = true;
#endif
    ComputeLayerTexCoord3(  texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord);
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
//#if !defined(_HEIGHT_BASED_BLEND_V2)
//    float _InheritBaseLayer0 = 1.0f; // Default value for lerp when all weights but base layer are zero.
//
//    // Compute the combined inheritance factor of layers 1,2 and 3
//    float inheritFactor = PROP_BLEND_SCALAR(_InheritBaseLayer, weights);
//    float3 vertexNormalTS = float3(0.0, 0.0, 1.0);
//    // The idea here is to lerp toward vertex normal. This way when we don't want to inherit, we will combine layer 1/2/3 normal with a vertex normal which is neutral.
//    float3 baseLayerNormalTS = normalize(lerp(vertexNormalTS, normalTS0, inheritFactor));
//    // Blend layer 1/2/3 normals before combining to the base layer. Again we need to have a neutral value for base layer (vertex normal) in case all weights are zero.
//    float3 layersNormalTS = BlendLayeredFloat3(vertexNormalTS, normalTS1, normalTS2, normalTS3, weights);
//    normalTS = BlendNormalRNM(baseLayerNormalTS, layersNormalTS);
//#else

    // Compute how much we want to inherit from base layer normal base on the mask. Base layer always fully inherit from "itself" if it's the visible layer.
    float inheritBaseNormal = BlendLayeredScalar(1.0f, _InheritBaseNormal1, _InheritBaseNormal2, _InheritBaseNormal3, weights);
    // Based on this inheritance parameters, fetch a lower level of the base layer normal map so that the less we inherit the more this tends to be "vertex normal"
    float maxMipBias = 12.0f; // We arbitrarly choose the max bias for a 2048 texture. Smaller texture will bias toward vertex normal faster.
    float3 inheritedBaseNormalTS = GetNormalTS0(input, layerTexCoord, float3(0.0, 0.0, 0.0), 0.0f, true, maxMipBias * (1.0 - inheritBaseNormal));

    // Blend all layers but the base one. This will then be added to the "inherited" normal of base layer (that's why base layer here is tangent space vertex normal so that if it's the visible layer we add nothing in term of normal map).
    float3 layersNormalTS = BlendLayeredFloat3(float3(0.0, 0.0, 1.0), normalTS1, normalTS2, normalTS3, weights);
    // Add the inherited normal to the blended top layers.
    normalTS = BlendNormalRNM(inheritedBaseNormalTS, layersNormalTS);
//#endif

    return normalTS;
}

float3 ComputeInheritedColor(float3 baseColor0, float3 baseColor1, float3 baseColor2, float3 baseColor3, float compoMask, LayerTexCoord layerTexCoord, float weights[_MAX_LAYER])
{
    //return BlendLayeredFloat3(baseColor0, baseColor1, baseColor2, baseColor3, weights);

    float inheritBaseColor = BlendLayeredScalar(1.0f, _InheritBaseColor1, _InheritBaseColor2, _InheritBaseColor3, weights);
    float inheritBaseColorThreshold = BlendLayeredScalar(1.0f, _InheritBaseColorThreshold1, _InheritBaseColorThreshold2, _InheritBaseColorThreshold3, weights);

    inheritBaseColor = inheritBaseColor * (1.0 - saturate(compoMask / inheritBaseColorThreshold));

    float textureBias = 12.0f;
    float3 baseMeanColor0 = SAMPLE_TEXTURE2D_BIAS(_BaseColorMap0, sampler_BaseColorMap0, layerTexCoord.base0.uv, textureBias).rgb * _BaseColor0.rgb;
    float3 baseMeanColor1 = SAMPLE_TEXTURE2D_BIAS(_BaseColorMap1, sampler_BaseColorMap0, layerTexCoord.base1.uv, textureBias).rgb * _BaseColor1.rgb;
    float3 baseMeanColor2 = SAMPLE_TEXTURE2D_BIAS(_BaseColorMap2, sampler_BaseColorMap0, layerTexCoord.base2.uv, textureBias).rgb * _BaseColor2.rgb;
    float3 baseMeanColor3 = SAMPLE_TEXTURE2D_BIAS(_BaseColorMap3, sampler_BaseColorMap0, layerTexCoord.base3.uv, textureBias).rgb * _BaseColor3.rgb;

    //float3 toto1 = lerp(baseMeanColor1, baseMeanColor0, _InheritBaseColor1) + baseColor1 - baseMeanColor1;
    //float3 toto2 = lerp(baseMeanColor2, baseMeanColor0, _InheritBaseColor2) + baseColor2 - baseMeanColor2;
    //float3 toto3 = lerp(baseMeanColor3, baseMeanColor0, _InheritBaseColor3) + baseColor3 - baseMeanColor3;

    //return BlendLayeredFloat3(baseColor0, toto1, toto3, toto3, weights);

    float3 meanColor = BlendLayeredFloat3(baseMeanColor0, baseMeanColor1, baseMeanColor2, baseMeanColor3, weights);
    float3 baseColor = BlendLayeredFloat3(baseColor0, baseColor1, baseColor2, baseColor3, weights);

    //return lerp(baseMeanColor1, baseColor0, _InheritBaseColor1) + (baseColor1 - baseMeanColor1);
    return lerp(meanColor, baseColor0, inheritBaseColor) + (baseColor - meanColor);
}

void ComputeLayerWeights(FragInputs input, LayerTexCoord layerTexCoord, float4 inputAlphaMask, out float outWeights[_MAX_LAYER])
{
    float height0 = SampleHeightmap0(layerTexCoord);
    float height1 = SampleHeightmap1(layerTexCoord, _HeightCenterOffset1, _HeightFactor1);
    float height2 = SampleHeightmap2(layerTexCoord, _HeightCenterOffset2, _HeightFactor2);
    float height3 = SampleHeightmap3(layerTexCoord, _HeightCenterOffset3, _HeightFactor3);

    float4 heights = float4(height0, height1, height2, height3);
    // Mask Values : Layer 1, 2, 3 are r, g, b
    float3 inputMaskValues = SAMPLE_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, input.texCoord0).rgb;

    // Mutually exclusive with _HEIGHT_BASED_BLEND
#if defined(_LAYER_MASK_VERTEX_COLOR_MUL) // Used when no layer mask is set
    inputMaskValues *= input.color.rgb;
#elif defined(_LAYER_MASK_VERTEX_COLOR_ADD) || defined(_HEIGHT_BASED_BLEND_V2) // When layer mask is set, color is additive to enable user to override it.
    inputMaskValues = saturate(inputMaskValues + input.color.rgb * 2.0 - 1.0);
#endif

#if defined(_HEIGHT_BASED_BLEND)
    #if !defined(_HEIGHT_BASED_BLEND_V2)
        float baseLayerHeight = heights.x;
        baseLayerHeight = ApplyHeightBasedBlend(inputMaskValues.r, baseLayerHeight, heights.y, _HeightOffset1, _HeightFactor1, _BlendSize1, input.color.r);
        baseLayerHeight = ApplyHeightBasedBlend(inputMaskValues.g, baseLayerHeight, heights.z, _HeightOffset2 + _HeightOffset1, _HeightFactor2, _BlendSize2, input.color.g);
        ApplyHeightBasedBlend(inputMaskValues.b, baseLayerHeight, heights.w, _HeightOffset3 + _HeightOffset2 + _HeightOffset1, _HeightFactor3, _BlendSize3, input.color.b);
    #else

        float3 minOpaParam = float3(_MinimumOpacity1, _MinimumOpacity2, _MinimumOpacity3);
        float3 remapedOpacity = (float3(1.0, 1.0, 1.0) - minOpaParam) * inputAlphaMask.yzw + minOpaParam; // Remap opacity mask from [0..1] to [minOpa..1]
        float3 opacityAsDensity = saturate((inputAlphaMask.yzw - (float3(1.0, 1.0, 1.0) - inputMaskValues))*20.0);

        float3 useOpacityAsDensityParam = float3(_OpacityAsDensity1, _OpacityAsDensity2, _OpacityAsDensity3);
        inputMaskValues = lerp(inputMaskValues * remapedOpacity, opacityAsDensity, useOpacityAsDensityParam);

        // HACK, use height0 to avoid compiler error for unused sampler
        // To remove once we have POM
        heights.y += (heights.x * 0.0001);

        inputMaskValues = ApplyHeightBasedBlendV2(inputMaskValues, heights.yzw, float3(_BlendUsingHeight1, _BlendUsingHeight2, _BlendUsingHeight3));
    #endif
#endif

    ComputeMaskWeights(inputMaskValues, outWeights);

//#if defined(_HEIGHT_BASED_BLEND_V2)
//    float inheritBaseHeight = BlendLayeredScalar(0.0f, _InheritBaseHeight1, _InheritBaseHeight2, _InheritBaseHeight3, weights);
//    float blendedLayerHeight = BlendLayeredScalar(heights.x, heights.y, heights.z, heights.w, weights);
//    float finalHeight = heights.x * inheritBaseHeight + blendedLayerHeight;
//    // Use this for POM/Tesselation
//#endif
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

#if defined(_HEIGHT_BASED_BLEND)
    surfaceData.baseColor = ComputeInheritedColor(surfaceData0.baseColor, surfaceData1.baseColor, surfaceData2.baseColor, surfaceData3.baseColor, alpha, layerTexCoord, weights);
#else
    surfaceData.baseColor = SURFACEDATA_BLEND_COLOR(surfaceData, baseColor, weights);
#endif
    surfaceData.specularOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, specularOcclusion, weights);
    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);

    float3 normalTS;
#if defined(_HEIGHT_BASED_BLEND)
    normalTS = ComputeInheritedNormalTS(input, normalTS0, normalTS1, normalTS2, normalTS3, layerTexCoord, weights);
#else
    normalTS = BlendLayeredFloat3(normalTS0, normalTS1, normalTS2, normalTS3, weights);
#endif

    surfaceData.normalWS = TransformTangentToWorld(normalTS, input.tangentToWorld);
    surfaceData.tangentWS = input.tangentToWorld[0].xyz;

    // NdotV should not be negative for visible pixels, but it can happen due to the
    // perspective projection and the normal mapping + decals. In that case, the normal
    // should be modified to become valid (i.e facing the camera) to avoid weird artifacts.
    // Note: certain applications (e.g. SpeedTree) make use of double-sided lighting.
    // This will  potentially reduce the length of the normal at edges of geometry.
    bool twoSided = false;    
    GetShiftedNdotV(surfaceData.normalWS, V, twoSided);

    // Orthonormalize the basis vectors using the Gram-Schmidt process.
    // We assume that the length of the surface normal is sufficiently close to 1.
    surfaceData.tangentWS = normalize(surfaceData.tangentWS - dot(surfaceData.tangentWS, surfaceData.normalWS));

    // Init other unused parameter
    surfaceData.materialId = 0;
    surfaceData.anisotropy = 0;
    surfaceData.specular = 0.04;
    surfaceData.subSurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subSurfaceProfile = 0;
    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

    GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
}

#endif // #ifndef LAYERED_LIT_SHADER

#ifdef TESSELLATION_ON
#include "LitTessellation.hlsl" // Must be after GetLayerTexCoord() declaration
#endif
