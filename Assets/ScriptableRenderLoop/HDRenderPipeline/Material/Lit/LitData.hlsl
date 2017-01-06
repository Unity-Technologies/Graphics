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

    builtinData.velocity = CalculateVelocity(input.positionCS, input.previousPositionCS);

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

// TODO: Handle BC5 format, currently this code is for DXT5nm
// THis function below must call UnpackNormalmapRGorAG
float3 SampleNormalLayer(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights)
{
    if (layerUV.isTriplanar)
    {
        float3 val = float3(0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * UnpackNormalAG(SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvYZ));
        if (weights.y > 0.0)
            val += weights.y * UnpackNormalAG(SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvZX));
        if (weights.z > 0.0)
            val += weights.z * UnpackNormalAG(SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvXY));

        return normalize(val);
    }
    else
    {
        return UnpackNormalAG(SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uv));
    }
}

// This version is for normalmap with AG encoding only (use with details map)
float3 SampleNormalLayerAG(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights)
{
    if (layerUV.isTriplanar)
    {
        float3 val = float3(0.0, 0.0, 0.0);

        if (weights.x > 0.0)
            val += weights.x * UnpackNormalAG(SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvYZ));
        if (weights.y > 0.0)
            val += weights.y * UnpackNormalAG(SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvZX));
        if (weights.z > 0.0)
            val += weights.z * UnpackNormalAG(SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvXY));

        return normalize(val);
    }
    else
    {
        return UnpackNormalAG(SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uv));
    }
}

// Macro to improve readibility of surface data
#define SAMPLE_LAYER_TEXTURE2D(textureName, samplerName, coord) SampleLayer(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights)
#define SAMPLE_LAYER_NORMALMAP(textureName, samplerName, coord) SampleNormalLayer(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights)
#define SAMPLE_LAYER_NORMALMAP_AG(textureName, samplerName, coord) SampleNormalLayerAG(TEXTURE2D_PARAM(textureName, samplerName), coord, layerTexCoord.weights)

// Transforms 2D UV by scale/bias property
#define TRANSFORM_TEX(tex,name) ((tex.xy) * name##_ST.xy + name##_ST.zw)

#ifndef LAYERED_LIT_SHADER

#define LAYER_INDEX 0
#define ADD_IDX(Name) Name
#define ADD_ZERO_IDX(Name) Name
#include "LitSurfaceData.hlsl"

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);

#ifdef _MAPPING_TRIPLANAR
    // one weight for each direction XYZ - Use vertex normal for triplanar
    layerTexCoord.weights = ComputeTriplanarWeights(input.tangentToWorld[2].xyz);
#endif

    // Be sure that the compiler is aware that we don't touch UV1 to UV3 for base layer in case of non layer shader
    // so it can remove code
    _UVMappingMask.yzw = float3(0.0, 0.0, 0.0);
    bool isTriplanar = false;
#ifdef _MAPPING_TRIPLANAR
    isTriplanar = true;
#endif
    ComputeLayerTexCoord(input, isTriplanar, layerTexCoord);
    // Transform view vector in tangent space
    float3 viewDirTS = TransformWorldToTangent(V, input.tangentToWorld);
    ApplyDisplacement(input, viewDirTS, layerTexCoord);
    float depthOffset = 0.0;

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, builtinData.depthOffset, posInput);
    ApplyDepthOffsetAttribute(depthOffset, input);
#endif

    float3 normalTS;
    float alpha = GetSurfaceData(input, layerTexCoord, surfaceData, normalTS);
    GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
    surfaceData.normalWS = TransformTangentToWorld(normalTS, input.tangentToWorld);
}

#else

#define ADD_ZERO_IDX(Name) Name##0

// Generate function for all layer
#define LAYER_INDEX 0
#define ADD_IDX(Name) Name##0
#include "LitSurfaceData.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX

#define LAYER_INDEX 1
#define ADD_IDX(Name) Name##1
#include "LitSurfaceData.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX

#define LAYER_INDEX 2
#define ADD_IDX(Name) Name##2
#include "LitSurfaceData.hlsl"
#undef LAYER_INDEX
#undef ADD_IDX

#define LAYER_INDEX 3
#define ADD_IDX(Name) Name##3
#include "LitSurfaceData.hlsl"
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

    edgeBlendStrength = max(0.001, edgeBlendStrength);

    float heightThreshold = previousLayerHeight + edgeBlendStrength;

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


#define SURFACEDATA_BLEND_COLOR(surfaceData, name, mask) BlendLayeredFloat3(surfaceData##0.##name, surfaceData##1.##name, surfaceData##2.##name, surfaceData##3.##name, mask);
#define SURFACEDATA_BLEND_SCALAR(surfaceData, name, mask) BlendLayeredScalar(surfaceData##0.##name, surfaceData##1.##name, surfaceData##2.##name, surfaceData##3.##name, mask);
#define PROP_BLEND_SCALAR(name, mask) BlendLayeredScalar(name##0, name##1, name##2, name##3, mask);

void GetSurfaceAndBuiltinData(FragInputs input, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    LayerTexCoord layerTexCoord;
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);

#if defined(_LAYER_MAPPING_TRIPLANAR_0) || defined(_LAYER_MAPPING_TRIPLANAR_1) || defined(_LAYER_MAPPING_TRIPLANAR_2) || defined(_LAYER_MAPPING_TRIPLANAR_3)
    // one weight for each direction XYZ - Use vertex normal for triplanar
    layerTexCoord.weights = ComputeTriplanarWeights(input.tangentToWorld[2].xyz);
#endif

    bool isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_0
    isTriplanar = true;
#endif
    ComputeLayerTexCoord0(input, isTriplanar, layerTexCoord);
    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_1
    isTriplanar = true;
#endif
    ComputeLayerTexCoord1(input, isTriplanar, layerTexCoord);
    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_2
    isTriplanar = true;
#endif
    ComputeLayerTexCoord2(input, isTriplanar, layerTexCoord);
    isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_3
    isTriplanar = true;
#endif
    ComputeLayerTexCoord3(input, isTriplanar, layerTexCoord);

    // Transform view vector in tangent space
    float3 viewDirTS = TransformWorldToTangent(V, input.tangentToWorld);
    float height0 = ApplyDisplacement0(input, viewDirTS, layerTexCoord);
    float height1 = ApplyDisplacement1(input, viewDirTS, layerTexCoord);
    float height2 = ApplyDisplacement2(input, viewDirTS, layerTexCoord);
    float height3 = ApplyDisplacement3(input, viewDirTS, layerTexCoord);
    float depthOffset = 0.0;

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(V, builtinData.depthOffset, posInput);
    ApplyDepthOffsetAttribute(depthOffset, input);
#endif

    SurfaceData surfaceData0;
    SurfaceData surfaceData1;
    SurfaceData surfaceData2;
    SurfaceData surfaceData3;
    float3 normalTS0;
    float3 normalTS1;
    float3 normalTS2;
    float3 normalTS3;
    float alpha0 = GetSurfaceData0(input, layerTexCoord, surfaceData0, normalTS0);
    float alpha1 = GetSurfaceData1(input, layerTexCoord, surfaceData1, normalTS1);
    float alpha2 = GetSurfaceData2(input, layerTexCoord, surfaceData2, normalTS2);
    float alpha3 = GetSurfaceData3(input, layerTexCoord, surfaceData3, normalTS3);

    // Mask Values : Layer 1, 2, 3 are r, g, b
    float3 maskValues = SAMPLE_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, input.texCoord0).rgb;

#if defined(_LAYER_MASK_VERTEX_COLOR)
    maskValues *= input.vertexColor.rgb;
#endif

#if defined(_HEIGHT_BASED_BLEND)
    float baseLayerHeight = height0;
    baseLayerHeight = ApplyHeightBasedBlend(maskValues.r, baseLayerHeight, height1, _HeightOffset1, _HeightFactor1, _BlendSize1, input.vertexColor.r);
    baseLayerHeight = ApplyHeightBasedBlend(maskValues.g, baseLayerHeight, height2, _HeightOffset2 + _HeightOffset1, _HeightFactor2, _BlendSize2, input.vertexColor.g);
    ApplyHeightBasedBlend(maskValues.b, baseLayerHeight, height3, _HeightOffset3 + _HeightOffset2 + _HeightOffset1, _HeightFactor3, _BlendSize3, input.vertexColor.b);
#endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(maskValues, weights);

    surfaceData.baseColor = SURFACEDATA_BLEND_COLOR(surfaceData, baseColor, weights);
    surfaceData.specularOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, specularOcclusion, weights);
    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);

    float3 normalTS;
#if defined(_HEIGHT_BASED_BLEND)
    float _InheritBaseLayer0 = 1.0f; // Default value for lerp when all weights but base layer are zero.

    // Compute the combined inheritance factor of layers 1,2 and 3
    float inheritFactor = PROP_BLEND_SCALAR(_InheritBaseLayer, weights);
    float3 vertexNormalTS = float3(0.0, 0.0, 1.0);
    // The idea here is to lerp toward vertex normal. This way when we don't want to inherit, we will combine layer 1/2/3 normal with a vertex normal which is neutral.
    float3 baseLayerNormalTS = normalize(lerp(vertexNormalTS, normalTS0, inheritFactor));
    // Blend layer 1/2/3 normals before combining to the base layer. Again we need to have a neutral value for base layer (vertex normal) in case all weights are zero.
    float3 layersNormalTS = BlendLayeredFloat3(vertexNormalTS, normalTS1, normalTS2, normalTS3, weights);
    normalTS = BlendNormal(baseLayerNormalTS, layersNormalTS);
#else
    normalTS = BlendLayeredFloat3(normalTS0, normalTS1, normalTS2, normalTS3, weights);
#endif
    surfaceData.normalWS = TransformTangentToWorld(normalTS, input.tangentToWorld);

    // Init other unused parameter
    surfaceData.materialId = 0;
    surfaceData.tangentWS = input.tangentToWorld[0].xyz;
    surfaceData.anisotropy = 0;
    surfaceData.specular = 0.04;
    surfaceData.subSurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subSurfaceProfile = 0;
    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

    float alpha = PROP_BLEND_SCALAR(alpha, weights);
    GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
}

#endif // #ifndef LAYERED_LIT_SHADER
