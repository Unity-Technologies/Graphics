# Execute your ray tracing code
This section assumes you have [a shader](create-shader.md) containing your ray tracing logic and an [acceleration structure](create-acceleration-structure.md) representing the geometry that the rays will intersect.

To execute your ray tracing shader on the GPU:
1. Build the acceleration structure.
2. Bind resources to the shader.
3. Dispatch the shader.

## Build the acceleration structure
You must build the acceleration structure after you create it and every time you modify it. For example:
```C#
IRayTracingAccelStruct rtAccelStruct = /* your acceleration structure */;
var cmd = new CommandBuffer();

// A scratch buffer is required to build the acceleration structure, this helper function allocates one with the required size.
GraphicsBuffer buildScratchBuffer = RayTracingHelper.CreateScratchBufferForBuild(rtAccelStruct);

// Build the ray tracing acceleration structure
rtAccelStruct.Build(cmd, buildScratchBuffer);
```

## Bind resources to the shader
You must bind the acceleration structure and any additional GPU resources declared in your shader, such as constants, buffers, and textures. For example:
```C#
IRayTracingShader rtShader = /* your shader */;

// Bind the acceleration structure. It is declared in the shader as: UNIFIED_RT_DECLARE_ACCEL_STRUCT(_YourAccelStruct);
rtShader.SetAccelerationStructure(cmd, "_YourAccelStruct", rtAccelStruct);

// Bind the other GPU resources (constants/uniforms, buffers and textures)
rtShader.SetIntParam(cmd, Shader.PropertyToID("_YourInteger"), 5);
```

## Dispatch the shader
To make the GPU run your ray tracing shader, call the [`Dispatch`](xref:UnityEngine.Rendering.UnifiedRayTracing.IRayTracingShader.Dispatch(UnityEngine.Rendering.CommandBuffer,UnityEngine.GraphicsBuffer,System.UInt32,System.UInt32,System.UInt32)) method. For example:
```C#
const int threadCountX = 256, threadCountY = 256, threadCountZ = 1;

// A scratch buffer is required to trace rays, this helper function allocates one with the required size.
GraphicsBuffer traceScratchBuffer = RayTracingHelper.CreateScratchBufferForTrace(rtShader, threadCountX, threadCountY, threadCountZ);

// Dispatch rays. Workgrid dimensions are supplied in threads, not workgroups
rtShader.Dispatch(cmd, traceScratchBuffer, threadCountX, threadCountY, threadCountZ);

// Execute the command buffer to effectively run all the previously registered commands.
Graphics.ExecuteCommandBuffer(cmd);
```

**Note:**  If its size satisfies the requirements for both operations, you can use the same scratch buffer for both the build and trace steps.






