#ifndef UNITY_SHADER_VARIABLES_RAY_TRACING_LIGHT_LOOP_INCLUDED
#define UNITY_SHADER_VARIABLES_RAY_TRACING_LIGHT_LOOP_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.cs.hlsl"

#define CELL_META_DATA_SIZE 5
// Indices into metadata 
#define CELL_META_DATA_TOTAL_INDEX 0
#define CELL_META_DATA_PUNCTUAL_END_INDEX 1
#define CELL_META_DATA_AREA_END_INDEX 2
#define CELL_META_DATA_ENV_END_INDEX 3
#define CELL_META_DATA_DECAL_END_INDEX 4


GLOBAL_RESOURCE(StructuredBuffer<uint>, _RaytracingLightCluster, RAY_TRACING_LIGHT_CLUSTER_REGISTER);
GLOBAL_RESOURCE(StructuredBuffer<LightData>, _LightDatasRT, RAY_TRACING_LIGHT_DATA_REGISTER);
GLOBAL_RESOURCE(StructuredBuffer<EnvLightData>, _EnvLightDatasRT, RAY_TRACING_ENV_LIGHT_DATA_REGISTER);

#endif // UNITY_SHADER_VARIABLES_RAY_TRACING_LIGHT_LOOP_INCLUDED
