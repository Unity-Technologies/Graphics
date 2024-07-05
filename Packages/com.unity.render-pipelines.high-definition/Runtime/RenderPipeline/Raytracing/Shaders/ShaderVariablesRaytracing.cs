namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    class HDLightClusterDefinitions
    {
        public static Vector3Int s_ClusterSize = new Vector3Int(64, 32, 64);
        public static int s_ClusterCellCount = s_ClusterSize.x * s_ClusterSize.y * s_ClusterSize.z;

        public static int s_CellMetaDataSize = 5;
    }

    [GenerateHLSL(needAccessors = false, generateCBuffer = true, constantRegister = (int)ConstantRegister.RayTracing)]
    unsafe struct ShaderVariablesRaytracing
    {
        public float _RayTracingPadding0;
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
        // Path tracing parameters
        public int _RaytracingMinRecursion;

        public int _RaytracingMaxRecursion;
        // Ray traced indirect diffuse data
        public int _RayTracingDiffuseLightingOnly;
        // Shadow value to be used when the point to shade is not inside of the cascades
        public float _DirectionalShadowFallbackIntensity;
        // Global bias applied to texture reading for various reasons.
        public float _RayTracingLodBias;

        // Bit mask that defines which fall back to use when a ray misses.
        public int _RayTracingRayMissFallbackHierarchy;
        // Flag that defines if we should use the ambient probe instead of the sky. Used for RTGI - performance mode.
        public int _RayTracingRayMissUseAmbientProbeAsSky;
        // Flag that defines if the sky should be used as an environment light.
        public int _RayTracingLastBounceFallbackHierarchy;
        // Flag that defines if
        public int _RayTracingClampingFlag;
        // Dimmer that allows us to nuke the ambient probe (and legacy probe as a side effect) in ray tracing effects.
        public float _RayTracingAmbientProbeDimmer;

        // Flag that defines if the APV should be used in the case of a ray miss in performance mode
        public int _RayTracingAPVRayMiss;
        // Near plane ray Bias
        public float _RayTracingRayBias;
        // Far plane ray bias
        public float _RayTracingDistantRayBias;
        // Ray Frame Index for reflection signals
        public int _RayTracingReflectionFrameIndex;
        // Layer Mask to use when sampling APV
        public uint _RaytracingAPVLayerMask;
    }
}
