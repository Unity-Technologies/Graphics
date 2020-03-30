#ifndef UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED
#define UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED

// Global Constant Buffers - b registers. Unity supports a maximum of 16 global constant buffers.
	// Common constant buffers
#define UNITY_GLOBAL_CBUFFER_REGISTER                           b0
#define UNITY_XR_VIEW_CONSTANTS_CBUFFER_REGISTER                b1
#define UNITY_PHYSICALLY_BASED_SKY_CBUFFER_REGISTER             b2
	// Ray tracing specific constant buffers
#define UNITY_RAY_TRACING_GLOBAL_CBUFFER_REGISTER               b3
#define UNITY_RAY_TRACING_LIGHT_LOOP_CBUFFER_REGISTER           b4

// Global Input Resources - t registers. Unity supports a maximum of 64 global input resources (compute buffers, textures, acceleration structure).
#define RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER             t0
#define RAY_TRACING_LIGHT_CLUSTER_REGISTER                      t1
#define RAY_TRACING_LIGHT_DATA_REGISTER                         t3
#define RAY_TRACING_ENV_LIGHT_DATA_REGISTER                     t4

#endif // UNITY_SHADER_VARIABLES_GLOBAL_INCLUDED