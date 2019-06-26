#ifndef LIGHTWEIGHT_LIT_DEBUG_PASS_INCLUDED
#define LIGHTWEIGHT_LIT_DEBUG_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
        
#define DEBUG_UNLIT 1
#define DEBUG_DIFFUSE 2
#define DEBUG_SPECULAR 3
#define DEBUG_ALPHA 4
#define DEBUG_SMOOTHNESS 5
#define DEBUG_OCCLUSION 6
#define DEBUG_EMISSION 7
#define DEBUG_NORMAL_WORLD_SPACE 8
#define DEBUG_NORMAL_TANGENT_SPACE 9
#define DEBUG_LIGHTING_COMPLEXITY 10
int _DebugMaterialIndex;

#define DEBUG_LIGHTING_SHADOW_CASCADES 1
#define DEBUG_LIGHTING_LIGHT_ONLY 2
#define DEBUG_LIGHTING_LIGHT_DETAIL 3
#define DEBUG_LIGHTING_REFLECTIONS 4
#define DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS 5
int _DebugLightingIndex;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

#if defined(_ADDITIONAL_LIGHTS)
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

#if defined(_MAIN_LIGHT_SHADOWS)
    float4 shadowCoord              : TEXCOORD7;
#endif
#ifdef DEBUG_LIGHTING_COMPLEXITY
    float4 ndc                      : TEXCOORD8;
#endif

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(_ADDITIONAL_LIGHTS)
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
#if (defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF))
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

#if defined(_ADDITIONAL_LIGHTS)
    output.positionWS = vertexInput.positionWS;
#endif

#if (defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF))
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;
    output.ndc = output.positionCS;

    return output;
}



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
        half4(0.1, 0.1, 0.9, 1.0), // blue
        half4(0.1, 0.9, 0.1, 1.0), // green
        half4(0.9, 0.9, 0.1, 1.0), // yellow
        half4(0.9, 0.1, 0.1, 1.0), // red
    };

    return cascadeColors[cascadeIndex];
}

half3 ShadowCascadeColor(Varyings input, InputData inputData, SurfaceData surfaceData)
{
    half4 shadowCascadeColor = GetShadowCascadeColor(inputData.shadowCoord, input.positionWS);

    // part adapted from LightweightFragmentPBR:

    BRDFData brdfData;
    InitializeBRDFData(shadowCascadeColor.rgb, 0.0, 0.0, 1.0, surfaceData.alpha, brdfData);

    Light mainLight = GetMainLight(inputData.shadowCoord);
    mainLight.color = shadowCascadeColor.rgb;
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI, half4(0, 0, 0, 0));

    half3 debugColor = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.normalWS, inputData.viewDirectionWS);
    debugColor += LightingPhysicallyBased(brdfData, mainLight, inputData.normalWS, inputData.viewDirectionWS);

    return debugColor;
}

#ifdef DEBUG_LIGHTING_COMPLEXITY
sampler2D _DebugNumberTexture;
half4 LightingComplexity(Varyings input)
{
    half4 lut[5] = {
            half4(0, 1, 0, 0),
            half4(0.25, 0.75, 0, 0),
            half4(0.498, 0.5019, 0.0039, 0),
            half4(0.749, 0.247, 0, 0),
            half4(1, 0, 0, 0)
    };

    // Assume a main light and add 1 to the additional lights.
    unsigned int numLights = clamp(GetAdditionalLightsCount()+1, 0, 4);
    half4 fc = lut[numLights];

    float2 ndc = saturate((input.ndc.xy / input.ndc.w) * 0.5 + 0.5);

#if UNITY_UV_STARTS_AT_TOP
    if(_ProjectionParams.x < 0)
        ndc.y = 1.0 - ndc.y;
#endif

    const float invNumChar = 1.0 / 10.0f;
    ndc.x *= 5.0;
    ndc.y *= 15.0;
    ndc.x = fmod(ndc.x, invNumChar) + (numLights * invNumChar);

    fc *= tex2D(_DebugNumberTexture, ndc.xy);

    return fc;
}
#endif

half4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);
    if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY || _DebugLightingIndex == DEBUG_LIGHTING_LIGHT_DETAIL)
    {
        surfaceData.albedo = half3(1.0h, 1.0h, 1.0h);
        surfaceData.metallic = 0.0;
        surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
        surfaceData.smoothness = 0.0;
        surfaceData.occlusion = 0.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
    }
    if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS)
        surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
    if (_DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS)
    {
        surfaceData.albedo = half3(0.0h, 0.0h, 0.0h);
        surfaceData.smoothness = 1.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
    }
    if (_DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS)
    {
        surfaceData.albedo = half3(0.0h, 0.0h, 0.0h);
        surfaceData.metallic = 1.0;
        surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
    }

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    
    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    half4 color = half4(0.0, 0.0, 0.0, 1.0);
    if (_DebugMaterialIndex == DEBUG_UNLIT)
        color.rgb = surfaceData.albedo;
        
    if (_DebugMaterialIndex == DEBUG_DIFFUSE)
        color.rgb = brdfData.diffuse;
        
    if (_DebugMaterialIndex == DEBUG_SPECULAR)
        color.rgb = brdfData.specular;
    
    if (_DebugMaterialIndex == DEBUG_ALPHA)
        color.rgb = (1.0 - surfaceData.alpha).xxx;
    
    if (_DebugMaterialIndex == DEBUG_SMOOTHNESS)
        color.rgb = surfaceData.smoothness.xxx;
    
    if (_DebugMaterialIndex == DEBUG_OCCLUSION)
        color.rgb = surfaceData.occlusion.xxx;
    
    if (_DebugMaterialIndex == DEBUG_EMISSION)
        color.rgb = surfaceData.emission;
        
    if (_DebugMaterialIndex == DEBUG_NORMAL_WORLD_SPACE)
        color.rgb = inputData.normalWS.xyz * 0.5 + 0.5;
        
    if (_DebugMaterialIndex == DEBUG_NORMAL_TANGENT_SPACE)
        color.rgb = surfaceData.normalTS.xyz * 0.5 + 0.5;

    if (_DebugLightingIndex == DEBUG_LIGHTING_SHADOW_CASCADES)
    {
        color.rgb = ShadowCascadeColor(input, inputData, surfaceData);
        color.a = surfaceData.alpha;
    }

    if (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY
     || _DebugLightingIndex == DEBUG_LIGHTING_LIGHT_DETAIL
     || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS
     || _DebugLightingIndex == DEBUG_LIGHTING_REFLECTIONS_WITH_SMOOTHNESS)
        color = LightweightFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

    if (_DebugMaterialIndex == DEBUG_LIGHTING_COMPLEXITY)
        color = LightingComplexity(input);

    return color;
}
#endif
