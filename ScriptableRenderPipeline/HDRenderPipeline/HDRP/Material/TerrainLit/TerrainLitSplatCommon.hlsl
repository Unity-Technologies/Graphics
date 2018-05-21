
#if defined(_TERRAIN_8_SPLATS)
    #define _LAYER_COUNT 8
#elif defined(_TERRAIN_7_SPLATS)
    #define _LAYER_COUNT 7
#elif defined(_TERRAIN_6_SPLATS)
    #define _LAYER_COUNT 6
#elif defined(_TERRAIN_5_SPLATS)
    #define _LAYER_COUNT 5
#elif defined(_TERRAIN_4_SPLATS)
    #define _LAYER_COUNT 4
#elif defined(_TERRAIN_3_SPLATS)
    #define _LAYER_COUNT 3
#elif defined(_TERRAIN_2_SPLATS)
    #define _LAYER_COUNT 2
#else
    #define _LAYER_COUNT 1
#endif

#define DECLARE_TERRAIN_LAYER(n)    \
    TEXTURE2D(_Splat##n);           \
    TEXTURE2D(_Normal##n);          \
    TEXTURE2D(_Height##n);          \
    float4 _Splat##n##_ST;          \
    float _Metallic##n;             \
    float _Smoothness##n;           \
    float _HeightCenter##n;         \
    float _HeightAmplitude##n

DECLARE_TERRAIN_LAYER(0);
DECLARE_TERRAIN_LAYER(1);
DECLARE_TERRAIN_LAYER(2);
DECLARE_TERRAIN_LAYER(3);
DECLARE_TERRAIN_LAYER(4);
DECLARE_TERRAIN_LAYER(5);
DECLARE_TERRAIN_LAYER(6);
DECLARE_TERRAIN_LAYER(7);

#undef DECLARE_TERRAIN_LAYER

TEXTURE2D(_Control0);
TEXTURE2D(_Control1);

SAMPLER(sampler_Splat0);
SAMPLER(sampler_Control0);

float GetMaxHeight(float4 heights0
#if _LAYER_COUNT > 4
    , float4 heights1
#endif
)
{
    float maxHeight = heights0.r;
#if _LAYER_COUNT > 1
    maxHeight = max(maxHeight, heights0.g);
#endif
#if _LAYER_COUNT > 2
    maxHeight = max(maxHeight, heights0.b);
#endif
#if _LAYER_COUNT > 3
    maxHeight = max(maxHeight, heights0.a);
#endif
#if _LAYER_COUNT > 4
    maxHeight = max(maxHeight, heights1.r);
#endif
#if _LAYER_COUNT > 5
    maxHeight = max(maxHeight, heights1.g);
#endif
#if _LAYER_COUNT > 6
    maxHeight = max(maxHeight, heights1.b);
#endif
#if _LAYER_COUNT > 7
    maxHeight = max(maxHeight, heights1.a);
#endif
    return maxHeight;
}

float GetSumHeight(float4 heights0
#if _LAYER_COUNT > 4
    , float4 heights1
#endif
)
{
    float sumHeight = heights0.r;
#if _LAYER_COUNT > 1
    sumHeight += heights0.g;
#endif
#if _LAYER_COUNT > 2
    sumHeight += heights0.b;
#endif
#if _LAYER_COUNT > 3
    sumHeight += heights0.a;
#endif
#if _LAYER_COUNT > 4
    sumHeight += heights1.r;
#endif
#if _LAYER_COUNT > 5
    sumHeight += heights1.g;
#endif
#if _LAYER_COUNT > 6
    sumHeight += heights1.b;
#endif
#if _LAYER_COUNT > 7
    sumHeight += heights1.a;
#endif
    return sumHeight;
}

float _HeightTransition;

// Returns layering blend mask after application of height based blend.
void ApplyHeightBlend(float4 weightedHeights0, float4 weightedHeights1, inout float4 blendMasks0, inout float4 blendMasks1)
{
    float maxHeight = GetMaxHeight(weightedHeights0
#if _LAYER_COUNT > 4
        , weightedHeights1
#endif
    );
    // Make sure that transition is not zero otherwise the next computation will be wrong.
    // The epsilon here also has to be bigger than the epsilon in the next computation.
    float transition = max(_HeightTransition, 1e-5);

    // The goal here is to have all but the highest layer at negative heights, then we add the transition so that if the next highest layer is near transition it will have a positive value.
    // Then we clamp this to zero and normalize everything so that highest layer has a value of 1.
    weightedHeights0 = weightedHeights0 - maxHeight.xxxx;
    // We need to add an epsilon here for active layers (hence the blendMask again) so that at least a layer shows up if everything's too low.
    weightedHeights0 = (max(0, weightedHeights0 + transition) + 1e-6) * blendMasks0;

#if _LAYER_COUNT > 4
    weightedHeights1 = weightedHeights1 - maxHeight.xxxx;
    weightedHeights1 = (max(0, weightedHeights1 + transition) + 1e-6) * blendMasks1;
#endif

    // Normalize
    float totalHeight = GetSumHeight(weightedHeights0
#if _LAYER_COUNT > 4
        , weightedHeights1
#endif
    );
    blendMasks0 = weightedHeights0 / totalHeight.xxxx;
#if _LAYER_COUNT > 4
    blendMasks1 = weightedHeights1 / totalHeight.xxxx;
#endif
}
