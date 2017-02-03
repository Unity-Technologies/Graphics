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

    // Dedicated for blend mask
    LayerUV blendMask;
#endif

    // triplanar weight
    float3 triplanarWeights;
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

    bool isTriplanar = false;
#ifdef _MAPPING_TRIPLANAR
    // one weight for each direction XYZ - Use vertex normal for triplanar
    layerTexCoord.triplanarWeights = ComputeTriplanarWeights(normalWS);
    isTriplanar = true;
#endif

    // Be sure that the compiler is aware that we don't touch UV1 to UV3 for main layer so it can optimize code
    _UVMappingMask.yzw = float3(0.0, 0.0, 0.0);
    ComputeLayerTexCoord(   texCoord0, texCoord1, texCoord2, texCoord3, 
                            positionWS, normalWS, isTriplanar, layerTexCoord);
}

float GetMaxDisplacement()
{
    float maxDisplacement = 0.0;
#if defined(_HEIGHTMAP)
    maxDisplacement = _HeightAmplitude;
#endif
    return maxDisplacement;
}

// Return the minimun uv size for all layers including triplanar
float2 GetMinUvSize(LayerTexCoord layerTexCoord)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

#if defined(_HEIGHTMAP)
    if (layerTexCoord.base.isTriplanar)
    {
        minUvSize = min(layerTexCoord.base.uvYZ * _HeightMap_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base.uvZX * _HeightMap_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base.uvXY * _HeightMap_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base.uv * _HeightMap_TexelSize.zw, minUvSize);
    }
#endif

    return minUvSize;
}

struct PerPixelHeightDisplacementParam
{
    float2 uv;
};

// Calculate displacement for per vertex displacement mapping
float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
{
    // Note: No multiply by amplitude here. This is include in the maxHeight provide to POM
    // Tiling is automatically handled correctly here.
    return SAMPLE_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, param.uv + texOffsetCurrent, lod).r;
}

#include "PerPixelDisplacement.hlsl"

void ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord)
{
    bool ppdEnable = false;
    bool isPlanar = false;
    bool isTriplanar = false;

#if defined(_PER_PIXEL_DISPLACEMENT) &&  defined(_HEIGHTMAP)
    ppdEnable = true;
    isPlanar = layerTexCoord.base.isPlanar;
    isTriplanar = layerTexCoord.base.isTriplanar;
#endif

    if (ppdEnable)
    {
        // See comment in layered version for details
        float maxHeight = GetMaxDisplacement();
        float2 minUvSize = GetMinUvSize(layerTexCoord);
        float lod = ComputeTextureLOD(minUvSize);

        PerPixelHeightDisplacementParam ppdParam;

        // We need to calculate the texture space direction. It depends on the mapping.
        if (isTriplanar)
        {
            // TODO: implement. Require 3 call to POM + dedicated viewDirTS based on triplanar convention
            // apply the 3 offset on all layers
            /*

            ppdParam.uv = layerTexCoord.base0.uvYZ;

            float3 viewDirTS = ;
            int numSteps = (int)lerp(_PPDMaxSamples, _PPDMinSamples, abs(viewDirTS.z));
            ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirTS, maxHeight, ppdParam);

            (...)
            */
        }
        else
        {
            ppdParam.uv = layerTexCoord.base.uv;

            // For planar the view vector is the world view vector (unless we want to support object triplanar ? and in this case used TransformWorldToObject)
            // TODO: do we support object triplanar ? See ComputeLayerTexCoord
            float3 viewDirTS = isPlanar ? float3(-V.xz, V.y) : TransformWorldToTangent(V, input.tangentToWorld);
            int numSteps = (int)lerp(_PPDMaxSamples, _PPDMinSamples, viewDirTS.z);
            float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirTS, maxHeight, ppdParam);

            // Apply offset to all UVSet
            layerTexCoord.base.uv += offset;
            layerTexCoord.details.uv += offset;
        }
    }
}

// Calculate displacement for per vertex displacement mapping
float ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
{
    return (SAMPLE_LAYER_TEXTURE2D_LOD(_HeightMap, sampler_HeightMap, layerTexCoord.base, lod).r - _HeightCenter) * _HeightAmplitude;
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
    // Done one time for all layered - cumulate with spec occ alpha for now
    surfaceData.specularOcclusion *= GetHorizonOcclusion(V, surfaceData.normalWS, input.tangentToWorld[2].xyz, _HorizonFade);

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

void GetLayerTexCoord(float2 texCoord0, float2 texCoord1, float2 texCoord2, float2 texCoord3,
                      float3 positionWS, float3 normalWS, out LayerTexCoord layerTexCoord)
{
    ZERO_INITIALIZE(LayerTexCoord, layerTexCoord);

#if defined(_LAYER_MAPPING_TRIPLANAR_0) || defined(_LAYER_MAPPING_TRIPLANAR_1) || defined(_LAYER_MAPPING_TRIPLANAR_2) || defined(_LAYER_MAPPING_TRIPLANAR_3)
    // one weight for each direction XYZ - Use vertex normal for triplanar
    layerTexCoord.triplanarWeights = ComputeTriplanarWeights(normalWS);
#endif

    bool isTriplanar = false;
#ifdef _LAYER_MAPPING_TRIPLANAR_0
    isTriplanar = true;
#endif

    // Be sure that the compiler is aware that we don't touch UV1 to UV3 for main layer so it can optimize code
    _UVMappingMask0.yzw = float3(0.0, 0.0, 0.0);
    // Note: Our BlendMask use the same uv mapping than the base layer but with its own tiling.
    // Here we get a first time the base0 but with _LayerTilingBlendMask. Save the result and recall the function regularly for the main layer.
    // It is just to share code.
    ComputeLayerTexCoord0(  texCoord0, float2(0.0, 0.0), float2(0.0, 0.0), float2(0.0, 0.0),
                            positionWS, normalWS, isTriplanar, layerTexCoord, _LayerTilingBlendMask);

    layerTexCoord.blendMask = layerTexCoord.base0;

    ComputeLayerTexCoord0(  texCoord0, float2(0.0, 0.0), float2(0.0, 0.0), float2(0.0, 0.0),
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

#if defined(_HEIGHTMAP0)
#define sampler_ShareHeightMap sampler_HeightMap0
#elif defined(_HEIGHTMAP1)
#define sampler_ShareHeightMap sampler_HeightMap1
#elif defined(_HEIGHTMAP2)
#define sampler_ShareHeightMap sampler_HeightMap2
#elif defined(_HEIGHTMAP3)
#define sampler_ShareHeightMap sampler_HeightMap3
#endif

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
    float4 blendMasks = useLodSampling ? SAMPLE_LAYER_TEXTURE2D_LOD(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.blendMask, lod) : SAMPLE_LAYER_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, layerTexCoord.blendMask);

#if defined(_LAYER_MASK_VERTEX_COLOR_MUL)
    blendMasks *= vertexColor;
#elif defined(_LAYER_MASK_VERTEX_COLOR_ADD)
    blendMasks = saturate(blendMasks + vertexColor * 2.0 - 1.0);
#endif

    return blendMasks;
}

// Return the maximun amplitude use by all enabled heightmap
// use for tessellation culling and per pixel displacement
float GetMaxDisplacement()
{
    float maxDisplacement = 0.0;

#if defined(_HEIGHTMAP0)
    maxDisplacement = max(  _LayerHeightAmplitude0, maxDisplacement);
#endif

#if defined(_HEIGHTMAP1)
    maxDisplacement = max(  _LayerHeightAmplitude1
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            +_LayerHeightAmplitude0 * _InheritBaseHeight1
                            #endif
                            , maxDisplacement);
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    maxDisplacement = max(  _LayerHeightAmplitude2
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            +_LayerHeightAmplitude0 * _InheritBaseHeight2
                            #endif
                            , maxDisplacement);
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    maxDisplacement = max(  _LayerHeightAmplitude3
                            #if defined(_MAIN_LAYER_INFLUENCE_MODE)
                            +_LayerHeightAmplitude0 * _InheritBaseHeight3
                            #endif
                            , maxDisplacement);
#endif
#endif

    return maxDisplacement;
}

// Return the minimun uv size for all layers including triplanar
float2 GetMinUvSize(LayerTexCoord layerTexCoord)
{
    float2 minUvSize = float2(FLT_MAX, FLT_MAX);

#if defined(_HEIGHTMAP0)
    if (layerTexCoord.base0.isTriplanar)
    {
        minUvSize = min(layerTexCoord.base0.uvYZ * _HeightMap0_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base0.uvZX * _HeightMap0_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base0.uvXY * _HeightMap0_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base0.uv * _HeightMap0_TexelSize.zw, minUvSize);
    }
#endif

#if defined(_HEIGHTMAP1)
    if (layerTexCoord.base1.isTriplanar)
    {
        minUvSize = min(layerTexCoord.base1.uvYZ * _HeightMap1_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base1.uvZX * _HeightMap1_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base1.uvXY * _HeightMap1_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base1.uv * _HeightMap1_TexelSize.zw, minUvSize);
    }
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    if (layerTexCoord.base2.isTriplanar)
    {
        minUvSize = min(layerTexCoord.base2.uvYZ * _HeightMap2_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base2.uvZX * _HeightMap2_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base2.uvXY * _HeightMap2_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base2.uv * _HeightMap2_TexelSize.zw, minUvSize);
    }
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    if (layerTexCoord.base3.isTriplanar)
    {
        minUvSize = min(layerTexCoord.base3.uvYZ * _HeightMap3_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base3.uvZX * _HeightMap3_TexelSize.zw, minUvSize);
        minUvSize = min(layerTexCoord.base3.uvXY * _HeightMap3_TexelSize.zw, minUvSize);
    }
    else
    {
        minUvSize = min(layerTexCoord.base3.uv * _HeightMap3_TexelSize.zw, minUvSize);
    }
#endif
#endif

    return minUvSize;
}

struct PerPixelHeightDisplacementParam
{
    float weights[_MAX_LAYER];
    float2 uv[_MAX_LAYER];
    float mainHeightInfluence;
};

// Calculate displacement for per vertex displacement mapping
float ComputePerPixelHeightDisplacement(float2 texOffsetCurrent, float lod, PerPixelHeightDisplacementParam param)
{
#if defined(_HEIGHTMAP0) || defined(_HEIGHTMAP1) || defined(_HEIGHTMAP2) || defined(_HEIGHTMAP3)
    // Note: No multiply by amplitude here, this is bake into the weights and apply in BlendLayeredScalar
    // The amplitude is normalize to be able to work with POM algorithm
    // Tiling is automatically handled correctly here as we use 4 differents uv even if they come from the same UVSet (they include the tiling)
    float height0 = SAMPLE_TEXTURE2D_LOD(_HeightMap0, sampler_ShareHeightMap, param.uv[0] + texOffsetCurrent, lod).r;
    float height1 = SAMPLE_TEXTURE2D_LOD(_HeightMap1, sampler_ShareHeightMap, param.uv[1] + texOffsetCurrent, lod).r;
    float height2 = SAMPLE_TEXTURE2D_LOD(_HeightMap2, sampler_ShareHeightMap, param.uv[2] + texOffsetCurrent, lod).r;
    float height3 = SAMPLE_TEXTURE2D_LOD(_HeightMap3, sampler_ShareHeightMap, param.uv[3] + texOffsetCurrent, lod).r;
    SetEnabledHeightByLayer(height0, height1, height2, height3);  // Not needed as already put in weights but paranoid mode
    return BlendLayeredScalar(height0, height1, height2, height3, param.weights) + height0 * param.mainHeightInfluence;
#else
    return 0.0;
#endif
}

#include "PerPixelDisplacement.hlsl"

// PPD is affecting only one mapping at the same time, mean we need to execute it for each mapping (UV0, UV1, 3 times for triplanar etc..)
// We chose to not support all this case that are extremely hard to manage (for example mixing different mapping, mean it also require different tangent space that is not supported in Unity)
// For these reasons we put the following rules
// Rules:
// - Mapping is the same for all layers that use an Heightmap (i.e all are UV, planar or triplanar)
// - Mapping UV is UV0 only because we need to convert view vector in texture space and this is only available for UV0
// - Heightmap can be enabled per layer
// - Blend Mask use same mapping as main layer (UVO, Planar, Triplanar)
// From these rules it mean that PPD is enable only if the user 1) ask for it, 2) if there is one heightmap enabled on active layer, 3) if mapping is the same for all layer respecting 2), 4) if mapping is UV0, planar or triplanar mapping
// Most contraint are handled by the inspector (i.e the UI) like the mapping constraint and is assumed in the shader.
void ApplyPerPixelDisplacement(FragInputs input, float3 V, inout LayerTexCoord layerTexCoord)
{
    bool ppdEnable = false;
    bool isPlanar = false;
    bool isTriplanar = false;

#ifdef _PER_PIXEL_DISPLACEMENT

    // To know if we are planar or triplanar just need to check if any of the active heightmap layer is true as they are enforce to be the same mapping
#if defined(_HEIGHTMAP0)
    ppdEnable = true;
    isPlanar = layerTexCoord.base0.isPlanar;
    isTriplanar = layerTexCoord.base0.isTriplanar;
#endif

#if defined(_HEIGHTMAP1)
    ppdEnable = true;
    isPlanar = layerTexCoord.base1.isPlanar;
    isTriplanar = layerTexCoord.base1.isTriplanar;
#endif

#if _LAYER_COUNT >= 3
#if defined(_HEIGHTMAP2)
    ppdEnable = true;
    isPlanar = layerTexCoord.base2.isPlanar;
    isTriplanar = layerTexCoord.base2.isTriplanar;
#endif
#endif

#if _LAYER_COUNT >= 4
#if defined(_HEIGHTMAP3)
    ppdEnable = true;
    isPlanar = layerTexCoord.base3.isPlanar;
    isTriplanar = layerTexCoord.base3.isTriplanar;
#endif
#endif

#endif // _PER_PIXEL_DISPLACEMENT

    if (ppdEnable)
    {
        // Even if we use same mapping we can have different tiling. For per pixel displacement we will perform the ray marching with already tiled uv
        float maxHeight = GetMaxDisplacement();
        // Compute lod as we will sample inside a loop(so can't use regular sampling)
        // Note: It appear that CALCULATE_TEXTURE2D_LOD only return interger lod. We want to use float lod to have smoother transition and fading, so do our own calculation.
        // Approximation of lod to used. Be conservative here, we will take the highest mip of all layers.
        // Remember, we assume that we used the same mapping for all layer, so only size matter.
        float2 minUvSize = GetMinUvSize(layerTexCoord);
        float lod = ComputeTextureLOD(minUvSize);

        // Calculate blend weights
        float4 blendMasks = GetBlendMask(layerTexCoord, input.color);

        float weights[_MAX_LAYER];
        ComputeMaskWeights(blendMasks, weights);

        // Be sure we are not considering weight here were there is no heightmap
        SetEnabledHeightByLayer(weights[0], weights[1], weights[2], weights[3]);

        PerPixelHeightDisplacementParam ppdParam;
#if defined(_MAIN_LAYER_INFLUENCE_MODE)        
        // For per pixel displacement we need to have normalized height scale to calculate the interesection (required by the algorithm we use)
        // mean that we will normalize by the highest amplitude.
        // We store this normalization factor with the weights as it will be multiply by the readed height.
        ppdParam.weights[0] = weights[0] * (_LayerHeightAmplitude0) / maxHeight;
        ppdParam.weights[1] = weights[1] * (_LayerHeightAmplitude1 + _LayerHeightAmplitude0 * _InheritBaseHeight1) / maxHeight;
        ppdParam.weights[2] = weights[2] * (_LayerHeightAmplitude2 + _LayerHeightAmplitude0 * _InheritBaseHeight2) / maxHeight;
        ppdParam.weights[3] = weights[3] * (_LayerHeightAmplitude3 + _LayerHeightAmplitude0 * _InheritBaseHeight3) / maxHeight;

        // Think that inheritbasedheight will be 0 if height0 is fully visible in weights. So there is no double contribution of height0
        float mainHeightInfluence = BlendLayeredScalar(0.0, _InheritBaseHeight1, _InheritBaseHeight2, _InheritBaseHeight3, weights);
        ppdParam.mainHeightInfluence = mainHeightInfluence;
#else
        [unroll]
        for (int i = 0; i < _MAX_LAYER; ++i)
        {
            ppdParam.weights[i] = weights[i];
        }
        ppdParam.mainHeightInfluence = 0.0;
#endif

        // We need to calculate the texture space direction. It depends on the mapping.
        if (isTriplanar)
        {
            // TODO: implement. Require 3 call to POM + dedicated viewDirTS based on triplanar convention
            // apply the 3 offset on all layers
            /*

            ppdParam.uv[0] = layerTexCoord.base0.uvYZ;
            ppdParam.uv[1] = layerTexCoord.base1.uvYZ;
            ppdParam.uv[2] = layerTexCoord.base2.uvYZ;
            ppdParam.uv[3] = layerTexCoord.base3.uvYZ;

            float3 viewDirTS = ;
            int numSteps = (int)lerp(_PPDMaxSamples, _PPDMinSamples, abs(viewDirTS.z));
            ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirTS, maxHeight, ppdParam);

            // Apply to all uvYZ

            // Repeat for uvZX

            // Repeat for uvXY

            // Apply to all layer that used triplanar
            */
        }
        else
        {
            ppdParam.uv[0] = layerTexCoord.base0.uv;
            ppdParam.uv[1] = layerTexCoord.base1.uv;
            ppdParam.uv[2] = layerTexCoord.base2.uv;
            ppdParam.uv[3] = layerTexCoord.base3.uv;

            // For planar the view vector is the world view vector (unless we want to support object triplanar ? and in this case used TransformWorldToObject)
            // TODO: do we support object triplanar ? See ComputeLayerTexCoord
            float3 viewDirTS = isPlanar ? float3(-V.xz, V.y) : TransformWorldToTangent(V, input.tangentToWorld);
            int numSteps = (int)lerp(_PPDMaxSamples, _PPDMinSamples, viewDirTS.z);
            float2 offset = ParallaxOcclusionMapping(lod, _PPDLodThreshold, numSteps, viewDirTS, maxHeight, ppdParam);

            // Apply offset to all planar uvset
            // _UVMappingPlanar0 will be 1.0 is planar is used - _UVMappingMask0.x will be 1.0 is UVSet0 is used;
            float4 offsetWeights = isPlanar ? float4(_UVMappingPlanar0, _UVMappingPlanar1, _UVMappingPlanar2, _UVMappingPlanar3) : float4(_UVMappingMask0.x, _UVMappingMask1.x, _UVMappingMask2.x, _UVMappingMask3.x);
            
            layerTexCoord.base0.uv += offsetWeights.x * offset;
            layerTexCoord.base1.uv += offsetWeights.y * offset;
            layerTexCoord.base2.uv += offsetWeights.z * offset;
            layerTexCoord.base3.uv += offsetWeights.w * offset;

            offsetWeights = isPlanar ? float4(_UVMappingPlanar0, _UVMappingPlanar1, _UVMappingPlanar2, _UVMappingPlanar3) : float4(_UVDetailsMappingMask0.x, _UVDetailsMappingMask1.x, _UVDetailsMappingMask2.x, _UVDetailsMappingMask3.x);

            layerTexCoord.details0.uv += offsetWeights.x * offset;
            layerTexCoord.details1.uv += offsetWeights.y * offset;
            layerTexCoord.details2.uv += offsetWeights.z * offset;
            layerTexCoord.details3.uv += offsetWeights.w * offset;
        }
    }
}

// Calculate displacement for per vertex displacement mapping
float ComputePerVertexDisplacement(LayerTexCoord layerTexCoord, float4 vertexColor, float lod)
{
    float4 blendMasks = GetBlendMask(layerTexCoord, vertexColor, true, lod);

    float weights[_MAX_LAYER];
    ComputeMaskWeights(blendMasks, weights);

#if defined(_HEIGHTMAP0) || defined(_HEIGHTMAP1) || defined(_HEIGHTMAP2) || defined(_HEIGHTMAP3)
    float height0 = (SAMPLE_LAYER_TEXTURE2D_LOD(_HeightMap0, sampler_ShareHeightMap, layerTexCoord.base0, lod).r - _LayerCenterOffset0) * _LayerHeightAmplitude0;
    float height1 = (SAMPLE_LAYER_TEXTURE2D_LOD(_HeightMap1, sampler_ShareHeightMap, layerTexCoord.base1, lod).r - _LayerCenterOffset1) * _LayerHeightAmplitude1;
    float height2 = (SAMPLE_LAYER_TEXTURE2D_LOD(_HeightMap2, sampler_ShareHeightMap, layerTexCoord.base2, lod).r - _LayerCenterOffset2) * _LayerHeightAmplitude2;
    float height3 = (SAMPLE_LAYER_TEXTURE2D_LOD(_HeightMap3, sampler_ShareHeightMap, layerTexCoord.base3, lod).r - _LayerCenterOffset3) * _LayerHeightAmplitude3;
    SetEnabledHeightByLayer(height0, height1, height2, height3);
    float heightResult = BlendLayeredScalar(height0, height1, height2, height3, weights);

#if defined(_MAIN_LAYER_INFLUENCE_MODE)
    // Think that inheritbasedheight will be 0 if height0 is fully visible in weights. So there is no double contribution of height0
    float inheritBaseHeight = BlendLayeredScalar(0.0, _InheritBaseHeight1, _InheritBaseHeight2, _InheritBaseHeight3, weights);
    return heightResult + height0 * inheritBaseHeight;
#endif

#else
    float heightResult = 0.0;
#endif
    return heightResult;
}

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
    float height0 = (SAMPLE_LAYER_TEXTURE2D(_HeightMap0, sampler_ShareHeightMap, layerTexCoord.base0).r - _LayerCenterOffset0) * _LayerHeightAmplitude0;
    float height1 = (SAMPLE_LAYER_TEXTURE2D(_HeightMap1, sampler_ShareHeightMap, layerTexCoord.base1).r - _LayerCenterOffset1) * _LayerHeightAmplitude1;
    float height2 = (SAMPLE_LAYER_TEXTURE2D(_HeightMap2, sampler_ShareHeightMap, layerTexCoord.base2).r - _LayerCenterOffset2) * _LayerHeightAmplitude2;
    float height3 = (SAMPLE_LAYER_TEXTURE2D(_HeightMap3, sampler_ShareHeightMap, layerTexCoord.base3).r - _LayerCenterOffset3) * _LayerHeightAmplitude3;
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
    float3 baseMeanColor0 = SAMPLE_LAYER_TEXTURE2D_BIAS(_BaseColorMap0, sampler_BaseColorMap0, layerTexCoord.base0, textureBias).rgb *_BaseColor0.rgb;
    float3 baseMeanColor1 = SAMPLE_LAYER_TEXTURE2D_BIAS(_BaseColorMap1, sampler_BaseColorMap0, layerTexCoord.base1, textureBias).rgb *_BaseColor1.rgb;
    float3 baseMeanColor2 = SAMPLE_LAYER_TEXTURE2D_BIAS(_BaseColorMap2, sampler_BaseColorMap0, layerTexCoord.base2, textureBias).rgb *_BaseColor2.rgb;
    float3 baseMeanColor3 = SAMPLE_LAYER_TEXTURE2D_BIAS(_BaseColorMap3, sampler_BaseColorMap0, layerTexCoord.base3, textureBias).rgb *_BaseColor3.rgb;

    float3 meanColor = BlendLayeredVector3(baseMeanColor0, baseMeanColor1, baseMeanColor2, baseMeanColor3, weights);

    // If we inherit from base layer, we will add a bit of it
    // We add variance of current visible level and the base color 0 or mean (to retrieve initial color) depends on influence
    // (baseColor - meanColor) + lerp(meanColor, baseColor0, inheritBaseColor) simplify to
    return saturate(influenceFactor * (baseColor0 - meanColor) + baseColor);
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

    // Note: If per pixel displacement is enabled it mean we will fetch again the various heightmaps at the intersection location. Not sure the compiler can optimize.
    float weights[_MAX_LAYER];
    ComputeLayerWeights(input, layerTexCoord, float4(alpha0, alpha1, alpha2, alpha3), weights);

    // For layered shader, alpha of base color is used as either an opacity mask, a composition mask for inheritance parameters or a density mask.
    float alpha = PROP_BLEND_SCALAR(alpha, weights);

#ifdef _ALPHATEST_ON
    clip(alpha - _AlphaCutoff);
#endif

#if defined(_MAIN_LAYER_INFLUENCE_MODE)
    surfaceData.baseColor = ComputeMainBaseColorInfluence(surfaceData0.baseColor, surfaceData1.baseColor, surfaceData2.baseColor, surfaceData3.baseColor, alpha, layerTexCoord, weights);
    float3 normalTS = ComputeMainNormalInfluence(input, normalTS0, normalTS1, normalTS2, normalTS3, layerTexCoord, weights);
#else
    surfaceData.baseColor = SURFACEDATA_BLEND_VECTOR3(surfaceData, baseColor, weights);
    float3 normalTS = BlendLayeredVector3(normalTS0, normalTS1, normalTS2, normalTS3, weights);
#endif

    surfaceData.perceptualSmoothness = SURFACEDATA_BLEND_SCALAR(surfaceData, perceptualSmoothness, weights);
    surfaceData.ambientOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, ambientOcclusion, weights);
    surfaceData.metallic = SURFACEDATA_BLEND_SCALAR(surfaceData, metallic, weights);

    // Init other unused parameter
    surfaceData.tangentWS = normalize(input.tangentToWorld[0].xyz);
    surfaceData.materialId = 0;
    surfaceData.anisotropy = 0;
    surfaceData.specular = 0.04;
    surfaceData.subsurfaceRadius = 1.0;
    surfaceData.thickness = 0.0;
    surfaceData.subsurfaceProfile = 0;
    surfaceData.coatNormalWS = float3(1.0, 0.0, 0.0);
    surfaceData.coatPerceptualSmoothness = 1.0;
    surfaceData.specularColor = float3(0.0, 0.0, 0.0);

    GetNormalAndTangentWS(input, V, normalTS, surfaceData.normalWS, surfaceData.tangentWS);
    // Done one time for all layered - cumulate with spec occ alpha for now
    surfaceData.specularOcclusion = SURFACEDATA_BLEND_SCALAR(surfaceData, specularOcclusion, weights);
    surfaceData.specularOcclusion *= GetHorizonOcclusion(V, surfaceData.normalWS, input.tangentToWorld[2].xyz, _HorizonFade);

    GetBuiltinData(input, surfaceData, alpha, depthOffset, builtinData);
}

#endif // #ifndef LAYERED_LIT_SHADER

#ifdef TESSELLATION_ON
#include "LitTessellation.hlsl" // Must be after GetLayerTexCoord() declaration
#endif
