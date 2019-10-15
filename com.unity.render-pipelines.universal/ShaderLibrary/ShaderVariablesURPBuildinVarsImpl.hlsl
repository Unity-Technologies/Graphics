#if !defined(UNITY_SHADER_VARIABLES_MATRIX_DEFS_URP_INCLUDED)
#error Please include ShaderVariablesMatrixDefsURP 
#endif

#ifndef UNITY_SHADER_VARIABLES_URP_BUILTIN_VARS_IMPL_INCLUDED
#define UNITY_SHADER_VARIABLES_URP_BUILTIN_VARS_IMPL_INCLUDED

//#define UNITY_ERROR_ON_BUILD_IN_VAR
// Define Getters
// Note: In order to be able to define our macro/getter to forbid usage of unity_* build-in shader vars
// We need to declare inline function.
float4x4 GetRawUnityObjectToWorld()        { return unity_ObjectToWorld; }
float4x4 GetRawUnityWorldToObject()        { return unity_WorldToObject; }
// To get instancing working, we must use UNITY_MATRIX_M / UNITY_MATRIX_I_M as UnityInstancing.hlsl redefine them
#define unity_ObjectToWorld        Use_Macro_UNITY_MATRIX_M_instead_of_unity_ObjectToWorld
#define unity_WorldToObject        Use_Macro_UNITY_MATRIX_I_M_instead_of_unity_WorldToObject

#if defined(UNITY_ERROR_ON_BUILD_IN_VAR)
float GetRawUnityDeltaTime()              { return unity_DeltaTime; }
real4 GetRawUnityWorldTransformParams()   { return unity_WorldTransformParams; }
real4 GetRawUnityFogColor()               { return unity_FogColor; }
real4 GetRawUnityLightData()              { return unity_LightData;  }
real4 GetRawUnityLightData(const int idx) { return unity_LightIndices[idx]; }

// Use getters for everything else
#define unity_DeltaTime            Use_GetRawUnityDeltaTime_instead_of_unity_DeltaTime
#define unity_WorldTransformParams Use_GetRawUnityWorldTransformParams_instead_of_unity_WorldTransformParams // w is usually 1.0, or -1.0 for odd-negative scale transforms
#define unity_FogColor             Use_GetRawUnityFogColor_instead_of_unity_FogColor
#define unity_LightData            Use_GetRawUnityLightData_instead_of_unity_LightData
#define unity_LightIndices         Use_GetRawUnityLightIndices_instead_of_unity_LightIndices
#endif

#endif // UNITY_SHADER_VARIABLES_URP_BUILTIN_VARS_IMPL_INCLUDED
