# Create an acceleration structure
[`IRayTracingAccelStruct`](xref:UnityEngine.Rendering.UnifiedRayTracing.IRayTracingAccelStruct) is the data structure used to represent a collection of instances and geometries that are used for GPU ray tracing.

Create an acceleration structure with the following code:
```C# 
var options = new AccelerationStructureOptions();
IRayTracingAccelStruct rtAccelStruct = rtContext.CreateAccelerationStructure(options);
```

## Build options
[`AccelerationStructureOptions`](xref:UnityEngine.Rendering.UnifiedRayTracing.AccelerationStructureOptions) allows you to configure the build algorithm. The trade-off is as follows: A faster build results in worse ray tracing performance, and conversely, a slower build can improve ray tracing performance.

The following fields can be configured:

|Field|Description|
|-|-|
|`buildFlags`|Adjust the buildFlags to prioritize either faster construction of the acceleration structure or faster ray tracing.|
|`useCPUBuild` (Compute backend)|When set to true, Unity builds the acceleration structure on the CPU instead of the GPU. This option has no effect when using the `Hardware` backend. CPU-based builds use a more advanced algorithm, resulting in a higher-quality acceleration structure, which enhances overall ray tracing performance.|

The following example demonstrates how to get the best ray tracing performance. These options make the acceleration structure longer to build, however.
```HLSL 
var options = new AccelerationStructureOptions() {
    buildFlags = BuildFlags.PreferFastTrace,
    useCPUBuild = true
}
```

## Populate the acceleration structure
Unlike [`RayTracingAccelerationStructure`](xref:UnityEngine.Rendering.RayTracingAccelerationStructure), there is no support for automatic synchronization between the acceleration structure's instances and the scene instances. You need to add them manually, for example:
```C#
var instanceDesc = new MeshInstanceDesc(mesh, 0 /*subMeshIndex*/);
instanceDesc.localToWorldMatrix = /* desired transform */;
rtAccelStruct.AddInstance(instanceDesc);
```

To trace rays against all the Mesh-based GameObjects in your scene, add them to the [`IRayTracingAccelStruct`](xref:UnityEngine.Rendering.UnifiedRayTracing.IRayTracingAccelStruct). For example:
```C# 
var meshRenderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
foreach (var renderer in meshRenderers)
{
    var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
    int subMeshCount = mesh.subMeshCount;

    for (int i = 0; i < subMeshCount; ++i)
    {
        var instanceDesc = new MeshInstanceDesc(mesh, i);
        instanceDesc.localToWorldMatrix = renderer.transform.localToWorldMatrix;
        rtAccelStruct.AddInstance(instanceDesc);
    }
}
```
