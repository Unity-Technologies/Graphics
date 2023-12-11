
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"

void InitializeInputData(Varyings input, out InputData inputData)
{
    inputData = (InputData)0;

    // InputData is only used for DebugDisplay purposes in Unlit, so these are not initialized.
    #if defined(DEBUG_DISPLAY)
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.normalWS = input.normalWS;
    #else
    inputData.positionWS = half3(0, 0, 0);
    inputData.normalWS = half3(0, 0, 1);
    inputData.viewDirectionWS = half3(0, 0, 1);
    #endif
    inputData.shadowCoord = 0;
    inputData.fogCoord = 0;
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.bakedGI = half3(0, 0, 0);
    inputData.normalizedScreenSpaceUV = 0;
    inputData.shadowMask = half4(1, 1, 1, 1);
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

void frag(
    PackedVaryings packedInput
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#if defined(_SURFACE_TYPE_TRANSPARENT)
    bool isTransparent = true;
#else
    bool isTransparent = false;
#endif

#if defined(_ALPHATEST_ON)
    half alpha = AlphaDiscard(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
#elif defined(_SURFACE_TYPE_TRANSPARENT)
    half alpha = surfaceDescription.Alpha;
#else
    half alpha = half(1.0);
#endif

    #if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
        LODFadeCrossFade(unpacked.positionCS);
    #endif

#if defined(_ALPHAMODULATE_ON)
    surfaceDescription.BaseColor = AlphaModulate(surfaceDescription.BaseColor, alpha);
#endif

#if defined(_DBUFFER)
    ApplyDecalToBaseColor(unpacked.positionCS, surfaceDescription.BaseColor);
#endif

    InputData inputData;
    InitializeInputData(unpacked, inputData);
    #ifdef VARYINGS_NEED_TEXCOORD0
        SETUP_DEBUG_TEXTURE_DATA(inputData, unpacked.texCoord0);
    #else
        SETUP_DEBUG_TEXTURE_DATA_NO_UV(inputData);
    #endif

    half4 finalColor = UniversalFragmentUnlit(inputData, surfaceDescription.BaseColor, alpha);
    finalColor.a = OutputAlpha(finalColor.a, isTransparent);

    #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
        float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(unpacked.positionCS);
        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
        finalColor.rgb *= aoFactor.directAmbientOcclusion;
    #endif

    outColor = finalColor;

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}
