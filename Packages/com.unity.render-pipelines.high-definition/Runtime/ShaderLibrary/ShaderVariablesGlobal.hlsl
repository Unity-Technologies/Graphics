#ifndef UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED
#define UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED

// -------------------------------------------------------------------------------------------------------------
// Global Input Resources - t registers used in Ray Tracing.
// Unity supports a maximum of 64 global input resources (textures, buffers, acceleration structure).
// -------------------------------------------------------------------------------------------------------------
#define RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER             t0
#define RAY_TRACING_LIGHT_CLUSTER_REGISTER                      t1
#define RAY_TRACING_CACHED_AREA_LIGHT_SHADOWMAP_ATLAS_REGISTER  t2
#define RAY_TRACING_CACHED_SHADOWMAP_ATLAS_REGISTER             t3
#define RAY_TRACING_SHADOWMAP_AREA_ATLAS_REGISTER               t4
#define RAY_TRACING_SHADOWMAP_ATLAS_REGISTER                    t5
#define RAY_TRACING_SHADOWMAP_CASCADE_ATLAS_REGISTER            t6
#define RAY_TRACING_COOKIE_ATLAS_REGISTER                       t7
#define RAY_TRACING_SKY_TEXTURE_REGISTER                        t8
#define RAY_TRACING_REFLECTION_ATLAS_REGISTER                   t9
#define RAY_TRACING_DIRECTIONAL_LIGHT_DATAS_REGISTER            t10
#define RAY_TRACING_HD_SHADOW_DATAS_REGISTER                    t11
#define RAY_TRACING_HD_DIRECTIONAL_SHADOW_DATA_REGISTER         t12
#define RAY_TRACING_AMBIENT_PROBE_DATA_REGISTER                 t13
#define RAY_TRACING_LIGHT_DATAS_REGISTER                        t14
#define RAY_TRACING_ENV_LIGHT_DATAS_REGISTER                    t15
#define RAY_TRACING_WORLD_LIGHT_DATAS_REGISTER                  t16
#define RAY_TRACING_WORLD_ENV_LIGHT_DATAS_REGISTER              t17
#define RAY_TRACING_VOLUMETRIC_CLOUDS_SHADOW_REGISTER           t18

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.cs.hlsl"

#endif // UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED
