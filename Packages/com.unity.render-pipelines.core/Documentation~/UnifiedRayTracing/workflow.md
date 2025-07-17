# Ray tracing workflow
To use the API to trace rays in Unity, follow these steps:
1. Create the ray tracing context.
2. Create an acceleration structure.
3. Create a shader.
4. Build your acceleration structure.
5. Execute your shader.

## Create the ray tracing context

The context serves as the API entry point. Create it using the following parameters:
1. `backend`: The chosen backend, which can be either `Hardware` or `Compute`.
2. `resources`: A collection of assets required by the context to function.
```C#
var rtContext = new RayTracingContext(backend, rtResources);
```

For more information, refer to [Create the ray tracing Context](create-ray-tracing-context.md).

## Create an acceleration structure

The acceleration structure is the data structure used to represent a collection of instances and geometries that are used for GPU ray tracing.
```C#
IRayTracingAccelStruct rtAccelStruct = rtContext.CreateAccelerationStructure(options);
```
After creating the structure, populate it with mesh instances using the [`AddInstance`](xref:UnityEngine.Rendering.UnifiedRayTracing.IRayTracingAccelStruct.AddInstance(UnityEngine.Rendering.UnifiedRayTracing.MeshInstanceDesc)) method.

For more information, refer to [Create an acceleration structure](create-acceleration-structure.md).

## Create a unified ray tracing shader

Depending on the chosen backend, the `RayTracingContext` runs either a `ComputeShader` or a `RayTracingShader`. The API introduces the unified ray tracing shader (`.urtshader`), which is a shader type that generates both variants for you.
```C#
IRayTracingShader rtShader = rtContext.LoadRayTracingShader("Assets/yourShader.urtshader");
```

For more information, refer to [Create a unified ray tracing shader](create-shader.md).

## Build your acceleration structure
Whenever it is modified or used for the first time, the acceleration structure needs to be built or rebuilt. This can be achieved with the following code:
```C#
rtAccelStruct.Build(cmd, buildScratchBuffer);
```

For more information, refer to [Execute your ray tracing code](execute-shader.md).

## Execute your shader
To execute your shader, call the [`Dispatch`](xref:UnityEngine.Rendering.UnifiedRayTracing.IRayTracingShader.Dispatch(UnityEngine.Rendering.CommandBuffer,UnityEngine.GraphicsBuffer,System.UInt32,System.UInt32,System.UInt32))
method of `IRayTracingShader`.
```C#
rtShader.Dispatch(cmd, traceScratchBuffer, threadCountX, threadCountY, threadCountZ);
```

For more information, refer to [Execute your ray tracing code](execute-shader.md).

