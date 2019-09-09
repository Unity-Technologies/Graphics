#define RAYTRACING_OPAQUE_FLAG      0x0f
#define RAYTRACING_TRANSPARENT_FLAG 0xf0

// The target acceleration acceleration structure should only be defined for non compute shaders
#ifndef SHADER_STAGE_COMPUTE
RaytracingAccelerationStructure         _RaytracingAccelerationStructure;
#endif
float                                   _RaytracingRayBias;
float                                   _RaytracingRayMaxLength;
int                                     _RaytracingNumSamples;
int                                     _RaytracingSampleIndex;
int                                     _RaytracingMinRecursion;
int                                     _RaytracingMaxRecursion;
float                                   _RaytracingIntensityClamp;
float                                   _RaytracingReflectionMaxDistance;
float                                   _RaytracingReflectionMinSmoothness;
int                                     _RaytracingIncludeSky;
int                                     _RaytracingFrameIndex;
float                                   _RaytracingPixelSpreadAngle;
int                                     _RayCountEnabled;
float                                   _RaytracingCameraNearPlane;
uint                                    _RaytracingDiffuseRay;
RWTexture2D<uint4>                      _RayCountTexture;
int                                     _RaytracingPreExposition;