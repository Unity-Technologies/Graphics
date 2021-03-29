#ifndef UIVERSAL_DBUFFER_INDLUDED
#define UIVERSAL_DBUFFER_INDLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"


#if defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
#define _DBUFFER
#endif

#define DBufferType0 half4
#define DBufferType1 half4
#define DBufferType2 half4

#if defined(_DBUFFER_MRT3)

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

#elif defined(_DBUFFER_MRT2)

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
void EncodeIntoDBuffer(DecalSurfaceData surfaceData
    , out DBufferType0 outDBuffer0
#if defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
    , out DBufferType1 outDBuffer1
#endif
#if defined(_DBUFFER_MRT3)
    , out DBufferType2 outDBuffer2
#endif
)
{
    outDBuffer0 = surfaceData.baseColor;
#if defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
    outDBuffer1 = half4(surfaceData.normalWS.xyz * 0.5 + 0.5, surfaceData.normalWS.w);
#endif
#if defined(_DBUFFER_MRT3)
    outDBuffer2 = surfaceData.mask;
#endif
}

void DecodeFromDBuffer(
    DBufferType0 inDBuffer0
#if defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
    , DBufferType1 inDBuffer1
#endif
#if defined(_DBUFFER_MRT3) || defined(DECALS_4RT)
    , DBufferType2 inDBuffer2
#endif
    , out DecalSurfaceData surfaceData
)
{
    ZERO_INITIALIZE(DecalSurfaceData, surfaceData);
    surfaceData.baseColor = inDBuffer0;
#if defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
    // Use (254.0 / 255.0) instead of 0.5 to allow to encode 0 perfectly (encode as 127)
    // Range goes from -0.99607 to 1.0039
    surfaceData.normalWS.xyz = inDBuffer1.xyz * 2.0 - (254.0 / 255.0);
    surfaceData.normalWS.w = inDBuffer1.w;
#endif
#if defined(_DBUFFER_MRT3)
    surfaceData.mask = inDBuffer2;
    surfaceData.MAOSBlend = half2(surfaceData.mask.w, surfaceData.mask.w);
#endif
}

DECLARE_DBUFFER_TEXTURE(_DBufferTexture);

void ApplyDecalToSurfaceData(float4 positionCS, inout SurfaceData surfaceData, inout InputData inputData)
{
    FETCH_DBUFFER(DBuffer, _DBufferTexture, int2(positionCS.xy));

    DecalSurfaceData decalSurfaceData;
    DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html, mean weight of 1 is neutral

    // Note: We only test weight (i.e decalSurfaceData.xxx.w is < 1.0) if it can save something
    surfaceData.albedo.xyz = surfaceData.albedo.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;

#if defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
    // Always test the normal as we can have decompression artifact
    if (decalSurfaceData.normalWS.w < 1.0) // TODO
    {
        inputData.normalWS.xyz = normalize(inputData.normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }
#endif

#if defined(_DBUFFER_MRT3)
#ifdef _SPECULAR_SETUP
    if (decalSurfaceData.MAOSBlend.x < 1.0)
    {
        float3 decalSpecularColor = ComputeFresnel0((decalSurfaceData.baseColor.w < 1.0) ? decalSurfaceData.baseColor.xyz : float3(1.0, 1.0, 1.0), decalSurfaceData.mask.x, DEFAULT_SPECULAR_VALUE);
        surfaceData.specularColor = surfaceData.specularColor * decalSurfaceData.MAOSBlend.x + decalSpecularColor * (1.0f - decalSurfaceData.MAOSBlend.x);
    }
#else
    surfaceData.metallic = surfaceData.metallic * decalSurfaceData.MAOSBlend.x + decalSurfaceData.mask.x;
#endif

    // TODO MAOSBlend

    surfaceData.occlusion = surfaceData.occlusion * decalSurfaceData.MAOSBlend.y + decalSurfaceData.mask.y;

    surfaceData.smoothness = surfaceData.smoothness * decalSurfaceData.mask.w + decalSurfaceData.mask.z;
#endif
}
#endif // UIVERSAL_DBUFFER_INDLUDED
