#ifndef LIGHTWEIGHT_INPUT_INCLUDED
#define LIGHTWEIGHT_INPUT_INCLUDED

#define MAX_VISIBLE_LIGHTS 16

// TODO: Graphics Emulation are breaking structured buffers for now disabling it until we have a fix
#define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0

// Must match check of use compute buffer in LightweightRenderPipeline.cs
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
CBUFFER_END

#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P   ERROR_UNITY_MATRIX_I_P_IS_NOT_DEFINED
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  _InvCameraViewProj
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)

#include "UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#endif
