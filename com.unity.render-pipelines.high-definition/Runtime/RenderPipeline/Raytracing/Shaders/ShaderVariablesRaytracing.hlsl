// The target acceleration acceleration structure
RaytracingAccelerationStructure         _RaytracingAccelerationStructure;
float                                   _RaytracingRayBias;
float                                   _RaytracingRayMaxLength;
int                                     _RaytracingNumSamples;
int                                     _RaytracingMaxRecursion;
float                                   _RaytracingIntensityClamp;
float                                   _RaytracingReflectionMaxDistance;
float                                   _RaytracingReflectionMinSmoothness;
int                                     _RaytracingFrameIndex;
float                                   _RaytracingPixelSpreadAngle;
int                                     _RayCountEnabled;
RWTexture2D<uint4>                      _RayCountTexture;
