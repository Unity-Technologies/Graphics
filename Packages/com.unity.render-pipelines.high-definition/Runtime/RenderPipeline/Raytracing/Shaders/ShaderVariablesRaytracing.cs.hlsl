//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESRAYTRACING_CS_HLSL
#define SHADERVARIABLESRAYTRACING_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesRaytracing
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesRaytracing, b3)
    float _RayTracingPadding0;
    float _RaytracingRayMaxLength;
    int _RaytracingNumSamples;
    int _RaytracingSampleIndex;
    float _RaytracingIntensityClamp;
    int _RayCountEnabled;
    int _RaytracingPreExposition;
    float _RaytracingCameraNearPlane;
    float _RaytracingPixelSpreadAngle;
    float _RaytracingReflectionMinSmoothness;
    float _RaytracingReflectionSmoothnessFadeStart;
    int _RaytracingMinRecursion;
    int _RaytracingMaxRecursion;
    int _RayTracingDiffuseLightingOnly;
    float _DirectionalShadowFallbackIntensity;
    float _RayTracingLodBias;
    int _RayTracingRayMissFallbackHierarchy;
    int _RayTracingLastBounceFallbackHierarchy;
    int _RayTracingClampingFlag;
    float _RayTracingAmbientProbeDimmer;
    int _RayTracingAPVRayMiss;
    float _RayTracingRayBias;
    float _RayTracingDistantRayBias;
    int _PaddingRT0;
CBUFFER_END


#endif
