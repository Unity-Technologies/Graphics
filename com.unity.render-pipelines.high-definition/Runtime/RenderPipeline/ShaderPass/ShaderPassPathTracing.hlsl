#ifdef SENSORSDK_ENABLE_LIDAR
    // When using SensorSDK in Lidar mode, an alternate computation is used implemented in a custom integrator
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/SensorLidarIntegrator.hlsl"
#else
    // Regular path tracing, using a forward integrator with multiple importance sampling
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntegrator.hlsl"
#endif
