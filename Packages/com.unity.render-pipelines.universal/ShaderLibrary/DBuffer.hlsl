#ifndef UNIVERSAL_DBUFFER_INCLUDED
#define UNIVERSAL_DBUFFER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DecalInput.hlsl"


#if (defined(_DBUFFER_MRT1) || defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)) && !defined(_SURFACE_TYPE_TRANSPARENT)
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

#define DECLARE_DBUFFER_TEXTURE(NAME)            \
    TEXTURE2D_X_HALF(MERGE_NAME(NAME, 0));       \
    TEXTURE2D_X_HALF(MERGE_NAME(NAME, 1));       \
    TEXTURE2D_X_HALF(MERGE_NAME(NAME, 2));

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

#define DECLARE_DBUFFER_TEXTURE(NAME)       \
    TEXTURE2D_X_HALF(MERGE_NAME(NAME, 0));  \
    TEXTURE2D_X_HALF(MERGE_NAME(NAME, 1));

#define FETCH_DBUFFER(NAME, TEX, unCoord2)                                              \
    DBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 0), unCoord2);  \
    DBufferType1 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 1), unCoord2);

#define ENCODE_INTO_DBUFFER(DECAL_SURFACE_DATA, NAME) EncodeIntoDBuffer(DECAL_SURFACE_DATA, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1))
#define DECODE_FROM_DBUFFER(NAME, DECAL_SURFACE_DATA) DecodeFromDBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), DECAL_SURFACE_DATA)


#else

#define OUTPUT_DBUFFER(NAME)                            \
    out DBufferType0 MERGE_NAME(NAME, 0) : SV_Target0

#define DECLARE_DBUFFER_TEXTURE(NAME)       \
    TEXTURE2D_X_HALF(MERGE_NAME(NAME, 0));

#define FETCH_DBUFFER(NAME, TEX, unCoord2)                                              \
    DBufferType0 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D_X(MERGE_NAME(TEX, 0), unCoord2);

#define ENCODE_INTO_DBUFFER(DECAL_SURFACE_DATA, NAME) EncodeIntoDBuffer(DECAL_SURFACE_DATA, MERGE_NAME(NAME,0))
#define DECODE_FROM_DBUFFER(NAME, DECAL_SURFACE_DATA) DecodeFromDBuffer(MERGE_NAME(NAME,0), DECAL_SURFACE_DATA)

#endif

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
    outDBuffer2 = half4(surfaceData.metallic, surfaceData.occlusion, surfaceData.smoothness, surfaceData.MAOSAlpha);
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
    surfaceData.metallic = inDBuffer2.x;
    surfaceData.occlusion = inDBuffer2.y;
    surfaceData.smoothness = inDBuffer2.z;
    surfaceData.MAOSAlpha = inDBuffer2.w;
#endif
}

DECLARE_DBUFFER_TEXTURE(_DBufferTexture);

void ApplyDecal(float4 positionCS,
    inout half3 baseColor,
    inout half3 specularColor,
    inout half3 normalWS,
    inout half metallic,
    inout half occlusion,
    inout half smoothness)
{
    FETCH_DBUFFER(DBuffer, _DBufferTexture, int2(positionCS.xy));

    DecalSurfaceData decalSurfaceData;
    DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html, mean weight of 1 is neutral

    // Note: We only test weight (i.e decalSurfaceData.xxx.w is < 1.0) if it can save something
    baseColor.xyz = baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;

#if defined(_DBUFFER_MRT2) || defined(_DBUFFER_MRT3)
    // Always test the normal as we can have decompression artifact
    if (decalSurfaceData.normalWS.w < 1.0)
    {
        normalWS.xyz = SafeNormalize(normalWS.xyz * decalSurfaceData.normalWS.w + decalSurfaceData.normalWS.xyz);
    }
#endif

#if defined(_DBUFFER_MRT3)
#ifdef _SPECULAR_SETUP
    if (decalSurfaceData.MAOSAlpha.x < 1.0)
    {
        half3 decalSpecularColor = ComputeFresnel0((decalSurfaceData.baseColor.w < 1.0) ? decalSurfaceData.baseColor.xyz : half3(1.0, 1.0, 1.0), decalSurfaceData.metallic, DEFAULT_SPECULAR_VALUE);
        specularColor = specularColor * decalSurfaceData.MAOSAlpha + decalSpecularColor * (1.0f - decalSurfaceData.MAOSAlpha);
    }
#else
    metallic = metallic * decalSurfaceData.MAOSAlpha + decalSurfaceData.metallic;
#endif

    occlusion = occlusion * decalSurfaceData.MAOSAlpha + decalSurfaceData.occlusion;

    smoothness = smoothness * decalSurfaceData.MAOSAlpha + decalSurfaceData.smoothness;
#endif
}

void ApplyDecalToBaseColor(float4 positionCS, inout half3 baseColor)
{
    FETCH_DBUFFER(DBuffer, _DBufferTexture, int2(positionCS.xy));

    DecalSurfaceData decalSurfaceData;
    DECODE_FROM_DBUFFER(DBuffer, decalSurfaceData);

    // using alpha compositing https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html, mean weight of 1 is neutral

    // Note: We only test weight (i.e decalSurfaceData.xxx.w is < 1.0) if it can save something
    baseColor.xyz = baseColor.xyz * decalSurfaceData.baseColor.w + decalSurfaceData.baseColor.xyz;
}

void ApplyDecalToBaseColorAndNormal(float4 positionCS, inout half3 baseColor, inout half3 normalWS)
{
    half3 specular = 0;
    half metallic = 0;
    half occlusion = 0;
    half smoothness = 0;
    ApplyDecal(positionCS,
        baseColor,
        specular,
        normalWS,
        metallic,
        occlusion,
        smoothness);
}

void ApplyDecalToSurfaceData(float4 positionCS, inout SurfaceData surfaceData, inout InputData inputData)
{
#ifdef _SPECULAR_SETUP
    half metallic = 0;
    ApplyDecal(positionCS,
        surfaceData.albedo,
        surfaceData.specular,
        inputData.normalWS,
        metallic,
        surfaceData.occlusion,
        surfaceData.smoothness);
#else
    half3 specular = 0;
    ApplyDecal(positionCS,
        surfaceData.albedo,
        specular,
        inputData.normalWS,
        surfaceData.metallic,
        surfaceData.occlusion,
        surfaceData.smoothness);
#endif
}
#endif // UNIVERSAL_DBUFFER_INCLUDED
