#ifndef UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED
#define UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED

// ----------------------------------------------------------------------------
// Macros that override the register local for constant buffers (for ray tracing mainly)
// ----------------------------------------------------------------------------
// Global Input Resources - t registers. Unity supports a maximum of 64 global input resources (compute buffers, textures, acceleration structure).
#define RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER             t0
#define RAY_TRACING_LIGHT_CLUSTER_REGISTER                      t1
#define RAY_TRACING_LIGHT_DATA_REGISTER                         t3
#define RAY_TRACING_ENV_LIGHT_DATA_REGISTER                     t4

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.cs.hlsl"

#endif // UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED
