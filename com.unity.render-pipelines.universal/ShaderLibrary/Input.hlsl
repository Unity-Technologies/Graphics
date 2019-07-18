#ifndef UNIVERSAL_INPUT_INCLUDED
#define UNIVERSAL_INPUT_INCLUDED

#define MAX_VISIBLE_LIGHTS 16

// TODO: Graphics Emulation are breaking structured buffers for now disabling it until we have a fix
#define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0

// Must match check of use compute buffer in UniversalRenderPipeline.cs
// GLES check here because of WebGL 1.0 support
// TODO: check performance of using StructuredBuffer on mobile as well
// #if defined(SHADER_API_MOBILE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLCORE)
// #define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0
// #else
// #define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 1
// #endif

struct InputData
{
    float3  positionWS;
    half3   normalWS;
    half3   viewDirectionWS;
    float4  shadowCoord;
    half    fogCoord;
    half3   vertexLighting;
    half3   bakedGI;
};

///////////////////////////////////////////////////////////////////////////////
//                      Constant Buffers                                     //
///////////////////////////////////////////////////////////////////////////////

CBUFFER_START(_PerFrame)
half4 _GlossyEnvironmentColor;
half4 _SubtractiveShadowColor;
CBUFFER_END

CBUFFER_START(_PerCamera)
float4x4 _InvCameraViewProj;
float4 _ScaledScreenParams;
CBUFFER_END

CBUFFER_START(_LightBuffer)
float4 _MainLightPosition;
half4 _MainLightColor;

half4 _AdditionalLightsCount;
float4 _AdditionalLightsPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsOcclusionProbes[MAX_VISIBLE_LIGHTS];
CBUFFER_END

#include "UnityInput.hlsl"

// Helper to handle camera relative space
float4x4 ApplyCameraTranslationToMatrix(float4x4 modelMatrix)
{
    // To handle camera relative rendering we substract the camera position in the model matrix
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    modelMatrix._m03_m13_m23 -= _WorldSpaceCameraPos;
#endif
    return modelMatrix;
}

float4x4 ApplyCameraTranslationToInverseMatrix(float4x4 inverseModelMatrix)
{
#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    // To handle camera relative rendering we need to apply translation before converting to object space
    float4x4 translationMatrix = { { 1.0, 0.0, 0.0, _WorldSpaceCameraPos.x },{ 0.0, 1.0, 0.0, _WorldSpaceCameraPos.y },{ 0.0, 0.0, 1.0, _WorldSpaceCameraPos.z },{ 0.0, 0.0, 0.0, 1.0 } };
    return mul(inverseModelMatrix, translationMatrix);
#else
    return inverseModelMatrix;
#endif
}

// Define Model Matrix Macro
// Note: In order to be able to define our macro to forbid usage of unity_ObjectToWorld/unity_WorldToObject
// We need to declare inline function. Using uniform directly mean they are expand with the macro
float4x4 GetRawUnityObjectToWorld() { return unity_ObjectToWorld; }
float4x4 GetRawUnityWorldToObject() { return unity_WorldToObject; }

#define UNITY_MATRIX_M     ApplyCameraTranslationToMatrix(GetRawUnityObjectToWorld())
#define UNITY_MATRIX_I_M   ApplyCameraTranslationToInverseMatrix(GetRawUnityWorldToObject())

// To get instancing working, we must use UNITY_MATRIX_M / UNITY_MATRIX_I_M as UnityInstancing.hlsl redefine them
#define unity_ObjectToWorld Use_Macro_UNITY_MATRIX_M_instead_of_unity_ObjectToWorld
#define unity_WorldToObject Use_Macro_UNITY_MATRIX_I_M_instead_of_unity_WorldToObject

#include "ShaderVariablesMatrixDefsUniversalRP.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#endif
