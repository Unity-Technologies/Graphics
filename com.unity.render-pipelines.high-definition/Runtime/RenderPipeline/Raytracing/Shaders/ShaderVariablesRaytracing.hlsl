#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/RayTracingGlobalShaderVariables.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/HDRayTracingManager.cs.hlsl"

// The target acceleration acceleration structure should only be defined for non compute shaders
#ifndef SHADER_STAGE_COMPUTE
RAY_TRACING_GLOBAL_RESOURCE(RaytracingAccelerationStructure, _RaytracingAccelerationStructure, RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER);
#endif

RAY_TRACING_GLOBAL_CBUFFER_START(UnityRayTracingGlobals, UNITY_RAY_TRACING_GLOBAL_CBUFFER_REGISTER)
    float                                   _RaytracingCameraNearPlane;
    int                                     _RaytracingMinRecursion;
    int                                     _RaytracingMaxRecursion;
CBUFFER_END

float                                   _RaytracingRayBias;
float                                   _RaytracingRayMaxLength;
int                                     _RaytracingNumSamples;
int                                     _RaytracingSampleIndex;
float                                   _RaytracingIntensityClamp;
float                                   _RaytracingReflectionMaxDistance;
float                                   _RaytracingReflectionMinSmoothness;
int                                     _RaytracingIncludeSky;
int                                     _RaytracingFrameIndex;
float                                   _RaytracingPixelSpreadAngle;
int                                     _RayCountEnabled;
uint                                    _RaytracingDiffuseRay;
int                                     _RaytracingPreExposition;

RW_TEXTURE2D_ARRAY(uint,                _RayCountTexture);
