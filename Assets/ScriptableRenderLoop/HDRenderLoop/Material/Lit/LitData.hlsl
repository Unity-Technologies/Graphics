
//-------------------------------------------------------------------------------------
// Fill SurfaceData/Builtin data function
//-------------------------------------------------------------------------------------

// In unity we can have a mix of fully baked lightmap (static lightmap) + enlighten realtime lightmap (dynamic lightmap)
// for each case we can have directional lightmap or not.
// Else we have lightprobe for dynamic/moving entity. Either SH9 per object lightprobe or SH4 per pixel per object volume probe
float3 SampleBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap)
{
    // If there is no lightmap, it assume lightprobe
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)

// TODO: Confirm with Ionut but it seems that UNITY_LIGHT_PROBE_PROXY_VOLUME is always define for high end and 
// unity_ProbeVolumeParams always bind.
    if (unity_ProbeVolumeParams.x == 0.0)
    {
        // TODO: pass a tab of coefficient instead!
        float4 SHCoefficients[7];
        SHCoefficients[0] = unity_SHAr;
        SHCoefficients[1] = unity_SHAg;
        SHCoefficients[2] = unity_SHAb;
        SHCoefficients[3] = unity_SHBr;
        SHCoefficients[4] = unity_SHBg;
        SHCoefficients[5] = unity_SHBb;
        SHCoefficients[6] = unity_SHC;

        return SampleSH9(SHCoefficients, normalWS);
    }
    else
    {
        // TODO: Move all this to C++!
        float4x4 identity = 0;
        identity._m00_m11_m22_m33 = 1.0;
        float4x4 WorldToTexture = (unity_ProbeVolumeParams.y == 1.0f) ? unity_ProbeVolumeWorldToObject : identity;

        float4x4 translation = identity;
        translation._m30_m31_m32 = -unity_ProbeVolumeMin.xyz;

        float4x4 scale = 0;
        scale._m00_m11_m22_m33 = float4(unity_ProbeVolumeSizeInv.xyz, 1.0);

        WorldToTexture = mul(mul(scale, translation), WorldToTexture);
    
        return SampleProbeVolumeSH4(TEXTURE3D_PARAM(unity_ProbeVolumeSH, samplerunity_ProbeVolumeSH), positionWS, normalWS, WorldToTexture, unity_ProbeVolumeParams.z);
    }

#else

    float3 bakeDiffuseLighting = float3(0.0, 0.0, 0.0);

    #ifdef LIGHTMAP_ON
        #ifdef DIRLIGHTMAP_COMBINED
        bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_Lightmap, samplerunity_Lightmap),
                                                        TEXTURE2D_PARAM(unity_LightmapInd, samplerunity_Lightmap),
                                                        uvStaticLightmap, unity_LightmapST, normalWS);
        #else
        bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_PARAM(unity_Lightmap, samplerunity_Lightmap), uvStaticLightmap, unity_LightmapST);
        #endif
    #endif

    #ifdef DYNAMICLIGHTMAP_ON
        #ifdef DIRLIGHTMAP_COMBINED
        bakeDiffuseLighting += SampleDirectionalLightmap(TEXTURE2D_PARAM(unity_DynamicLightmap, samplerunity_DynamicLightmap),
                                                        TEXTURE2D_PARAM(unity_DynamicDirectionality, samplerunity_DynamicLightmap),
                                                        uvDynamicLightmap, unity_DynamicLightmapST, normalWS);
        #else
        bakeDiffuseLighting += SampleSingleLightmap(TEXTURE2D_PARAM(unity_DynamicLightmap, samplerunity_DynamicLightmap), uvDynamicLightmap, unity_DynamicLightmapST);
        #endif
    #endif

    return bakeDiffuseLighting;

#endif
}

float2 CalculateVelocity(float4 positionCS, float4 previousPositionCS)
{
    // This test on define is required to remove warning of divide by 0 when initializing empty struct
    // TODO: Add forward opaque MRT case...
#if (SHADERPASS == SHADERPASS_VELOCITY) || (SHADERPASS == SHADERPASS_GBUFFER && SHADEROPTIONS_VELOCITY_IN_GBUFFER)
    // Encode velocity
    positionCS.xy = positionCS.xy / positionCS.w;
    previousPositionCS.xy = previousPositionCS.xy / previousPositionCS.w;

    return (positionCS.xy - previousPositionCS.xy) * _ForceNoMotion;
#else
    return float2(0.0, 0.0);
#endif
}
                                
float3 LerpWhiteTo(float3 b, float t)
{
    float oneMinusT = 1.0 - t;
    return float3(oneMinusT, oneMinusT, oneMinusT) + b * t;
}

void GetBuiltinData(FragInput input, SurfaceData surfaceData, float alpha, out BuiltinData builtinData)
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
#elif defined(_MASKMAP) // If we have a MaskMap, use emissive slot as a mask on baseColor
    builtinData.emissiveColor = surfaceData.baseColor * (SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, input.texCoord0).b * builtinData.emissiveIntensity).xxx;
#else
    builtinData.emissiveColor = float3(0.0, 0.0, 0.0);
#endif

    builtinData.velocity = CalculateVelocity(input.positionCS, input.previousPositionCS);

    builtinData.distortion = float2(0.0, 0.0);
    builtinData.distortionBlur = 0.0;
}

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
    // Regular texcoord
    LayerUV base0;
    LayerUV base1;
    LayerUV base2;
    LayerUV base3;

    LayerUV details0;
    LayerUV details1;
    LayerUV details2;
    LayerUV details3;

    float2 uvStaticLightmap;
    float2 uvDynamicLightmap;

    // triplanar weight
    float3 weights;
};

// Transforms 2D UV by scale/bias property
#define TRANSFORM_TEX(tex,name) ((tex.xy) * name##_ST.xy + name##_ST.zw)

float4 SampleLayer(TEXTURE2D_ARGS(layerTex, layerSampler), LayerUV layerUV, float3 weights)
{
    if (layerUV.isTriplanar)
    {
        float4 val = float4(0.0, 0.0, 0.0, 0.0);

        if (weights.x > 0.0)
        {
            val += outCoord.blendWeights.x * SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvYZ);
        }
        if (weights.y > 0.0)
        {
            val += outCoord.blendWeights.y * SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvZX);
        }
        if (weights.z > 0.0)
        {
            val += outCoord.blendWeights.z * SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uvXY);
        }

        return val;
    }
    else
    {
        return SAMPLE_TEXTURE2D(layerTex, layerSampler, layerUV.uv);
    }
}

#ifndef LAYERED_LIT_SHADER

#define SAMPLE_LAYER_TEXTURE2D(textureName, samplerName, coord) SampleLayer(TEXTURE2D_PASS(textureName, samplerName), coord, coord.weights);

#define SAMPLE_LAYER_DETAIL_TEXTURE2D
#define ADD_IDX(Name) Name
#include "LitSurfaceData.hlsl"

void GetSurfaceAndBuiltinData(FragInput input, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    bool isTriplanar = false;
    LayerTexCoord layerTexCoord[_MAX_LAYER];


    // one weight for each direction XYZ - Use vertex normal for triplanar
    float3 triplanarBlendWeights = ComputeTriplanarWeights(input.tangentToWorld[2].xyz); // Will be remove if triplanar not used

    // For each layer
#if defined(_LAYER_MAPPING_TRIPLANAR_0)
    isTriplanar = true;
#else
    isTriplanar = false;
#endif
    ComputeLayerTexCoord0(input, isTriplanar, texCoord.layerTexCoord[0]);
    ApplyDisplacement0(input, texCoord.layerTexCoord[0]);
    GetSurfaceData0(input, texCoord.layerTexCoord[0]);

#if define _LAYEREDLIT_2_LAYERS
        
            

        ComputeLayerTexCoord

    float2 offset = GetHeigthData(input, surfaceData);
    input.texCoord0 += offset;
    input.texCoord1 += offset;
    float alpha = GetSurfaceData(input, surfaceData);
    GetBuiltinData(input, surfaceData, alpha, builtinData);
}

#else

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


#define SAMPLE_LAYER_TEXTURE2D(textureName, samplerName, coord2) SampleLayer(TEXTURE2D_ARGS(textureName##0, samplerName##0), LayerCoordinates layerCoord, 0)
#include "LitSurfaceData.hlsl"

void GetSurfaceAndBuiltinData(FragInput input, out SurfaceData surfaceData, out BuiltinData builtinData)
{
    LayerCoordinates layerCoord;
    ComputeLayerCoordinates(layerCoord, input);
    GetHeigthData0();


    // Mask Values : Layer 1, 2, 3 are r, g, b
    float3 maskValues = float3(1.0, 1.0, 1.0);

#if defined(_LAYER_MASK_MAP) || defined(_LAYER_MASK_MAP_VERTEX_COLOR)
    maskValues *= SAMPLE_TEXTURE2D(_LayerMaskMap, sampler_LayerMaskMap, input.texCoord0).rgb;
#endif
#if defined(_LAYER_MASK_VERTEX_COLOR) || defined(_LAYER_MASK_MAP_VERTEX_COLOR)
    maskValues *= input.vertexColor.rgb;
#endif

    float weights[_MAX_LAYER];
    ComputeMaskWeights(maskValues, weights);

    SurfaceData surfaceData0;
    SurfaceData surfaceData1;
    SurfaceData surfaceData2;
    SurfaceData surfaceData3;

    float alpha = GetSurfaceData0(input, surfaceData0);
    GetBuiltinData(input, surfaceData, alpha, builtinData);
}


#if !defined(LAYERED_LIT_SHADER)

float3 BlendLayeredColor(float3 rgb0, float3 rgb1, float3 rgb2, float3 rgb3, float weight[4])
{
    float3 result = float3(0.0, 0.0, 0.0);

    result = rgb0 * weight[0] + rgb1 * weight[1];
#if _LAYER_COUNT >= 3
    result += (rgb2 * weight[2]);
#endif
#if _LAYER_COUNT >= 4
    result += rgb3 * weight[3];
#endif

    return result;
}

float3 BlendLayeredNormal(float3 normal0, float3 normal1, float3 normal2, float3 normal3, float weight[4])
{
    float3 result = float3(0.0, 0.0, 0.0);

        // TODO : real normal map blending function
        result = normal0 * weight[0] + normal1 * weight[1];
#if _LAYER_COUNT >= 3
    result += normal2 * weight[2];
#endif
#if _LAYER_COUNT >= 4
    result += normal3 * weight[3];
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

#endif
