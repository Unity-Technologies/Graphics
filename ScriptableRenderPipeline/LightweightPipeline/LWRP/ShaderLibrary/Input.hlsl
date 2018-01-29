#ifndef LIGHTWEIGHT_INPUT_INCLUDED
#define LIGHTWEIGHT_INPUT_INCLUDED

#define MAX_VISIBLE_LIGHTS 16

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
float4 _MainLightPosition;
half4 _MainLightColor;
half4 _MainLightDistanceAttenuation;
half4 _MainLightSpotDir;
half4 _MainLightSpotAttenuation;
float4x4 _WorldToLight;

half4 _AdditionalLightCount;
float4 _AdditionalLightPosition[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightColor[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightDistanceAttenuation[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotDir[MAX_VISIBLE_LIGHTS];
half4 _AdditionalLightSpotAttenuation[MAX_VISIBLE_LIGHTS];
CBUFFER_END

#define UNITY_MATRIX_M     unity_ObjectToWorld
#define UNITY_MATRIX_I_M   unity_WorldToObject
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P   ERROR_UNITY_MATRIX_I_P_IS_NOT_DEFINED
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  ERROR_UNITY_MATRIX_I_VP_IS_NOT_DEFINED
#define UNITY_MATRIX_MV    mul(UNITY_MATRIX_V, UNITY_MATRIX_M)
#define UNITY_MATRIX_T_MV  transpose(UNITY_MATRIX_MV)
#define UNITY_MATRIX_IT_MV transpose(mul(UNITY_MATRIX_I_M, UNITY_MATRIX_I_V))
#define UNITY_MATRIX_MVP   mul(UNITY_MATRIX_VP, UNITY_MATRIX_M)

#include "InputBuiltin.hlsl"
#include "CoreRP/ShaderLibrary/UnityInstancing.hlsl"
#include "CoreFunctions.hlsl"

#endif
