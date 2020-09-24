
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

void InitializeInputData(Varyings input, out InputData inputData)
{
    #if defined(_DEBUG_SHADER)
    inputData.positionWS = input.positionWS;
    inputData.normalWS = input.normalWS;
    inputData.viewDirectionWS = input.viewDirectionWS;
    #else
    inputData.positionWS = input.positionCS;
    inputData.normalWS = half3(0, 0, 1);
    inputData.viewDirectionWS = half3(0, 0, 1);
    #endif
    inputData.shadowCoord = 0;
    inputData.fogCoord = 0;
    inputData.vertexLighting = half3(0, 0, 0);
    inputData.bakedGI = half3(0, 0, 0);
    inputData.normalizedScreenSpaceUV = 0;
    inputData.shadowMask = half4(1, 1, 1, 1);

    inputData.normalTS = half3(0, 0, 1);
    #if defined(LIGHTMAP_ON)
    inputData.lightmapUV = half2(0, 0);
    #else
    inputData.vertexSH = half3(0, 0, 0);
    #endif

    #if defined(_DEBUG_SHADER)
    inputData.uv = input.texCoord1;
    #endif
}

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    #if _AlphaClip
        half alpha = surfaceDescription.Alpha;
        clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
        half alpha = surfaceDescription.Alpha;
    #else
        half alpha = 1;
    #endif

    InputData inputData = (InputData)0;
    InitializeInputData(packedInput, inputData);

    half4 finalColor = UniversalFragmentUnlit(inputData, surfaceDescription.BaseColor, alpha);

    return finalColor;
}
