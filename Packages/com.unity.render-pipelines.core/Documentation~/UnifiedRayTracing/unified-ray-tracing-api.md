# Ray tracing with the UnifiedRayTracing API
The `UnifiedRayTracing` API enables you to write ray tracing code that can execute on a wide range of GPUs. It leverages hardware ray tracing acceleration on supported GPUs, while providing a compute shader-based software fallback for those that do not.

|Section|Description|
|-|-|
|[Get started with ray tracing](get-started.md)|Learn the essential information about the API.|
|[Ray tracing workflow](workflow.md)|Create a ray tracing context, and create and execute a ray tracing shader.|
|[Create the ray tracing context](create-ray-tracing-context.md)|How to create the API entry point.|
|[Create an acceleration structure](create-acceleration-structure.md)|Create and initialize an acceleration structure describing your geometry.|
|[Create a unified ray tracing shader](create-shader.md)|Create a unified ray tracing shader file.|
|[Write your shader code](write-shader.md)|Write the ray tracing logic in your a unified ray tracing shader file.|
|[Execute your ray tracing code](execute-shader.md)|How to execute your ray tracing shader.|
|[Sample code](trace-camera-rays-full-sample.md)|Complete code example showcasing tracing rays from the scene's camera.|
|[Unified ray tracing shader code reference](shader-code-reference.md)|API reference for the unified ray tracing shader code.|

