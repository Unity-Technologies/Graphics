#ifdef SENSORSDK_ENABLE_LIDAR
    // SensorSDK support: in Lidar mode, an alternate computation is used, implemented in a separate "pass"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassSensorLidar.hlsl"
#else
    // Regular path tracing, using a forward integrator with multiple importance sampling
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/PathTracing/Shaders/PathTracingForwardIntegrator.hlsl"
#endif
