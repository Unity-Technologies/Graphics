#ifndef UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED
#define UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED

// ----------------------------------------------------------------------------
// Macros that override the register local for constant buffers (for ray tracing mainly)
// ----------------------------------------------------------------------------
#if (SHADER_STAGE_RAY_TRACING && UNITY_RAY_TRACING_GLOBAL_RESOURCES)
#define GLOBAL_RESOURCE(type, name, reg) type name : register(reg, space1);
#define GLOBAL_CBUFFER_START(name, reg) cbuffer name : register(reg, space1) {
#else
#define GLOBAL_RESOURCE(type, name, reg) type name;
#define GLOBAL_CBUFFER_START(name, reg) CBUFFER_START(name)
#endif

// Global Input Resources - t registers. Unity supports a maximum of 64 global input resources (compute buffers, textures, acceleration structure).
#define RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER             t0
#define RAY_TRACING_LIGHT_CLUSTER_REGISTER                      t1
#define RAY_TRACING_LIGHT_DATA_REGISTER                         t3
#define RAY_TRACING_ENV_LIGHT_DATA_REGISTER                     t4

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.cs.hlsl"

//#ifdef UNITY_GPU_DRIVEN_PIPELINE
//#define UNITY_DEFINE_GPU_DRIVEN_PROP(Type, Name) Type Name; uint Name##_GPU_Driven_Index;   
//#define UNITY_GPU_DRIVEN_PROP_ACCESS1(Name, Buffer, Offset)  Buffer.Load(Name##_GPU_Driven_Index + Offset)
//#define UNITY_GPU_DRIVEN_PROP_ACCESS2(Name, Buffer, Offset)  Buffer.Load2(Name##_GPU_Driven_Index + Offset)
//#define UNITY_GPU_DRIVEN_PROP_ACCESS3(Name, Buffer, Offset)  Buffer.Load3(Name##_GPU_Driven_Index + Offset)
//#define UNITY_GPU_DRIVEN_PROP_ACCESS4(Name, Buffer, Offset)  Buffer.Load4(Name##_GPU_Driven_Index + Offset)
//#else
#define UNITY_DEFINE_GPU_DRIVEN_PROP(Type, Name) Type Name;  
#define UNITY_GPU_DRIVEN_PROP_ACCESS1(Name, Buffer, Offset) Name
#define UNITY_GPU_DRIVEN_PROP_ACCESS2(Name, Buffer, Offset) Name
#define UNITY_GPU_DRIVEN_PROP_ACCESS3(Name, Buffer, Offset) Name
#define UNITY_GPU_DRIVEN_PROP_ACCESS4(Name, Buffer, Offset) Name
//#endif

#endif // UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED
