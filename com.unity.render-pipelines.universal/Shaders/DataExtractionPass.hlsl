#ifndef UNIVERSAL_LIT_EXTRACTION_PASS_INCLUDED
#define UNIVERSAL_LIT_EXTRACTION_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DataExtraction.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
    float2 uv3          : TEXCOORD3;
    float2 uv4          : TEXCOORD4;
    float2 uv5          : TEXCOORD5;
    float2 uv6          : TEXCOORD6;
    float2 uv7          : TEXCOORD7;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    float3 positionWS               : TEXCOORD1;
    float3 normalWS                 : TEXCOORD3;
    float4 tangentWS                : TEXCOORD4;    // xyz: tangent, w: sign
    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

    inputData.positionWS = input.positionWS;

#ifdef _NORMALMAP
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.normalizedScreenSpaceUV = input.positionCS.xy;
}

Varyings ExtractionVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);

    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

    if (UNITY_DataExtraction_Space == 0)
        output.positionCS = float4(input.texcoord, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 1)
        output.positionCS = float4(input.uv1.xy, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 2)
        output.positionCS = float4(input.uv2, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 3)
        output.positionCS = float4(input.uv3, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 4)
        output.positionCS = float4(input.uv4, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 5)
        output.positionCS = float4(input.uv5, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 6)
        output.positionCS = float4(input.uv6, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 7)
        output.positionCS = float4(input.uv7, 0.0F, 1.0f);

    return output;
}

float4 ExtractionFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData = (SurfaceData)0;
    INITIALIZE_DATA_EXTRACTION_SURFACE_DATA(input.uv, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    #if _ALPHATEST_ON
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    ExtractionInputs extraction = (ExtractionInputs)0;
    extraction.vertexNormalWS = input.normalWS;
    extraction.pixelNormalWS = inputData.normalWS;
    extraction.positionWS = inputData.positionWS;
    extraction.deviceDepth = input.positionCS.z;

    // TODO: Implement these when DataExtraction is intended to support all material properties
    // extraction.baseColor = surfaceData.albedo;
    // extraction.alpha = OutputAlpha(UniversalFragmentPBR(inputData, surfaceData).a);
    // #ifdef _SPECULAR_SETUP
    // extraction.specular = surfaceData.specular;
    // #else
    // extraction.metallic = surfaceData.metallic;
    // #endif
    // extraction.smoothness = surfaceData.smoothness;
    // extraction.occlusion = surfaceData.occlusion;
    // extraction.emission = surfaceData.emission;

    return OutputExtraction(extraction);
}

#endif
