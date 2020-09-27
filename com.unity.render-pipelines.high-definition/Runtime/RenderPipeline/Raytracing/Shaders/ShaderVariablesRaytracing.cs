namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)ConstantRegister.RayTracing)]
    unsafe struct ShaderVariablesRaytracing
    {
        // Global ray bias used for all trace rays
        public float _RaytracingRayBias;
        // Maximal ray length for trace ray (in case an other one does not override it)
        public float _RaytracingRayMaxLength;
        // Number of samples that will be used to evaluate an effect
        public int _RaytracingNumSamples;
        // Index of the current sample
        public int _RaytracingSampleIndex;
        // Value used to clamp the intensity of the signal to reduce the signal/noise ratio
        public float _RaytracingIntensityClamp;
        // Flag that tracks if ray counting is enabled
        public int _RayCountEnabled;
        // Flag that tracks if a ray traced signal should be pre-exposed
        public int _RaytracingPreExposition;
        // Near plane distance of the camera used for ray tracing
        public float _RaytracingCameraNearPlane;
        // Angle of a pixel (used for texture filtering)
        public float _RaytracingPixelSpreadAngle;
        // Ray traced reflection Data
        public float _RaytracingReflectionMinSmoothness;
        public float _RaytracingReflectionSmoothnessFadeStart;
        public int _RaytracingIncludeSky;
        // Path tracing parameters
        public int _RaytracingMinRecursion;
        public int _RaytracingMaxRecursion;
        // Ray traced indirect diffuse data
        public int _RayTracingDiffuseLightingOnly;
    }
}
