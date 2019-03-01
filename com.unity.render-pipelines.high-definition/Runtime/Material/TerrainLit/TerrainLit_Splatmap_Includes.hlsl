#ifdef _TERRAIN_8_LAYERS
    #define _LAYER_COUNT 8
#else
    #define _LAYER_COUNT 4
#endif

#define DECLARE_TERRAIN_LAYER_PROPS(n)  \
    float4 _Splat##n##_ST;              \
    float _Metallic##n;                 \
    float _Smoothness##n;               \
    float _NormalScale##n;              \
    float4 _DiffuseRemapScale##n;       \
    float4 _MaskMapRemapOffset##n;      \
    float4 _MaskMapRemapScale##n;

#define DECLARE_TERRAIN_LAYER_PROPS_FIRST_4 \
    DECLARE_TERRAIN_LAYER_PROPS(0)          \
    DECLARE_TERRAIN_LAYER_PROPS(1)          \
    DECLARE_TERRAIN_LAYER_PROPS(2)          \
    DECLARE_TERRAIN_LAYER_PROPS(3)          \

#ifdef _TERRAIN_8_LAYERS
    #define UNITY_TERRAIN_CB_VARS \
        DECLARE_TERRAIN_LAYER_PROPS_FIRST_4 \
        DECLARE_TERRAIN_LAYER_PROPS(4)      \
        DECLARE_TERRAIN_LAYER_PROPS(5)      \
        DECLARE_TERRAIN_LAYER_PROPS(6)      \
        DECLARE_TERRAIN_LAYER_PROPS(7)      \
        float _HeightTransition;
#else
    #define UNITY_TERRAIN_CB_VARS \
        DECLARE_TERRAIN_LAYER_PROPS_FIRST_4 \
        float _HeightTransition;
#endif

#define UNITY_TERRAIN_CB_DEBUG_VARS \
    float4 _Control0_TexelSize;     \
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
