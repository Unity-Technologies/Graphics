# Unified ray tracing shader code reference
This section presents the different functions and structs provided by the API for tracing rays in a shader.

All types are defined inside the `UnifiedRT` namespace. In your code, you need to prefix them with ```UnifiedRT::```. Alternatively, you can add ```using namespace UnifiedRT;``` after your `TraceRayAndQueryHit.hlsl` include statement.

## function TraceRayClosestHit
```HLSL 
Hit TraceRayClosestHit(DispatchInfo dispatchInfo, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags)
 ```
Searches for intersections between a ray and an acceleration structure. It returns hit information about the closest triangle encountered along the ray. 
### Parameters
|Type|Name|Description|
|-|-|-|
|[`DispatchInfo`](#struct-dispatchinfo)|*dispatchInfo*|The dispatch info. Must be the value that is passed by `RayGenExecute`.|
|`RayTracingAccelStruct`|*accelStruct*|The acceleration structure to test the ray against.|
|`uint`|*instanceMask*|The lower 8 bits of this mask are used to include geometry instances based on the instance mask that was set in `MeshInstanceDesc` for each instance.|
|[`Ray`](#struct-ray)|*ray*|Describes the ray segment that is intersected against the acceleration structure.|
|`uint`|*rayFlags*|Flags that filter out the triangles that participate in the intersection test. Can be one of the following: <ul><li>kRayFlagNone</li><li>kRayFlagCullBackFacingTriangles</li><li>kRayFlagCullFrontFacingTriangles</li> </ul>|
### Returns
[`Hit`](#struct-hit) containing geometry information about the hit triangle. When no primitive has intersected with the ray, `hit.IsValid()` returns false.

## function TraceRayAnyHit
```HLSL 
bool TraceRayAnyHit(DispatchInfo dispatchInfo, RayTracingAccelStruct accelStruct, uint instanceMask, Ray ray, uint rayFlags)
```
Searches for any intersection between a ray and an acceleration structure. The search ends as soon as a valid triangle hit is found. This function can typically be used to trace shadow rays or perform occlusion queries.  
### Parameters
|Type|Name|Description|
|-|-|-|
|[`DispatchInfo`](#struct-dispatchinfo)|*dispatchInfo*|The dispatch info. Must be the value that is passed by `RayGenExecute`.|
|`RayTracingAccelStruct`|*accelStruct*|The acceleration structure to test the ray against.|
|`uint`|*instanceMask*|The lower 8 bits of this mask are used to include geometry instances based on the instance mask that was set in `MeshInstanceDesc` for each instance.|
|[`Ray`](#struct-ray)|*ray*|Describes the ray segment that is intersected against the acceleration structure.|
|`uint`|*rayFlags*|Flags that filter out the triangles that participate in the intersection test. Can be one of the following: <ul><li>kRayFlagNone</li><li>kRayFlagCullBackFacingTriangles</li><li>kRayFlagCullFrontFacingTriangles</li> </ul>|
### Returns
A boolean that is true if any primitive was hit by the ray.

## struct Ray
Describes a ray.
The `tMin` and `tMax` fields define the segment of the ray to be tested against the acceleration structures's primitives.
Mathematically, the ray consists of all the points defined as `P = ray.origin + t * ray.direction`, where `ray.tMin ≤ t ≤ ray.tMax`.
### Fields
|Type|Name|Description|
|-|-|-|
|`float3`|*origin*|The ray's origin.|
|`float3`|*direction*|The ray's direction.|
|`float`|*tMin*|The ray's starting point.|
|`float`|*tMax*|The ray's endpoint.|

## struct DispatchInfo
Provides information about the current thread that is invoked.
### Fields
|Type|Name|Description|
|-|-|-|
|`uint3`|*dispatchThreadID*|Same semantic as `SV_DispatchThreadID`.|
|`uint`|*localThreadIndex*|Same semantic as `SV_GroupIndex`.|
|`uint3`|*dispatchDimensionsInThreads*|Total numbers of threads dispatched in the X, Y, and Z workgrid directions.|
|`uint`|*globalThreadIndex*|Global thread index that is unique within the workgrid.|

## struct Hit
Describes a Hit.
### Fields
|Type|Name|Description|
|-|-|-|
|`uint`|*instanceID*|Matches the `instanceID` supplied from C# in `MeshInstanceDesc.instanceID`.|
|`uint`|*primitiveIndex*|Index of the hit triangle in its source Mesh.|
|`float2`|*uvBarycentrics*|Barycentric coordinates of the hit triangle.|
|`float`|*hitDistance*|Defines the hit position: `hitPos = ray.origin + ray.direction * hit.hitDistance`.|
|`bool`|*isFrontFace*|Indicates whether the hit triangle is front-facing or back-facing.|
### Methods
```HLSL 
bool IsValid();
```
Returns true when a hit has been found. When a hit is invalid `hit.instanceID` is equal to `~0` and the other fields are undefined.
