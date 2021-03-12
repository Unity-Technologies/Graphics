#define DBufferType0 float4
#define DBufferType1 float4
#define DBufferType2 float4

#if defined(DECALS_3RT)

#define OUTPUT_DBUFFER(NAME)                            \
    out DBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,  \
    out DBufferType1 MERGE_NAME(NAME, 1) : SV_Target1,  \
    out DBufferType2 MERGE_NAME(NAME, 2) : SV_Target2

#define DECLARE_DBUFFER_TEXTURE(NAME)   \
    TEXTURE2D_X(MERGE_NAME(NAME, 0));       \
    TEXTURE2D_X(MERGE_NAME(NAME, 1));       \
    TEXTURE2D_X(MERGE_NAME(NAME, 2));

#define FETCH_DBUFFER(NAME, TEX, unCoord2)                                              \
    DBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 0), unCoord2);  \
    DBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 1), unCoord2);  \
    DBufferType2 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 2), unCoord2);

#define ENCODE_INTO_DBUFFER(DECAL_SURFACE_DATA, NAME) EncodeIntoDBuffer(DECAL_SURFACE_DATA, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))
#define DECODE_FROM_DBUFFER(NAME, DECAL_SURFACE_DATA) DecodeFromDBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2), DECAL_SURFACE_DATA)

#elif defined(DECALS_2RT)

#define OUTPUT_DBUFFER(NAME)                            \
    out DBufferType0 MERGE_NAME(NAME, 0) : SV_Target0,  \
    out DBufferType1 MERGE_NAME(NAME, 1) : SV_Target1

#define DECLARE_DBUFFER_TEXTURE(NAME)   \
    TEXTURE2D_X(MERGE_NAME(NAME, 0));       \
    TEXTURE2D_X(MERGE_NAME(NAME, 1));

#define FETCH_DBUFFER(NAME, TEX, unCoord2)                                              \
    DBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 0), unCoord2);  \
    DBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 1), unCoord2);

#define ENCODE_INTO_DBUFFER(DECAL_SURFACE_DATA, NAME) EncodeIntoDBuffer(DECAL_SURFACE_DATA, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1))
#define DECODE_FROM_DBUFFER(NAME, DECAL_SURFACE_DATA) DecodeFromDBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), DECAL_SURFACE_DATA)


#else

#define OUTPUT_DBUFFER(NAME)                            \
    out DBufferType0 MERGE_NAME(NAME, 0) : SV_Target0

#define DECLARE_DBUFFER_TEXTURE(NAME)   \
    TEXTURE2D_X(MERGE_NAME(NAME, 0));

#define FETCH_DBUFFER(NAME, TEX, unCoord2)                                              \
    DBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 0), unCoord2);

#define ENCODE_INTO_DBUFFER(DECAL_SURFACE_DATA, NAME) EncodeIntoDBuffer(DECAL_SURFACE_DATA, MERGE_NAME(NAME,0))
#define DECODE_FROM_DBUFFER(NAME, DECAL_SURFACE_DATA) DecodeFromDBuffer(MERGE_NAME(NAME,0), DECAL_SURFACE_DATA)

#endif

// Must be in sync with RT declared in HDRenderPipeline.cs ::Rebuild
void EncodeIntoDBuffer( DecalSurfaceData surfaceData
                        , out DBufferType0 outDBuffer0
#if defined(DECALS_2RT) || defined(DECALS_3RT)
                        , out DBufferType1 outDBuffer1
#endif
#if defined(DECALS_3RT)
                        , out DBufferType2 outDBuffer2
#endif
                        )
{
    outDBuffer0 = surfaceData.baseColor;
#if defined(DECALS_2RT) || defined(DECALS_3RT)
    outDBuffer1 = float4(surfaceData.normalWS.xyz * 0.5 + 0.5, surfaceData.normalWS.w);
#endif
#if defined(DECALS_3RT)
    outDBuffer2 = surfaceData.mask;
#endif
}

void DecodeFromDBuffer(
    DBufferType0 inDBuffer0
#if defined(DECALS_2RT) || defined(DECALS_3RT)
    , DBufferType1 inDBuffer1
#endif
#if defined(DECALS_3RT) || defined(DECALS_4RT)
    , DBufferType2 inDBuffer2
#endif
    , out DecalSurfaceData surfaceData
)
{
    ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
    surfaceData.baseColor = inDBuffer0;
#if defined(DECALS_2RT) || defined(DECALS_3RT)
    // Use (254.0 / 255.0) instead of 0.5 to allow to encode 0 perfectly (encode as 127)
    // Range goes from -0.99607 to 1.0039
    surfaceData.normalWS.xyz = inDBuffer1.xyz * 2.0 - (254.0 / 255.0);
    surfaceData.normalWS.w = inDBuffer1.w;
#endif
#if defined(DECALS_3RT)
    surfaceData.mask = inDBuffer2;
    surfaceData.MAOSBlend = float2(surfaceData.mask.w, surfaceData.mask.w);
#endif
}
