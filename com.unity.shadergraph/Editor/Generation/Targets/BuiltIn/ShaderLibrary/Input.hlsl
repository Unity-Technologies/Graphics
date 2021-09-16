#ifndef BUILTIN_INPUT_INCLUDED
#define BUILTIN_INPUT_INCLUDED

#define MAX_VISIBLE_LIGHTS_UBO  32
#define MAX_VISIBLE_LIGHTS_SSBO 256

// Keep in sync with RenderingUtils.useStructuredBuffer
#define USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA 0

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderTypes.cs.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Deprecated.hlsl"

#if defined(SHADER_API_MOBILE) && (defined(SHADER_API_GLES) || defined(SHADER_API_GLES30))
    #define MAX_VISIBLE_LIGHTS 16
#elif defined(SHADER_API_MOBILE) || (defined(SHADER_API_GLCORE) && !defined(SHADER_API_SWITCH)) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3) // Workaround because SHADER_API_GLCORE is also defined when SHADER_API_SWITCH is
    #define MAX_VISIBLE_LIGHTS 32
#else
    #define MAX_VISIBLE_LIGHTS 256
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
    float2  normalizedScreenSpaceUV;
    half4   shadowMask;
};

///////////////////////////////////////////////////////////////////////////////
//                      Constant Buffers                                     //
///////////////////////////////////////////////////////////////////////////////

half4 _GlossyEnvironmentColor;
half4 _SubtractiveShadowColor;

#define _InvCameraViewProj unity_MatrixInvVP
float4 _ScaledScreenParams;

float4 _MainLightPosition;
half4 _MainLightColor;
half4 _MainLightOcclusionProbes;

// xyz are currently unused
// w: directLightStrength
half4 _AmbientOcclusionParam;

half4 _AdditionalLightsCount;

#if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
StructuredBuffer<LightData> _AdditionalLightsBuffer;
StructuredBuffer<int> _AdditionalLightsIndices;
#else
// GLES3 causes a performance regression in some devices when using CBUFFER.
#ifndef SHADER_API_GLES3
CBUFFER_START(AdditionalLights)
#endif
float4 _AdditionalLightsPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightsOcclusionProbes[MAX_VISIBLE_LIGHTS];
#ifndef SHADER_API_GLES3
CBUFFER_END
#endif
#endif

// Duplicate defined symbols in built-in target
#ifndef BUILTIN_TARGET_API
#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P   (float4x4)0
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  (float4x4)0
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)
#define UNITY_PREV_MATRIX_M   unity_MatrixPreviousM
#define UNITY_PREV_MATRIX_I_M unity_MatrixPreviousMI
#else
// Not defined already by built-in
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_I_P   (float4x4)0
#define UNITY_MATRIX_I_VP  (float4x4)0
#define UNITY_PREV_MATRIX_M   (float4x4)0
#define UNITY_PREV_MATRIX_I_M (float4x4)0
#endif


// Note: #include order is important here.
// UnityInput.hlsl must be included before UnityInstancing.hlsl, so constant buffer
// declarations don't fail because of instancing macros.
// BuiltInDOTSInstancing.hlsl must be included after UnityInstancing.hlsl
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/BuiltInDOTSInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

#endif
