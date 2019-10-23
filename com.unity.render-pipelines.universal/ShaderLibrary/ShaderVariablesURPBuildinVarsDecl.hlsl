#if !defined(UNITY_SHADER_VARIABLES_MATRIX_DEFS_URP_INCLUDED)
#error Please include ShaderVariablesMatrixDefsURP 
#endif

#ifndef UNITY_SHADER_VARIABLES_URP_BUILTIN_VARS_DECL_INCLUDED
#define UNITY_SHADER_VARIABLES_URP_BUILTIN_VARS_DECL_INCLUDED
// Vars here are from builtin renderer. They will be replaced by SRP driven shader vars.
// TODO: replace vars in this file with SRP solution
#define BUILDIN_unity_ObjectToWorld        unity_ObjectToWorld
#define BUILDIN_unity_WorldToObject        unity_WorldToObject
#define BUILDIN_unity_DeltaTime            unity_DeltaTime
#define BUILDIN_unity_WorldTransformParams unity_WorldTransformParams // w is usually 1.0, or -1.0 for odd-negative scale transforms
#define BUILDIN_unity_FogColor             unity_FogColor
#define BUILDIN_unity_LightData            unity_LightData
#define BUILDIN_unity_LightIndices         unity_LightIndices
#define BUILDIN_unity_SHAr                 unity_SHAr
#define BUILDIN_unity_SHAg                 unity_SHAg
#define BUILDIN_unity_SHAb                 unity_SHAb
#define BUILDIN_unity_SHBr                 unity_SHBr
#define BUILDIN_unity_SHBg                 unity_SHBg
#define BUILDIN_unity_SHBb                 unity_SHBb
#define BUILDIN_unity_SHC                  unity_SHC;

//XRTODO: Add the following to BuildinVarsImpl.hlsl to prevent people from using them
#define BUILDIN_unity_SpecCube0            unity_SpecCube0;
#define BUILDIN_samplerunity_SpecCube0     samplerunity_SpecCube0
#define BUILDIN_unity_SpecCube0_HDR        unity_SpecCube0_HDR
#define BUILDIN_unity_LightmapST           unity_LightmapST
#define BUILDIN_unity_DynamicLightmapST    unity_DynamicLightmapST
#define BUILDIN_unity_LODFade              unity_LODFade
#define BUILDIN_unity_FogParams            unity_FogParams
#define BUILDIN_unity_ProbesOcclusion      unity_ProbesOcclusion
#define BUILDIN_unity_Lightmap             unity_Lightmap
#define BUILDIN_samplerunity_Lightmap      samplerunity_Lightmap

#define BUILDIN_unity_CameraInvProjection  unity_CameraInvProjection
#define BUILDIN_unity_CameraToWorld        unity_CameraToWorld
#define BUILDIN_unity_StereoEyeIndex       unity_StereoEyeIndex
#endif // UNITY_SHADER_VARIABLES_URP_BUILTIN_VARS_DECL_INCLUDED
