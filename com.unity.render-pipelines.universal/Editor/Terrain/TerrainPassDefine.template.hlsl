
#define SHADERPASS_MAINTEX     (27)
#define SHADERPASS_METALLICTEX (28)

#ifdef UNIVERSAL_TERRAIN_SPLAT01
#define GetSplat0UV(x) x.uvSplat01.xy
#define GetSplat1UV(x) x.uvSplat01.zw
#define GetSplat4UV(x) x.uvSplat01.xy
#define GetSplat5UV(x) x.uvSplat01.zw
#else
#define GetSplat0UV(x) 0.0
#define GetSplat1UV(x) 0.0
#define GetSplat4UV(x) 0.0
#define GetSplat5UV(x) 0.0
#endif

#ifdef UNIVERSAL_TERRAIN_SPLAT23
#define GetSplat2UV(x) x.uvSplat23.xy
#define GetSplat3UV(x) x.uvSplat23.zw
#define GetSplat6UV(x) x.uvSplat23.xy
#define GetSplat7UV(x) x.uvSplat23.zw
#else
#define GetSplat2UV(x) 0.0
#define GetSplat3UV(x) 0.0
#define GetSplat6UV(x) 0.0
#define GetSplat7UV(x) 0.0
#endif

SAMPLER(sampler_Splat1);
SAMPLER(sampler_Splat2);
SAMPLER(sampler_Splat3);

//SAMPLER(sampler_Normal1);
//SAMPLER(sampler_Normal2);
//SAMPLER(sampler_Normal3);

SAMPLER(sampler_Mask1);
SAMPLER(sampler_Mask2);
SAMPLER(sampler_Mask3);

#ifdef SampleLayerAlbedo
#undef SampleLayerAlbedo
#endif
#ifdef SampleLayerNormal
#undef SampleLayerNormal
#endif
#ifdef SampleLayerMasks
#undef SampleLayerMasks
#endif

#define SampleLayerAlbedo(i) (SAMPLE_TEXTURE2D(_Splat##i, sampler_Splat##i, splatuv) * half4(_DiffuseRemapScale##i.rgb, 1.0h))

#ifdef _NORMALMAP
    #define SampleLayerNormal(i) UnpackNormalScale(SAMPLE_TEXTURE2D(_Normal##i, sampler_Splat##i, splatuv), _NormalScale##i)
#else
    #define SampleLayerNormal(i) half3(0.0, 0.0, 1.0)
#endif

#ifdef _MASKMAP
    #define SampleLayerMasks(i) (_MaskMapRemapOffset##i + _MaskMapRemapScale##i * lerp(0.5h, SAMPLE_TEXTURE2D(_Mask##i, sampler_Mask##i, splatuv), _LayerHasMask##i));
#else
    #define SampleLayerMasks(i) (_MaskMapRemapOffset##i + _MaskMapRemapScale##i * 0.5h);
#endif

#define FETCH_SPLAT_CONTROL0                                                                        \
    float2 controlUV0 = (IN.uv0.xy * (_Control_TexelSize.zw - 1.0) + 0.5) * _Control_TexelSize.xy;  \
    half4 splatControl0 = SAMPLE_TEXTURE2D(_Control, sampler_Control, controlUV0);                  \
    half controlLerp0 = _DstBlend;

#define FETCH_SPLAT_CONTROL1                                                                        \
    float2 controlUV1 = (IN.uv0.xy * (_Control_TexelSize.zw - 1.0) + 0.5) * _Control_TexelSize.xy;  \
    half4 splatControl1 = SAMPLE_TEXTURE2D(_Control, sampler_Control, controlUV1);                  \
    half controlLerp1 = 1.0 - _DstBlend;

#define DECLARE_LAYER_PREREQUISITES \
    half2 splatuv;

#define DECLARE_LAYER_ATTRIBUTES(i) \
    half4 albedoSmoothness##i;      \
    half3 normal##i;                \
    half4 mask##i;                  \
    half defaultSmoothness##i;      \
    half defaultMetallic##i;        \
    half defaultOcclusion##i;       \
    half layerLerp##i;

#define FETCH_LAYER_ATTRIBUTES(i)                                                                           \
    splatuv = GetSplat##i##UV(IN);                                                                          \
    albedoSmoothness##i = SampleLayerAlbedo(i);                                                             \
    normal##i = SampleLayerNormal(i);                                                                       \
    mask##i = SampleLayerMasks(i);                                                                          \
    defaultSmoothness##i = albedoSmoothness##i.a * _Smoothness##i;                                          \
    defaultMetallic##i = _Metallic##i;                                                                      \
    defaultOcclusion##i = _MaskMapRemapScale##i.g * _MaskMapRemapOffset##i.g;                               \
    layerLerp##i = _DstBlend;

#define FETCH_LAYER_ATTRIBUTES_8LAYERS(layerIndex, sampleIndex)                                             \
    splatuv = GetSplat##sampleIndex##UV(IN);                                                                \
    albedoSmoothness##layerIndex = SampleLayerAlbedo(sampleIndex);                                          \
    normal##layerIndex = SampleLayerNormal(sampleIndex);                                                    \
    mask##layerIndex = SampleLayerMasks(sampleIndex);                                                       \
    defaultSmoothness##layerIndex = albedoSmoothness##layerIndex.a * _Smoothness##sampleIndex;              \
    defaultMetallic##layerIndex = _Metallic##sampleIndex;                                                   \
    defaultOcclusion##layerIndex = _MaskMapRemapScale##sampleIndex.g * _MaskMapRemapOffset##sampleIndex.g;  \
    layerLerp##layerIndex = 1.0 - _DstBlend;

#define DECLARE_AND_FETCH_LAYER_ATTRIBUTES(i)                               \
    DECLARE_LAYER_ATTRIBUTES(i)                                             \
    FETCH_LAYER_ATTRIBUTES(i)

#define DECLARE_AND_FETCH_LAYER_ATTRIBUTES_8LAYERS(layerIndex, sampleIndex) \
    DECLARE_LAYER_ATTRIBUTES(layerIndex)                                    \
    FETCH_LAYER_ATTRIBUTES_8LAYERS(layerIndex, sampleIndex)

#define FetchControl0 splatControl0
#define FetchControl1 splatControl1

#define FetchLayerAlbedo0 albedoSmoothness0.rgb
#define FetchLayerAlbedo1 albedoSmoothness1.rgb
#define FetchLayerAlbedo2 albedoSmoothness2.rgb
#define FetchLayerAlbedo3 albedoSmoothness3.rgb
#define FetchLayerAlbedo4 albedoSmoothness4.rgb
#define FetchLayerAlbedo5 albedoSmoothness5.rgb
#define FetchLayerAlbedo6 albedoSmoothness6.rgb
#define FetchLayerAlbedo7 albedoSmoothness7.rgb

#define FetchLayerNormal0 normal0
#define FetchLayerNormal1 normal1
#define FetchLayerNormal2 normal2
#define FetchLayerNormal3 normal3
#define FetchLayerNormal4 normal4
#define FetchLayerNormal5 normal5
#define FetchLayerNormal6 normal6
#define FetchLayerNormal7 normal7

#define FetchLayerMetallic0 lerp(defaultMetallic0, mask0.r, _LayerHasMask0)
#define FetchLayerMetallic1 lerp(defaultMetallic1, mask1.r, _LayerHasMask1)
#define FetchLayerMetallic2 lerp(defaultMetallic2, mask2.r, _LayerHasMask2)
#define FetchLayerMetallic3 lerp(defaultMetallic3, mask3.r, _LayerHasMask3)
#define FetchLayerMetallic4 lerp(defaultMetallic4, mask4.r, _LayerHasMask0)
#define FetchLayerMetallic5 lerp(defaultMetallic5, mask5.r, _LayerHasMask1)
#define FetchLayerMetallic6 lerp(defaultMetallic6, mask6.r, _LayerHasMask2)
#define FetchLayerMetallic7 lerp(defaultMetallic7, mask7.r, _LayerHasMask3)

#define FetchLayerSmoothness0 lerp(defaultSmoothness0, mask0.a, _LayerHasMask0)
#define FetchLayerSmoothness1 lerp(defaultSmoothness1, mask1.a, _LayerHasMask1)
#define FetchLayerSmoothness2 lerp(defaultSmoothness2, mask2.a, _LayerHasMask2)
#define FetchLayerSmoothness3 lerp(defaultSmoothness3, mask3.a, _LayerHasMask3)
#define FetchLayerSmoothness4 lerp(defaultSmoothness4, mask4.a, _LayerHasMask0)
#define FetchLayerSmoothness5 lerp(defaultSmoothness5, mask5.a, _LayerHasMask1)
#define FetchLayerSmoothness6 lerp(defaultSmoothness6, mask6.a, _LayerHasMask2)
#define FetchLayerSmoothness7 lerp(defaultSmoothness7, mask7.a, _LayerHasMask3)

#define FetchLayerOcclusion0 lerp(defaultOcclusion0, mask0.g, _LayerHasMask0)
#define FetchLayerOcclusion1 lerp(defaultOcclusion1, mask1.g, _LayerHasMask1)
#define FetchLayerOcclusion2 lerp(defaultOcclusion2, mask2.g, _LayerHasMask2)
#define FetchLayerOcclusion3 lerp(defaultOcclusion3, mask3.g, _LayerHasMask3)
#define FetchLayerOcclusion4 lerp(defaultOcclusion4, mask4.g, _LayerHasMask0)
#define FetchLayerOcclusion5 lerp(defaultOcclusion5, mask5.g, _LayerHasMask1)
#define FetchLayerOcclusion6 lerp(defaultOcclusion6, mask6.g, _LayerHasMask2)
#define FetchLayerOcclusion7 lerp(defaultOcclusion7, mask7.g, _LayerHasMask3)

#if !defined(_TERRAIN_BASEMAP_GEN) && !defined(TERRAIN_SPLAT_ADDPASS) // 0 ~ 3 layers pass
    #undef FetchControl1
    #define FetchControl1 half4(0.0h, 0.0h, 0.0h, 0.0h)

    #undef FetchLayerAlbedo4
    #undef FetchLayerAlbedo5
    #undef FetchLayerAlbedo6
    #undef FetchLayerAlbedo7
    #define FetchLayerAlbedo4 0.0
    #define FetchLayerAlbedo5 0.0
    #define FetchLayerAlbedo6 0.0
    #define FetchLayerAlbedo7 0.0

    #undef FetchLayerNormal4
    #undef FetchLayerNormal5
    #undef FetchLayerNormal6
    #undef FetchLayerNormal7
    #define FetchLayerNormal4 0.0
    #define FetchLayerNormal5 0.0
    #define FetchLayerNormal6 0.0
    #define FetchLayerNormal7 0.0

    #undef FetchLayerMetallic4
    #undef FetchLayerMetallic5
    #undef FetchLayerMetallic6
    #undef FetchLayerMetallic7
    #define FetchLayerMetallic4 0.0
    #define FetchLayerMetallic5 0.0
    #define FetchLayerMetallic6 0.0
    #define FetchLayerMetallic7 0.0

    #undef FetchLayerSmoothness4
    #undef FetchLayerSmoothness5
    #undef FetchLayerSmoothness6
    #undef FetchLayerSmoothness7
    #define FetchLayerSmoothness4 0.0
    #define FetchLayerSmoothness5 0.0
    #define FetchLayerSmoothness6 0.0
    #define FetchLayerSmoothness7 0.0

    #undef FetchLayerOcclusion4
    #undef FetchLayerOcclusion5
    #undef FetchLayerOcclusion6
    #undef FetchLayerOcclusion7
    #define FetchLayerOcclusion4 0.0
    #define FetchLayerOcclusion5 0.0
    #define FetchLayerOcclusion6 0.0
    #define FetchLayerOcclusion7 0.0
#elif !defined(_TERRAIN_BASEMAP_GEN) && defined(TERRAIN_SPLAT_ADDPASS) // 4 ~ 7 layers pass(addpass)
    #undef FetchControl0
    #define FetchControl0 half4(0.0h, 0.0h, 0.0h, 0.0h)

    #undef FetchLayerAlbedo0
    #undef FetchLayerAlbedo1
    #undef FetchLayerAlbedo2
    #undef FetchLayerAlbedo3
    #define FetchLayerAlbedo0 0.0
    #define FetchLayerAlbedo1 0.0
    #define FetchLayerAlbedo2 0.0
    #define FetchLayerAlbedo3 0.0

    #undef FetchLayerNormal0
    #undef FetchLayerNormal1
    #undef FetchLayerNormal2
    #undef FetchLayerNormal3
    #define FetchLayerNormal0 0.0
    #define FetchLayerNormal1 0.0
    #define FetchLayerNormal2 0.0
    #define FetchLayerNormal3 0.0

    #undef FetchLayerMetallic0
    #undef FetchLayerMetallic1
    #undef FetchLayerMetallic2
    #undef FetchLayerMetallic3
    #define FetchLayerMetallic0 0.0
    #define FetchLayerMetallic1 0.0
    #define FetchLayerMetallic2 0.0
    #define FetchLayerMetallic3 0.0

    #undef FetchLayerSmoothness0
    #undef FetchLayerSmoothness1
    #undef FetchLayerSmoothness2
    #undef FetchLayerSmoothness3
    #define FetchLayerSmoothness0 0.0
    #define FetchLayerSmoothness1 0.0
    #define FetchLayerSmoothness2 0.0
    #define FetchLayerSmoothness3 0.0

    #undef FetchLayerOcclusion0
    #undef FetchLayerOcclusion1
    #undef FetchLayerOcclusion2
    #undef FetchLayerOcclusion3
    #define FetchLayerOcclusion0 0.0
    #define FetchLayerOcclusion1 0.0
    #define FetchLayerOcclusion2 0.0
    #define FetchLayerOcclusion3 0.0
#endif

#if !defined(_TERRAIN_BASEMAP_GEN)
    #define FetchControl(i) FetchControl##i

    #define FetchLayerAlbedo(i, control) (FetchLayerAlbedo##i * control)
    #define FetchLayerNormal(i, control) (FetchLayerNormal##i * control)
    #define FetchLayerMetallic(i, control) (FetchLayerMetallic##i * control)
    #define FetchLayerSmoothness(i, control) (FetchLayerSmoothness##i * control)
    #define FetchLayerOcclusion(i, control) (FetchLayerOcclusion##i * control)
#else
    #define FetchControl(i) lerp(FetchControl##i, 0.0, controlLerp##i)

    #define FetchLayerAlbedo(i, control) (FetchLayerAlbedo##i * lerp(control, 0.0, layerLerp##i))
    #define FetchLayerNormal(i, control) (FetchLayerNormal##i * lerp(control, 0.0, layerLerp##i))
    #define FetchLayerMetallic(i, control) (FetchLayerMetallic##i * lerp(control, 0.0, layerLerp##i))
    #define FetchLayerSmoothness(i, control) (FetchLayerSmoothness##i * lerp(control, 0.0, layerLerp##i))
    #define FetchLayerOcclusion(i, control) (FetchLayerOcclusion##i * lerp(control, 0.0, layerLerp##i))
#endif
