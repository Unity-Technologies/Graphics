
#ifdef _TERRAIN_8_LAYERS
    #define _LAYER_COUNT 8
#else
    #define _LAYER_COUNT 4
#endif

#define DECLARE_TERRAIN_LAYER(n)    \
    TEXTURE2D(_Splat##n);           \
    TEXTURE2D(_Normal##n);          \
    TEXTURE2D(_Mask##n);            \
    float4 _Splat##n##_ST;          \
    float _Metallic##n;             \
    float _Smoothness##n;           \
    float _Density##n;              \
    float _HeightCenter##n;         \
    float _HeightAmplitude##n

DECLARE_TERRAIN_LAYER(0);
DECLARE_TERRAIN_LAYER(1);
DECLARE_TERRAIN_LAYER(2);
DECLARE_TERRAIN_LAYER(3);

TEXTURE2D(_Control0);
SAMPLER(sampler_Splat0);
SAMPLER(sampler_Control0);

#ifdef _TERRAIN_8_LAYERS
    DECLARE_TERRAIN_LAYER(4);
    DECLARE_TERRAIN_LAYER(5);
    DECLARE_TERRAIN_LAYER(6);
    DECLARE_TERRAIN_LAYER(7);
    TEXTURE2D(_Control1);
#endif

#undef DECLARE_TERRAIN_LAYER

float _HeightTransition;

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

float3 SampleNormalGrad(TEXTURE2D_ARGS(textureName, samplerName), float2 uv, float2 dxuv, float2 dyuv, float3 tangentWS, float3 bitangentWS)
{
    float4 nrm = SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, uv, dxuv, dyuv);
#ifdef SURFACE_GRADIENT
    #ifdef UNITY_NO_DXT5nm
        real2 deriv = UnpackDerivativeNormalRGB(nrm, 1);
    #else
        real2 deriv = UnpackDerivativeNormalRGorAG(nrm, 1);
    #endif
    return SurfaceGradientFromTBN(deriv, tangentWS, bitangentWS);
#else
    #ifdef UNITY_NO_DXT5nm
        return UnpackNormalRGB(nrm, 1);
    #else
        return UnpackNormalmapRGorAG(nrm, 1);
    #endif
#endif
}

float4 RemapMasks(float4 masks, float blendMask, float heightCenter, float heightAmplitude, float smoothness, float metallic)
{
    return float4(
        (masks.r * blendMask - heightCenter) * heightAmplitude,
        masks.g * smoothness,
        masks.b * metallic,
        0);
}

#ifdef OVERRIDE_SAMPLER_NAME
    #define sampler_Splat0 OVERRIDE_SAMPLER_NAME
#endif

void TerrainSplatBlend(float2 uv, float3 tangentWS, float3 bitangentWS,
    out float3 outAlbedo, out float3 outNormalTS, out float outSmoothness, out float outMetallic)
{
    // TODO: triplanar and SURFACE_GRADIENT?
    // TODO: POM

    float4 albedo[_LAYER_COUNT];
    float3 normal[_LAYER_COUNT];
    float4 masks[_LAYER_COUNT];

#ifdef _NORMALMAP
    #define SampleNormal(i) SampleNormalGrad(_Normal##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv, tangentWS, bitangentWS)
#else
    #define SampleNormal(i) float3(0, 0, 1)
#endif

#ifdef _MASKMAP
    #define SampleMasks(i, blendMask) RemapMasks(SAMPLE_TEXTURE2D_GRAD(_Mask##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv), blendMask, _HeightCenter##i, _HeightAmplitude##i, _Smoothness##i, _Metallic##i)
#else
    #define SampleMasks(i, blendMask) float4(0, albedo[i].a * _Smoothness##i, _Metallic##i, 0)
#endif

#define SampleResults(i, mask)                                                                          \
    UNITY_BRANCH if (mask > 0)                                                                          \
    {                                                                                                   \
        float2 splatuv = uv * _Splat##i##_ST.xy + _Splat##i##_ST.zw;                                    \
        float2 splatdxuv = dxuv * _Splat##i##_ST.x;                                                     \
        float2 splatdyuv = dyuv * _Splat##i##_ST.y;                                                     \
        albedo[i] = SAMPLE_TEXTURE2D_GRAD(_Splat##i, sampler_Splat0, splatuv, splatdxuv, splatdyuv);    \
        normal[i] = SampleNormal(i);                                                                    \
        masks[i] = SampleMasks(i, mask);                                                                \
    }                                                                                                   \
    else                                                                                                \
    {                                                                                                   \
        albedo[i] = float4(0, 0, 0, 0);                                                                 \
        normal[i] = float3(0, 0, 0);                                                                    \
        masks[i] = float4(-1, 0, 0, 0);                            \
    }

    float2 dxuv = ddx(uv);
    float2 dyuv = ddy(uv);

    float4 blendMasks0 = SAMPLE_TEXTURE2D(_Control0, sampler_Control0, uv);
    #ifdef _TERRAIN_8_LAYERS
        float4 blendMasks1 = SAMPLE_TEXTURE2D(_Control1, sampler_Control0, uv);
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

#undef SampleNormal
#undef SampleMasks
#undef SampleResults

    float weights[_LAYER_COUNT];
    ZERO_INITIALIZE_ARRAY(float, weights, _LAYER_COUNT);

    #if defined(_TERRAIN_BLEND_HEIGHT) && defined(_MASKMAP)
        // Modify blendMask to take into account the height of the layer. Higher height should be more visible.
        float maxHeight = masks[0].x;
        maxHeight = max(maxHeight, masks[1].x);
        maxHeight = max(maxHeight, masks[2].x);
        maxHeight = max(maxHeight, masks[3].x);
        #ifdef _TERRAIN_8_LAYERS
            maxHeight = max(maxHeight, masks[4].x);
            maxHeight = max(maxHeight, masks[5].x);
            maxHeight = max(maxHeight, masks[6].x);
            maxHeight = max(maxHeight, masks[7].x);
        #endif

        // Make sure that transition is not zero otherwise the next computation will be wrong.
        // The epsilon here also has to be bigger than the epsilon in the next computation.
        float transition = max(_HeightTransition, 1e-5);

        // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
        // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
        float4 weightedHeights0 = { masks[0].x, masks[1].x, masks[2].x, masks[3].x };
        weightedHeights0 = weightedHeights0 - maxHeight.xxxx;
        // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
        weightedHeights0 = (max(0, weightedHeights0 + transition) + 1e-6) * blendMasks0;

        #ifdef _TERRAIN_8_LAYERS
            float4 weightedHeights1 = { masks[4].x, masks[5].x, masks[6].x, masks[7].x };
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

    #elif defined(_TERRAIN_BLEND_DENSITY) && defined(_MASKMAP)
        // Denser layers are more visible.
        float4 opacityAsDensity0 = saturate((float4(albedo[0].a, albedo[1].a, albedo[2].a, albedo[3].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks0)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
        float4 useOpacityAsDensityParam0 = { _Density0, _Density1, _Density2, _Density3 };
        blendMasks0 = lerp(blendMasks0, opacityAsDensity0, useOpacityAsDensityParam0);
        #ifdef _TERRAIN_8_LAYERS
            float4 opacityAsDensity1 = saturate((float4(albedo[4].a, albedo[5].a, albedo[6].a, albedo[7].a) - (float4(1.0, 1.0, 1.0, 1.0) - blendMasks1)) * 20.0); // 20.0 is the number of steps in inputAlphaMask (Density mask. We decided 20 empirically)
            float4 useOpacityAsDensityParam1 = { _Density4, _Density5, _Density6, _Density7 };
            blendMasks1 = lerp(blendMasks1, opacityAsDensity1, useOpacityAsDensityParam1);
        #endif
    #endif

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

    #if defined(_TERRAIN_BLEND_DENSITY) && defined(_MASKMAP)
        // calculate weight of each layers
        // Algorithm is like this:
        // Top layer have priority on others layers
        // If a top layer doesn't use the full weight, the remaining can be use by the following layer.
        float weightsSum = 0.0;

        UNITY_UNROLL
        for (int i = _LAYER_COUNT - 1; i >= 0; --i)
        {
            weights[i] = min(weights[i], (1.0 - weightsSum));
            weightsSum = saturate(weightsSum + weights[i]);
        }
    #endif

    outAlbedo = 0;
    outNormalTS = 0;
    float2 outMasks = 0;
    UNITY_UNROLL for (int i = 0; i < _LAYER_COUNT; ++i)
    {
        outAlbedo += albedo[i].rgb * weights[i];
        outNormalTS += normal[i].rgb * weights[i];
        outMasks += masks[i].yz * weights[i];
    }
    #ifndef _NORMALMAP
        #ifdef SURFACE_GRADIENT
            outNormalTS = float3(0.0, 0.0, 0.0); // No gradient
        #else
            outNormalTS = float3(0.0, 0.0, 1.0);
        #endif
    #endif
    outSmoothness = outMasks.x;
    outMetallic = outMasks.y;
}
