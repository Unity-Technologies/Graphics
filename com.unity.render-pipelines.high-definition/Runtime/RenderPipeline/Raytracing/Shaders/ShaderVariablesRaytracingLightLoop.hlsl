#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/RayTracingGlobalShaderVariables.hlsl"

RAY_TRACING_GLOBAL_RESOURCE(StructuredBuffer<uint>, _RaytracingLightCluster, RAY_TRACING_LIGHT_CLUSTER_REGISTER);
RAY_TRACING_GLOBAL_RESOURCE(StructuredBuffer<LightData>, _LightDatasRT, RAY_TRACING_LIGHT_DATA_REGISTER);
RAY_TRACING_GLOBAL_RESOURCE(StructuredBuffer<EnvLightData>, _EnvLightDatasRT, RAY_TRACING_ENV_LIGHT_DATA_REGISTER);

RAY_TRACING_GLOBAL_CBUFFER_START(UnityRayTracingLightLoop, UNITY_RAY_TRACING_LIGHT_LOOP_CBUFFER_REGISTER)
    uint                                        _LightPerCellCount;
    float3                                      _MinClusterPos;
    float3                                      _MaxClusterPos;
    uint                                        _PunctualLightCountRT;
    uint                                        _AreaLightCountRT;
    uint                                        _EnvLightCountRT;
CBUFFER_END
