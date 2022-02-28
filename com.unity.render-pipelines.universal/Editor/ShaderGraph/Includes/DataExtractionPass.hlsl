#ifndef SG_DATA_EXTRACTION_PASS_INCLUDED
#define SG_DATA_EXTRACTION_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DataExtraction.hlsl"

void InitializeInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

    #ifdef _NORMALMAP
        // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
        float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
        float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);

        inputData.tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
        #if _NORMAL_DROPOFF_TS
            inputData.normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, inputData.tangentToWorld);
        #elif _NORMAL_DROPOFF_OS
            inputData.normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
        #elif _NORMAL_DROPOFF_WS
            inputData.normalWS = surfaceDescription.NormalWS;
        #endif
    #else
        inputData.normalWS = input.normalWS;
    #endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV.xy, input.sh, inputData.normalWS);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.sh, inputData.normalWS);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);

    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamicLightmapUV.xy;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.sh;
    #endif
    #endif
}

PackedVaryings vert(
    Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);

/*  Not yet supported.
    if (UNITY_DataExtraction_Space == 0)
        packedOutput.positionCS = float4(uv0, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 1)
        packedOutput.positionCS = float4(uv1, 0.0F,  1.0f);
    else if (UNITY_DataExtraction_Space == 2)
        packedOutput.positionCS = float4(uv2, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 3)
        packedOutput.positionCS = float4(uv3, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 4)
        packedOutput.positionCS = float4(uv4, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 5)
        packedOutput.positionCS = float4(uv5, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 6)
        packedOutput.positionCS = float4(uv6, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 7)
        packedOutput.positionCS = float4(uv7, 0.0F, 1.0f);
*/
    return packedOutput;
}

half4 frag(PackedVaryings packedInput, float4 positionCS : SV_POSITION) : SV_TARGET
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

    InputData inputData;
    InitializeInputData(unpacked, surfaceDescription, inputData);

    ExtractionInputs extraction = (ExtractionInputs)0;
    extraction.vertexNormalWS = unpacked.normalWS;
    extraction.pixelNormalWS = inputData.normalWS;
    extraction.positionWS = inputData.positionWS;
    extraction.deviceDepth = positionCS.z;
    #if 0
    extraction.baseColor = surfaceDescription.BaseColor;
    extraction.alpha = alpha;
    #ifdef _SPECULAR_SETUP
    extraction.specular = surfaceDescription.Specular;
    #else
    extraction.metallic = surfaceDescription.Metallic;
    #endif
    extraction.smoothness = surfaceDescription.Smoothness;
    extraction.occlusion = surfaceDescription.Occlusion;
    extraction.emission = surfaceDescription.Emission.xyz;
    #endif

    // half precision will not preserve things like IDs which require exact results.
    return half4(OutputExtraction(extraction));
}

#endif
