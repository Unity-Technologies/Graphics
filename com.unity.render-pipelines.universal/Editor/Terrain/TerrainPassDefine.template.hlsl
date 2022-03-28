
#define SHADERPASS_MAINTEX     (27)
#define SHADERPASS_METALLICTEX (28)

#ifdef UNIVERSAL_TERRAIN_SPLAT01
#define GetSplat0UV(x) x.uvSplat01.xy
#define GetSplat1UV(x) x.uvSplat01.zw
#else
#define GetSplat0UV(x) 0.0
#define GetSplat1UV(x) 0.0
#endif

#ifdef UNIVERSAL_TERRAIN_SPLAT23
#define GetSplat2UV(x) x.uvSplat23.xy
#define GetSplat3UV(x) x.uvSplat23.zw
#else
#define GetSplat2UV(x) 0.0
#define GetSplat3UV(x) 0.0
#endif

#define DECLARE_SPLAT_ATTRIBUTES(i) \
    half2 splat##i##uv;             \
    half4 albedoSmoothness##i;      \
    half3 normal##i;                \
    half4 mask##i;                  \
    half defaultSmoothness##i;      \
    half defaultMetallic##i;        \
    half defaultOcclusion##i;       \

#define FETCH_SPLAT_ATTRIBUTES(i)                                               \
    splat##i##uv = GetSplat##i##UV(IN);                                         \
    albedoSmoothness##i = SampleLayerAlbedo(i);                                 \
    normal##i = SampleLayerNormal(i);                                           \
    mask##i = SampleLayerMasks(i);                                              \
    defaultSmoothness##i = albedoSmoothness##i.a * _Smoothness##i;              \
    defaultMetallic##i = _Metallic##i;                                          \
    defaultOcclusion##i = _MaskMapRemapScale##i.g * _MaskMapRemapOffset##i.g;   \

#define DECLARE_AND_FETCH_SPLAT_ATTRIBUTES(i)   \
    DECLARE_SPLAT_ATTRIBUTES(i)                 \
    FETCH_SPLAT_ATTRIBUTES(i)                   \

#define FetchLayerAlbedo(i) albedoSmoothness##i.rgb
#define FetchLayerNormal(i) normal##i
#define FetchLayerMetallic(i) lerp(defaultMetallic##i, mask##i.r, _LayerHasMask##i)
#define FetchLayerSmoothness(i) lerp(defaultSmoothness##i, mask##i.a, _LayerHasMask##i)
#define FetchLayerOcclusion(i) lerp(defaultOcclusion##i, mask##i.g, _LayerHasMask##i)
