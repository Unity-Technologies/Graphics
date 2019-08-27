#ifndef UNIVERSAL_INPUT_INCLUDED
#define UNIVERSAL_INPUT_INCLUDED

#define MAX_VISIBLE_LIGHTS_SSBO 256
#define MAX_VISIBLE_LIGHTS_UBO  32

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderTypes.cs.hlsl"

// There are some performance issues by using SSBO in mobile.
// Also some GPUs don't supports SSBO in vertex shader.
#if !defined(SHADER_API_MOBILE) && (defined(SHADER_API_METAL) || defined(SHADER_API_VULKAN) || defined(SHADER_API_PS4) || defined(SHADER_API_XBOXONE))
    #define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0
    #define MAX_VISIBLE_LIGHTS MAX_VISIBLE_LIGHTS_SSBO
// We don't use SSBO in D3D because we can't figure out without adding shader variants if platforms is D3D10.
// We don't use SSBO on Nintendo Switch as UBO path is faster.
// However here we use same limits as SSBO path. 
#elif defined(SHADER_API_D3D11) || defined(SHADER_API_SWITCH)
    #define MAX_VISIBLE_LIGHTS MAX_VISIBLE_LIGHTS_SSBO
    #define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0
// We use less limits for mobile as some mobile GPUs have small SP cache for constants
// Using more than 32 might cause spilling to main memory.
#else
    #define MAX_VISIBLE_LIGHTS MAX_VISIBLE_LIGHTS_UBO
    #define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0
#endif

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

half4 _GlossyEnvironmentColor;
half4 _SubtractiveShadowColor;

float4x4 _InvCameraViewProj;
float4 _ScaledScreenParams;

float4 _MainLightPosition;
half4 _MainLightColor;

half4 _AdditionalLightsCount;
#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
StructuredBuffer<LightData> _AdditionalLightsBuffer;
StructuredBuffer<int> _AdditionalLightsIndices;
#else
float4 _AdditionalLightsPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsOcclusionProbes[MAX_VISIBLE_LIGHTS];
#endif

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

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#endif
