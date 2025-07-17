# Write your ray tracing code
This section guides you through the process of implementing ray tracing logic in a unified ray tracing shader (`.urtshader`).

Follow these steps:
1. Include the UnifiedRayTracing API.
2. Declare the acceleration structure.
3. Define the ray generation function.
4. Define a ray.
5. Retrieve the acceleration structure.
6. Trace the ray.

## Include the UnifiedRayTracing API
At the top of your `.urtshader` add the following statement:
```HLSL 
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl"
```

## Declare the acceleration structure
Declare the acceleration structure binding with the following macro:
```HLSL 
UNIFIED_RT_DECLARE_ACCEL_STRUCT(_YourAccelStruct);
```
Ensure that the name you declare here matches the one specified in your C# code when calling [`IRayTracingShader.SetAccelerationStructure`](xref:UnityEngine.Rendering.UnifiedRayTracing.IRayTracingShader.SetAccelerationStructure(UnityEngine.Rendering.CommandBuffer,System.String,UnityEngine.Rendering.UnifiedRayTracing.IRayTracingAccelStruct)).

**Note**: You can declare and use multiple acceleration structures within the same shader.

## Define the ray generation function
This is your kernel function that will be invoked by the GPU for each thread of your dispatch workgrid. It must be defined as follows:
```HLSL 
void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{

}
```
The [`DispatchInfo`](shader-code-reference.md#struct-dispatchinfo) struct provides information about the currently invoked thread. For instance, you can query its location within the workgrid using:
```HLSL 
uint3 threadID = dispatchInfo.dispatchThreadID;
```

At this point, you have a valid `.urtshader` file. The next steps involve implementing the ray tracing logic.
## Define a ray
Use the `Ray` struct to define a ray. For example, here is a ray starting at the origin and pointing towards the positive Z-axis:
```HLSL 
UnifiedRT::Ray ray;
ray.origin = 0;
ray.direction = float3(0, 0, 1);
ray.tMin = 0;
ray.tMax = 1000.0f;
```
The `tMin` and `tMax` fields define the segment of the ray to be tested against the acceleration structure's primitives.
In mathematical terms, the ray consists of all the points defined as `P = ray.origin + t * ray.direction` where `ray.tMin ≤ t ≤ ray.tMax`.

## Retrieve the acceleration structure
This is achieved with the following macro:
```HLSL 
UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_YourAccelStruct);
```

## Trace the ray
Invoke one of the following functions that perform ray tracing: [`TraceRayClosestHit`](shader-code-reference.md#function-tracerayclosesthit) or [`TraceRayAnyHit`](shader-code-reference.md#function-tracerayanyhit).
```HLSL 
UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, 0);
```
The returned [`Hit`](shader-code-reference.md#struct-hit) structure provides geometry information about the found intersection such as the instance ID or the triangle index.
Use `hitResult.IsValid()` to check whether a hit has been found.

## Full shader code example
Here is a complete example of a unified ray tracing shader:
```HLSL 
// Include file for the UnifiedRayTracing API functions
#include "Packages/com.unity.render-pipelines.core/Runtime/UnifiedRayTracing/TraceRayAndQueryHit.hlsl"

// Use this macro to declare the acceleration structure binding
UNIFIED_RT_DECLARE_ACCEL_STRUCT(_AccelStruct);

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    UnifiedRT::Ray ray;
    ray.origin = 0;
    ray.direction = float3(0, 0, 1);
    ray.tMin = 0;
    ray.tMax = 1000.0f;
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_AccelStruct);
    UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, 0);
    if (hitResult.IsValid())
    {
        // Handle found intersection
    }
}
```
When you create a unified ray tracing shader, Unity prefills the shader with a similar code template.

## Additional resources
- [Create a unified ray tracing shader](create-shader.md)
- [Ray tracing shader code reference](shader-code-reference.md)
