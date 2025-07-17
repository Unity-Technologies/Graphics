# Create the ray tracing context

The [`RayTracingContext`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingContext) serves as the initial API entry point, allowing the creation of all essential objects required to execute ray tracing code.

Follow these steps:
1. Load the ray tracing resources.
2. Create the context.

## Load the ray tracing resources
The [`RayTracingContext`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingContext) needs a few utility shaders that the [`RayTracingResources`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingResources) object supplies. You can load these resources in several different ways.

If your project uses SRP (Scriptable Render Pipeline), load the resources via [`RayTracingResources.LoadFromRenderPipelineResources`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingContext.LoadFromRenderPipelineResources()). This always works in the Editor.
```C#
var rtResources = new RayTracingResources();
bool result = rtResources.LoadFromRenderPipelineResources();
```
You can instruct Unity to also include the resources in Player builds:
```C#
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

#if UNITY_EDITOR
class MyURTStripping: IRenderPipelineGraphicsSettingsStripper<RayTracingRenderPipelineResources>
{
    public bool active => true;
    public bool CanRemoveSettings(RayTracingRenderPipelineResources settings) => false;
}
#endif
```

You can also load the resources via the Asset Database:
```C# 
var rtResources = new RayTracingResources();
rtResources.Load();
```
**Note:** Since the Player doesn't give access to the Asset Database, this method only works in the Editor.

To load the [`RayTracingResources`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingResources) in the Player, you can also build the **unifiedraytracing** AssetBundle. For more information about how to build and load AssetBundles, refer to [AssetBundles](xref:AssetBundlesIntro).
```C# 
var rtResources = new RayTracingResources();

// Load the AssetBundle 
var asssetBundle = AssetBundle.LoadFromFile("Assets/pathToYourBuiltAssetBundles/unifiedraytracing");

// Load the RayTracingResources
rtResources.LoadFromAssetBundle(asssetBundle);
```

## Create the context
Once the [`RayTracingResources`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingResources) are loaded, use them to create the [`RayTracingContext`](xref:UnityEngine.Rendering.UnifiedRayTracing.RayTracingContext).
```C# 
// Choose a backend
var backend = RayTracingContext.IsBackendSupported(RayTracingBackend.Hardware) ? RayTracingBackend.Hardware : RayTracingBackend.Compute;

// Create the context
var context = new RayTracingContext(backend, rtResources);
```
