# Release your ray tracing objects

You must free up the memory your ray tracing objects use after you finish with them.

Dispose of the following:

- Scratch buffers. Use [`GraphicsBuffer.Dispose`](xref:UnityEngine.GraphicsBuffer.Dispose()) after you've executed the final command buffer.
- Ray tracing acceleration structures. Use [`IRayTracingAccelStruct.Dispose`](xref:UnityEngine.Rendering.UnifiedRayTracing.IRayTracingAccelStruct.Dispose()) after you've executed the final command buffer.
- Ray tracing context. Use [`RayTracingContext.Dispose`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingContext.Dispose()) after you dispose of all its acceleration structures.

**Note**: Unity automatically disposes of [`RayTracingResources`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingResources) and [`IRayTracingShader`](xref:UnityEngine.Rendering.UnifiedRayTracing.IRayTracingShader).

For example:

```C# 
RayTracingContext rtContext = new RayTracingContext(rtResources);
IRayTracingShader rtShader = rtContext.LoadRayTracingShader("Assets/yourShader.urtshader");
IRayTracingAccelStruct rtAccelStruct = rtContext.CreateAccelerationStructure(new AccelerationStructureOptions());
GraphicsBuffer rtScratchBuffer = RayTracingHelper.CreateScratchBufferForBuild(rtAccelStruct);

// ... create and dispatch your ray tracing command buffers ...

rtScratchBuffer.Dispose();
rtAccelStruct.Dispose();
rtContext.Dispose();
```
