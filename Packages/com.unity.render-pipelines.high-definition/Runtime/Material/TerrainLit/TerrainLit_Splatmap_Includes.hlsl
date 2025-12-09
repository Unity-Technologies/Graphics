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
    float4 _Splat##n##_TexelSize;       \
    float _Metallic##n;                 \
    float _Smoothness##n;               \
    float _NormalScale##n;              \
    float4 _DiffuseRemapScale##n;       \
    float4 _MaskMapRemapOffset##n;      \
    float4 _MaskMapRemapScale##n;       \
    float _LayerHasMask##n;             \
    float _SmoothnessSource##n;

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
        uint _NumLayersCount;
#else
    #define UNITY_TERRAIN_CB_VARS \
        DECLARE_TERRAIN_LAYER_PROPS_FIRST_4 \
        float _HeightTransition;
        uint _NumLayersCount;
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
#define UNITY_TERRAIN_CB_DEBUG_VARS \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Control0); \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Splat0);   \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Splat1);   \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Splat2);   \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Splat3);   \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Splat4);   \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Splat5);   \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Splat6);   \
    UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_Splat7);
