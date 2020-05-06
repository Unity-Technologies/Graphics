#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariablesGlobal.hlsl"
#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/HDRayTracingManager.cs.hlsl"

// The target acceleration acceleration structure should only be defined for non compute shaders
#ifndef SHADER_STAGE_COMPUTE
GLOBAL_RESOURCE(RaytracingAccelerationStructure, _RaytracingAccelerationStructure, RAY_TRACING_ACCELERATION_STRUCTURE_REGISTER);
#endif

GLOBAL_CBUFFER_START(UnityRayTracingGlobals, UNITY_RAY_TRACING_GLOBAL_CBUFFER_REGISTER)
	// Global ray bias used for all trace rays
	float _RaytracingRayBias;
	// Maximal ray length for trace ray (in case an other one does not override it)
	float _RaytracingRayMaxLength;
	// Number of samples that will be used to evaluate an effect
	int _RaytracingNumSamples;
	// Index of the current sample
	int _RaytracingSampleIndex;
	// Value used to clamp the intensity of the signal to reduce the signal/noise ratio
	float _RaytracingIntensityClamp;
	// Flag that tracks if ray counting is enabled
	int _RayCountEnabled;
	// Flag that tracks if a ray traced signal should be pre-exposed
	int _RaytracingPreExposition;
	// Near plane distance of the camera used for ray tracing
	float _RaytracingCameraNearPlane;
	// Angle of a pixel (used for texture filtering)
	float _RaytracingPixelSpreadAngle;
	// Flag that tracks if only diffuse lighting should be computed
	uint _RaytracingDiffuseRay;
	// Ray traced reflection Data
	float _RaytracingReflectionMaxDistance;
	float _RaytracingReflectionMinSmoothness;
	float _RaytracingReflectionSmoothnessFadeStart;
	int _RaytracingIncludeSky;
	// Path tracing parameters
	int _RaytracingMinRecursion;
	int _RaytracingMaxRecursion;
	// Ray traced indirect diffuse data
	int _RayTracingDiffuseLightingOnly;
CBUFFER_END

RW_TEXTURE2D_ARRAY(uint, _RayCountTexture);
