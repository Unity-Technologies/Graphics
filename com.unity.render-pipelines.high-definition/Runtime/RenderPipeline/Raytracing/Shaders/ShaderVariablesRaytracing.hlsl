#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/HDRaytracingManager.cs.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.cs.hlsl"

// The target acceleration acceleration structure should only be defined for non compute shaders
#ifndef SHADER_STAGE_COMPUTE
GLOBAL_RESOURCE(RaytracingAccelerationStructure, _RaytracingAccelerationStructure, RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER);
#endif

RW_TEXTURE2D_ARRAY(uint, _RayCountTexture);
