#ifdef _TERRAIN_8_LAYERS
    #define _LAYER_COUNT 8
#else
    #define _LAYER_COUNT 4
#endif

#ifndef _TERRAIN_BLEND_HEIGHT
    #define _TERRAIN_BLEND_DENSITY // enable density blending by default and use DiffuseRemap.w to control whether the density blending is enabled for a layer
#endif

#define DECLARE_TERRAIN_LAYER_PROPS(n)  \
    float4 _Splat##n##_ST;              \
    float _Metallic##n;                 \
    float _Smoothness##n;               \
    float _NormalScale##n;              \
    float4 _DiffuseRemapScale##n;       \
    float4 _MaskMapRemapOffset##n;      \
    float4 _MaskMapRemapScale##n;       \
    float _LayerHasMask##n;

#define DECLARE_TERRAIN_LAYER_PROPS_FIRST_4 \
    DECLARE_TERRAIN_LAYER_PROPS(0)          \
    DECLARE_TERRAIN_LAYER_PROPS(1)          \
    DECLARE_TERRAIN_LAYER_PROPS(2)          \
    DECLARE_TERRAIN_LAYER_PROPS(3)          \
    float4 _Control0_TexelSize;             \


#ifdef _TERRAIN_8_LAYERS
#define UNITY_TERRAIN_CB_VARS \
        DECLARE_TERRAIN_LAYER_PROPS_FIRST_4 \
        DECLARE_TERRAIN_LAYER_PROPS(4)      \
        DECLARE_TERRAIN_LAYER_PROPS(5)      \
        DECLARE_TERRAIN_LAYER_PROPS(6)      \
        DECLARE_TERRAIN_LAYER_PROPS(7)      \
        float4 _Control1_TexelSize;         \
        float _HeightTransition;
#else
#define UNITY_TERRAIN_CB_VARS \
        DECLARE_TERRAIN_LAYER_PROPS_FIRST_4 \
        float _HeightTransition;
#endif

#ifdef TERRAIN_SPLAT_BASEPASS
#define UNITY_TERRAIN_CB_DEBUG_VARS \
    float4 _MainTex_TexelSize;      \
    float4 _MainTex_MipInfo;
#else
#define UNITY_TERRAIN_CB_DEBUG_VARS \
    float4 _Control0_MipInfo;       \
    float4 _Splat0_TexelSize;       \
    float4 _Splat0_MipInfo;         \
    float4 _Splat1_TexelSize;       \
    float4 _Splat1_MipInfo;         \
    float4 _Splat2_TexelSize;       \
    float4 _Splat2_MipInfo;         \
    float4 _Splat3_TexelSize;       \
    float4 _Splat3_MipInfo;         \
    float4 _Splat4_TexelSize;       \
    float4 _Splat4_MipInfo;         \
    float4 _Splat5_TexelSize;       \
    float4 _Splat5_MipInfo;         \
    float4 _Splat6_TexelSize;       \
    float4 _Splat6_MipInfo;         \
    float4 _Splat7_TexelSize;       \
    float4 _Splat7_MipInfo;
#endif

CBUFFER_START(UnityTerrain)
UNITY_TERRAIN_CB_VARS
#ifdef _TERRAIN_BASEMAP_GEN
float4 _Control0_ST;
#else
#ifdef UNITY_INSTANCING_ENABLED
float4 _TerrainHeightmapRecipSize;  // float4(1.0f/width, 1.0f/height, 1.0f/(width-1), 1.0f/(height-1))
float4 _TerrainHeightmapScale;      // float4(hmScale.x, hmScale.y / (float)(kMaxHeight), hmScale.z, 0.0f)
#endif
#ifdef DEBUG_DISPLAY
UNITY_TERRAIN_CB_DEBUG_VARS
#endif
// ShaderGraph already defines these
#if defined(SCENESELECTIONPASS) && !defined(TERRAIN_ENABLED)
    int _ObjectId;
    int _PassValue;
#endif
#endif
CBUFFER_END

// Splat texture declarations
TEXTURE2D(_Control0);
SAMPLER(sampler_Control0);
//#ifndef _TERRAIN_BASEMAP_GEN
//float4 _Control0_ST;
//#endif

#ifdef _ALPHATEST_ON
TEXTURE2D(_TerrainHolesTexture);
SAMPLER(sampler_TerrainHolesTexture);
#endif

#ifndef TERRAIN_SPLAT_BASEPASS
// Include splat properties for all non-basepass terrain shaders
#define DECLARE_TERRAIN_LAYER_TEXS(n)   \
    TEXTURE2D(_Splat##n);               \
    TEXTURE2D(_Normal##n);              \
    TEXTURE2D(_Mask##n)

DECLARE_TERRAIN_LAYER_TEXS(0);
DECLARE_TERRAIN_LAYER_TEXS(1);
DECLARE_TERRAIN_LAYER_TEXS(2);
DECLARE_TERRAIN_LAYER_TEXS(3);
#ifdef _TERRAIN_8_LAYERS
DECLARE_TERRAIN_LAYER_TEXS(4);
DECLARE_TERRAIN_LAYER_TEXS(5);
DECLARE_TERRAIN_LAYER_TEXS(6);
DECLARE_TERRAIN_LAYER_TEXS(7);
TEXTURE2D(_Control1);
#endif

#undef DECLARE_TERRAIN_LAYER_TEXS

SAMPLER(sampler_Splat0);

#ifdef OVERRIDE_SPLAT_SAMPLER_NAME
#define sampler_Splat0 OVERRIDE_SPLAT_SAMPLER_NAME
SAMPLER(OVERRIDE_SPLAT_SAMPLER_NAME);
#endif
#endif

// Defines for sampling splats
#if !defined(TERRAIN_SPLAT_BASEPASS) && !defined(SHADERGRAPH_PREVIEW)
float4 albedo[_LAYER_COUNT];
float3 normal[_LAYER_COUNT];
float4 masks[_LAYER_COUNT];


#define SampleResults(i, mask)                                                                                  \
    UNITY_BRANCH if (mask > 0)                                                                                  \
    {                                                                                                           \
        float2 splatuv = splatBaseUV * _Splat##i##_ST.xy + _Splat##i##_ST.zw;                                   \
        float2 splatdxuv = dxuv * _Splat##i##_ST.x;                                                             \
        float2 splatdyuv = dyuv * _Splat##i##_ST.y;                                                             \
        albedo[i] = SAMPLE_TEXTURE2D_GRAD(_Splat##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv);            \
        albedo[i].rgb *= _DiffuseRemapScale##i.xyz;                                                             \
        normal[i] = SampleNormal(i);                                                                            \
        masks[i] = SampleMasks(i, mask);                                                                        \
    }                                                                                                           \
    else                                                                                                        \
    {                                                                                                           \
        albedo[i] = float4(0, 0, 0, 0);                                                                         \
        normal[i] = float3(0, 0, 0);                                                                            \
        masks[i] = NullMask(i);                                                                                 \
    }

#ifdef _NORMALMAP
#define SampleNormal(i) SampleNormalGrad(_Normal##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv, _NormalScale##i)
#else
#define SampleNormal(i) float3(0, 0, 0)
#endif

#define DefaultMask(i) float4(_Metallic##i, _MaskMapRemapOffset##i.y + _MaskMapRemapScale##i.y, _MaskMapRemapOffset##i.z + 0.5 * _MaskMapRemapScale##i.z, albedo[i].a * _Smoothness##i)

#ifdef _MASKMAP
#define MaskModeMasks(i, blendMask) RemapMasks(SAMPLE_TEXTURE2D_GRAD(_Mask##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv), blendMask, _MaskMapRemapOffset##i, _MaskMapRemapScale##i)
#define SampleMasks(i, blendMask) lerp(DefaultMask(i), MaskModeMasks(i, blendMask), _LayerHasMask##i)
#define NullMask(i)               float4(0, 1, _MaskMapRemapOffset##i.z, 0) // only height matters when weight is zero.
#else
#define SampleMasks(i, blendMask) DefaultMask(i)
#define NullMask(i)               float4(0, 1, 0, 0)
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

// This is really silly but I don't know how to else to get around SampleResults treating a variable name as a string for the sake of accessing the splat properties.
//#define ToConstInt(i) ((i == 0) ? 0 : ((i == 1) ? 1 : ((i == 2) ? 2 : ((i == 3) ? 3 : ((i == 4) ? 4 : ((i == 5) ? 5 : ((i == 6) ? 6 : 7)))))));

#ifdef _TERRAIN_8_LAYERS
#define SampleControl(i) i > 4 ? SAMPLE_TEXTURE2D(_Control1, sampler_Control0, ((uv.xy * (_Control1_TexelSize.zw - 1.0f) + 0.5f) * _Control1_TexelSize.xy)) : SAMPLE_TEXTURE2D(_Control0, sampler_Control0, ((uv.xy * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy));
#else
#define SampleControl(i) SAMPLE_TEXTURE2D(_Control0, sampler_Control0, ((uv.xy * (_Control0_TexelSize.zw - 1.0f) + 0.5f) * _Control0_TexelSize.xy));
#endif

float3 SampleNormalGrad(TEXTURE2D_PARAM(textureName, samplerName), float2 uv, float2 dxuv, float2 dyuv, float scale)
{
    float4 nrm = SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uv, dxuv, dyuv);
#ifdef SURFACE_GRADIENT
#ifdef UNITY_NO_DXT5nm
    return float3(UnpackDerivativeNormalRGB(nrm, scale), 0);
#else
    return float3(UnpackDerivativeNormalRGorAG(nrm, scale), 0);
#endif
#else
#ifdef UNITY_NO_DXT5nm
    return UnpackNormalRGB(nrm, scale);
#else
    return UnpackNormalmapRGorAG(nrm, scale);
#endif
#endif
}

float GetSumHeight(float4 heights0, float4 heights1)
{
    float sumHeight = heights0.x;
    sumHeight += heights0.y;
    sumHeight += heights0.z;
    sumHeight += heights0.w;
#ifdef _TERRAIN_8_LAYERS
    sumHeight += heights1.x;
    sumHeight += heights1.y;
    sumHeight += heights1.z;
    sumHeight += heights1.w;
#endif
    return sumHeight;
}

float4 RemapMasks(float4 masks, float blendMask, float4 remapOffset, float4 remapScale)
{
    float4 ret = masks;
    ret.b *= blendMask; // height needs to be weighted before remapping
    ret = ret * remapScale + remapOffset;
    return ret;
}


void GetSplatData(float2 uv, float layer, bool doBlend, out float3 outAlbedo, out float3 outNormal, out float outMetallic, out float outSmoothness, out float outOcclusion, out float outAlpha, out float4 outControl)
{
    layer = layer % _LAYER_COUNT; // Prevent out of bounds access
    float2 splatBaseUV = uv;
    float2 dxuv = ddx(splatBaseUV);
    float2 dyuv = ddy(splatBaseUV);

    outControl = SampleControl(layer);
#ifdef _ALPHATEST_ON
    outAlpha = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, uv).r == 0.0f ? 0.0f : 1.0f;
#else
    outAlpha = 1.0f;
#endif

    if (doBlend)
    {
        float4 blendMasks0 = layer < 4 ? outControl : SampleControl(0);
#ifdef _TERRAIN_8_LAYERS
        float4 blendMasks1 = layer > 3 ? outControl : SampleControl(4);
#else
        float4 blendMasks1 = float4(0, 0, 0, 0);
#endif
        SampleResults(0, blendMasks0.x);
        SampleResults(1, blendMasks0.y);
        SampleResults(2, blendMasks0.z);
        SampleResults(3, blendMasks0.w);
#ifdef _TERRAIN_8_LAYERS
        SampleResults(4, blendMasks1.x);
        SampleResults(5, blendMasks1.y);
        SampleResults(6, blendMasks1.z);
        SampleResults(7, blendMasks1.w);
#endif

        float weights[_LAYER_COUNT];
        ZERO_INITIALIZE_ARRAY(float, weights, _LAYER_COUNT);
#ifdef _MASKMAP
#if defined(_TERRAIN_BLEND_HEIGHT)
        // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
        float maxHeight = masks[0].z;
        maxHeight = max(maxHeight, masks[1].z);
        maxHeight = max(maxHeight, masks[2].z);
        maxHeight = max(maxHeight, masks[3].z);
#ifdef _TERRAIN_8_LAYERS
        maxHeight = max(maxHeight, masks[4].z);
        maxHeight = max(maxHeight, masks[5].z);
        maxHeight = max(maxHeight, masks[6].z);
        maxHeight = max(maxHeight, masks[7].z);
#endif

        // Make sure that transition is not zero otherwise the next computation will be wrong.
        // The epsilon here also has to be bigger than the epsilon in the next computation.
        float transition = max(_HeightTransition, 1e-5);

        // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
        // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
        float4 weightedHeights0 = { masks[0].z, masks[1].z, masks[2].z, masks[3].z };
        weightedHeights0 = weightedHeights0 - maxHeight.xxxx;
        // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
        weightedHeights0 = (max(0, weightedHeights0 + transition) + 1e-6) * blendMasks0;

#ifdef _TERRAIN_8_LAYERS
        float4 weightedHeights1 = { masks[4].z, masks[5].z, masks[6].z, masks[7].z };
        weightedHeights1 = weightedHeights1 - maxHeight.xxxx;
        weightedHeights1 = (max(0, weightedHeights1 + transition) + 1e-6) * blendMasks1;
#else
        float4 weightedHeights1 = { 0, 0, 0, 0 };
#endif

        // Normalize
        float sumHeight = GetSumHeight(weightedHeights0, weightedHeights1);
        blendMasks0 = weightedHeights0 / sumHeight.xxxx;
#ifdef _TERRAIN_8_LAYERS
        blendMasks1 = weightedHeights1 / sumHeight.xxxx;
#endif
#elif defined(_TERRAIN_BLEND_DENSITY)
        // Denser layers are more visible.
        float4 opacityAsDensity0 = saturate((float4(albedo[0].a, albedo[1].a, albedo[2].a, albedo[3].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks0)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
        opacityAsDensity0 += 0.001f * blendMasks0;      // if all weights are zero, default to what the blend mask says
        float4 useOpacityAsDensityParam0 = { _DiffuseRemapScale0.w, _DiffuseRemapScale1.w, _DiffuseRemapScale2.w, _DiffuseRemapScale3.w }; // 1 is off
        blendMasks0 = lerp(opacityAsDensity0, blendMasks0, useOpacityAsDensityParam0);
#ifdef _TERRAIN_8_LAYERS
        float4 opacityAsDensity1 = saturate((float4(albedo[4].a, albedo[5].a, albedo[6].a, albedo[7].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks1)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
        opacityAsDensity1 += 0.001f * blendMasks1;  // if all weights are zero, default to what the blend mask says
        float4 useOpacityAsDensityParam1 = { _DiffuseRemapScale4.w, _DiffuseRemapScale5.w, _DiffuseRemapScale6.w, _DiffuseRemapScale7.w };
        blendMasks1 = lerp(opacityAsDensity1, blendMasks1, useOpacityAsDensityParam1);
#endif

        // Normalize
        float sumHeight = GetSumHeight(blendMasks0, blendMasks1);
        blendMasks0 = blendMasks0 / sumHeight.xxxx;
#ifdef _TERRAIN_8_LAYERS
        blendMasks1 = blendMasks1 / sumHeight.xxxx;
#endif
#endif // if _TERRAIN_BLEND_HEIGHT
#endif // if _MASKMAP

        weights[0] = blendMasks0.x;
        weights[1] = blendMasks0.y;
        weights[2] = blendMasks0.z;
        weights[3] = blendMasks0.w;
#ifdef _TERRAIN_8_LAYERS
        weights[4] = blendMasks1.x;
        weights[5] = blendMasks1.y;
        weights[6] = blendMasks1.z;
        weights[7] = blendMasks1.w;
#endif

        outAlbedo = 0;
        outNormal = 0;
        outMetallic = 0;
        outSmoothness = 0;
        outOcclusion = 0;
        float3 outMasks = 0;
        UNITY_UNROLL for (int i = 0; i < _LAYER_COUNT; ++i)
        {
            outAlbedo += albedo[i].rgb * weights[i];
            outNormal += normal[i].rgb * weights[i];
            outMasks += masks[i].xyw * weights[i];
        }
        outSmoothness = outMasks.z;
        outMetallic = outMasks.x;
        outOcclusion = outMasks.y;
    }
    else
    {
        // This is really silly but I don't know how to else to get around SampleResults treating a variable name as a string for the sake of accessing the splat properties.
        if (layer == 0)
            SampleResults(0, outControl[0]);
        if (layer == 1)
            SampleResults(1, outControl[1]);
        if (layer == 2)
            SampleResults(2, outControl[2]);
        if (layer == 3)
            SampleResults(3, outControl[3]);
#ifdef _TERRAIN_8_LAYERS
        if (layer == 4)
            SampleResults(4, outControl[0]);
        if (layer == 5)
            SampleResults(5, outControl[1]);
        if (layer == 6)
            SampleResults(6, outControl[2]);
        if (layer == 7)
            SampleResults(7, outControl[3]);
#endif
        //SampleResults(layer, outControl[layer % 4]);
        outAlbedo = albedo[layer].xyz;
        outNormal = normal[layer].xyz;
        outMetallic = masks[layer].x;
        outSmoothness = masks[layer].w;
        outOcclusion = masks[layer].y;
    }
}
#endif
