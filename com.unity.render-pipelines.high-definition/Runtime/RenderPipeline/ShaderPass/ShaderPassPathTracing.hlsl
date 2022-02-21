#ifdef SENSORSDK_ENABLE_LIDAR
    // Custom Lidar integrator, implemented in the SensorSDK package
    #include "Packages/com.unity.sensorsdk/Runtime/Sensors/PathTracing/SensorIntegrator.hlsl"
#else
    // Regular path tracing, using a forward integrator with multiple importance sampling
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingIntegrator.hlsl"
#endif
