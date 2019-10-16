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
real4 GetRawUnitySHAr() { return unity_SHAr; }
real4 GetRawUnitySHAg() { return unity_SHAg; }
real4 GetRawUnitySHAb() { return unity_SHAb; }
real4 GetRawUnitySHBr() { return unity_SHBr; }
real4 GetRawUnitySHBg() { return unity_SHBg; }
real4 GetRawUnitySHBb() { return unity_SHBb; }
real4 GetRawUnitySHC()  { return unity_SHC; }

// Use getters for everything else
#define unity_DeltaTime            Use_GetRawUnityDeltaTime_instead_of_unity_DeltaTime
#define unity_WorldTransformParams Use_GetRawUnityWorldTransformParams_instead_of_unity_WorldTransformParams // w is usually 1.0, or -1.0 for odd-negative scale transforms
#define unity_FogColor             Use_GetRawUnityFogColor_instead_of_unity_FogColor
#define unity_LightData            Use_GetRawUnityLightData_instead_of_unity_LightData
#define unity_LightIndices         Use_GetRawUnityLightIndices_instead_of_unity_LightIndices
#define unity_SHAr                 Use_GetRawUnitySHAr_instead_of_unity_SHAr
#define unity_SHAg                 Use_GetRawUnitySHAg_instead_of_unity_SHAg
#define unity_SHAb                 Use_GetRawUnitySHAb_instead_of_unity_SHAb
#define unity_SHBr                 Use_GetRawUnitySHBr_instead_of_unity_SHBr
#define unity_SHBg                 Use_GetRawUnitySHBg_instead_of_unity_SHBg
#define unity_SHBb                 Use_GetRawUnitySHBb_instead_of_unity_SHBb
#define unity_SHC                  Use_GetRawUnitySHC_instead_of_unity_SHC

#endif

#endif // UNITY_SHADER_VARIABLES_URP_BUILTIN_VARS_IMPL_INCLUDED
