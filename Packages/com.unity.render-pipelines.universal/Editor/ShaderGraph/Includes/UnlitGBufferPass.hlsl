
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

void InitializeInputData(Varyings input, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionCS = input.positionCS;
    inputData.normalWS = normalize(input.normalWS);
    inputData.positionWS = float3(0, 0, 0);
    inputData.viewDirectionWS = half3(0, 0, 1);
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

GBufferFragOutput frag(PackedVaryings packedInput)
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

    SurfaceData surfaceData = (SurfaceData)0;
    surfaceData.alpha = 1;

#if defined(_ALPHATEST_ON)
    surfaceData.alpha = AlphaDiscard(surfaceDescription.Alpha, surfaceDescription.AlphaClipThreshold);
#endif

#if defined(LOD_FADE_CROSSFADE) && USE_UNITY_CROSSFADE
    LODFadeCrossFade(unpacked.positionCS);
#endif

#if defined(_ALPHAMODULATE_ON)
    surfaceData.albedo = AlphaModulate(surfaceDescription.BaseColor, surfaceData.alpha);
#else
    surfaceData.albedo = surfaceDescription.BaseColor;
#endif

#if defined(_DBUFFER)
    ApplyDecalToBaseColor(unpacked.positionCS, surfaceData.albedo);
#endif

    InputData inputData;
    InitializeInputData(unpacked, inputData);

    #if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT)
        float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(unpacked.positionCS);
        AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
        surfaceData.albedo.rgb *= aoFactor.directAmbientOcclusion;
    #else
        surfaceData.occlusion = 1;
    #endif

    return PackGBuffersSurfaceData(surfaceData, inputData, float3(0,0,0));
}
