#ifndef LIGHTWEIGHT_FORWARD_LIT_PASS_INCLUDED
#define LIGHTWEIGHT_FORWARD_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//#define _DEBUG_SHOW_SHADOW_CASCADES

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

#if defined(_ADDITIONAL_LIGHTS) || defined(_DEBUG_SHOW_SHADOW_CASCADES)
    float3 positionWS               : TEXCOORD2;
#endif

#ifdef _NORMALMAP
    half4 normalWS                  : TEXCOORD3;    // xyz: normal, w: viewDir.x
    half4 tangentWS                 : TEXCOORD4;    // xyz: tangent, w: viewDir.y
    half4 bitangentWS                : TEXCOORD5;    // xyz: bitangent, w: viewDir.z
#else
    half3 normalWS                  : TEXCOORD3;
    half3 viewDirWS                 : TEXCOORD4;
#endif

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#ifdef _MAIN_LIGHT_SHADOWS
    float4 shadowCoord              : TEXCOORD7;
#endif

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(_ADDITIONAL_LIGHTS) || defined(_DEBUG_SHOW_SHADOW_CASCADES)
    inputData.positionWS = input.positionWS;
#endif

#ifdef _NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    inputData.normalWS = TransformTangentToWorld(normalTS,
        half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
#else
    half3 viewDirWS = input.viewDirWS;
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    viewDirWS = SafeNormalize(viewDirWS);

    inputData.viewDirectionWS = viewDirWS;
#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    inputData.shadowCoord = input.shadowCoord;
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

#ifdef _NORMALMAP
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = NormalizeNormalPerVertex(normalInput.normalWS);
    output.viewDirWS = viewDirWS;
#endif
    
    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(_ADDITIONAL_LIGHTS) || defined(_DEBUG_SHOW_SHADOW_CASCADES)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;

    return output;
}

#if defined(_DEBUG_SHOW_SHADOW_CASCADES)

#if 0
// TODO: Set of colors that should still provide contrast for the Color-blind
const half4 Purple = half4(156.0 / 255.0, 79.0 / 255.0, 255.0 / 255.0, 1.0); // #9C4FFF 
const half4 Red = half4(203.0 / 255.0, 48.0 / 255.0, 34.0 / 255.0, 1.0) ; // #CB3022
const half4 Green = half4(8.0 / 255.0, 215.0 / 255.0, 139.0 / 255.0, 1.0) ; // #08D78B
const half4 YellowGreen = half4(151.0 / 255.0, 209.0 / 255.0, 61.0 / 255.0, 1.0) ; // #97D13D
const half4 Blue = half4(75.0 / 255.0, 146.0 / 255.0, 243.0 / 255.0, 1.0) ; // #4B92F3
const half4 OrangeBrown = half4(219.0 / 255.0, 119.0 / 255.0, 59.0 / 255.0, 1.0) ; // #4B92F3
const half4 Gray = half4(174.0 / 255.0, 174.0 / 255.0, 174.0 / 255.0, 1.0) ; // #AEAEAE   
#endif

half4 GetShadowCascadeColor(float4 shadowCoord, float3 positionWS)
{
    Light mainLight = GetMainLight(shadowCoord);
    half cascadeIndex = ComputeCascadeIndex(positionWS);

    half4 cascadeColors[] =
    {
        half4(0.1, 0.1, 0.9, 1.0),  // blue
        half4(0.1, 0.9, 0.1, 1.0),  // green
        half4(0.9, 0.9, 0.1, 1.0),  // yellow
        half4(0.9, 0.1, 0.1, 1.0),  // red
    };

    return cascadeColors[cascadeIndex];
}

#endif

// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

#if defined(_DEBUG_SHOW_SHADOW_CASCADES)
    half4 shadowCascadeColor = GetShadowCascadeColor(inputData.shadowCoord, input.positionWS);

    // part adapted from LightweightFragmentPBR:

    BRDFData brdfData;
    InitializeBRDFData(shadowCascadeColor.rgb, 0.0, 0.0, 1.0, surfaceData.alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord);
    mainLight.color = shadowCascadeColor.rgb;
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));

    half3 debugColor = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
    debugColor += LightingPhysicallyBased(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS);

    return half4(debugColor, 1.0);
#endif

    half4 color = LightweightFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}

#endif
