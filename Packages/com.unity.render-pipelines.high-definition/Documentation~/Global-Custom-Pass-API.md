# Manage a Custom Pass without a GameObject

Use the Global custom pass API to register custom passes in the render loop without a custom pass Volume.
This means you don't need to use the [Custom Pass workflow](Custom-Pass-Volume-Workflow.md) on a GameObject, modify user scenes or dynamically spawn custom pass volumes at runtime.

<a name="Custom-Pass-C# -template"></a>

## The Global Custom Pass API

The  Global Custom Pass API contains the following static functions in the [CustomPassVolume](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/api/UnityEngine.Rendering.HighDefinition.CustomPassVolume.html) component:
- `Register` adds a custom pass instance to the list of execution during the rendering. 
- `UnRegister` removes a custom pass instance from the execution list.

The following example code registers an outline custom pass in the `BeforePostProcess` injection point:


```C#
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;


#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
static class RegisterGlobalOutline
{
   static RegisterGlobalOutline() => RegisterCustomPasses();
  
   [RuntimeInitializeOnLoadMethod]
   static void RegisterCustomPasses()
   {
       var outline = new Outline
       {
           outlineColor = Color.red,
           outlineLayer = LayerMask.GetMask("Outline"),
           threshold = 0,
       };
       CustomPassVolume.RegisterGlobalCustomPass(CustomPassInjectionPoint.BeforePostProcess, outline);
   }
}
```

To register a custom pass when the editor starts, use [UnityEditor.InitializeOnLoad](https://docs.unity3d.com/ScriptReference/InitializeOnLoadAttribute.html) and [RuntimeInitializeOnLoadMethod](https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute.html). You need to do this because HDRP doesn't serialize the list of global custom pass between [C# domain reloads](https://docs.unity3d.com/Manual/DomainReloading.html).

This script doesn't call the `UnregisterGlobalCustomPass` function. This means that the custom pass is always enabled. If you need to disable a custom pass, you can disable the registered custom passes or unregister them in the UI.

You can add a single custom pass instance multiple times at a single injection point. This script calls the `Unregister` function to remove all instances of this custom pass.

## Execution order

To learn when in the render pipeline HDRP can execute custom passes, refer to [Execution order](rendering-execution-order.md).

If you assign more than one custom pass to the same injection point, HDRP executes the custom passes in priority order. HDRP excecutes high priority custom passes before the low priority ones. You can set the priority when you register a global custom pass.

For more information, see [Custom pass injection points](Custom-Pass-Injection-Points.md). 

## Good practices

Use the following methods to avoid common issues when you script a custom pass:

- Write standalone custom passes that don't rely on a chain of custom passes. This avoids a break in that chain when an external custom pass is injected in the middle of your passes.

- If the custom pass you write is only used by your system in C#, then use the [HideInInspector] attribute to prevent your custom pass from showing in the inspector when you manually add new passes to a volume.

- You can use the **Enable** property in a custom pass to toggle the effect after you register it. You don't need to unregister and register the pass every time.

- If you use a shader in your custom pass, add it to a Resources folder or reference it somewhere so that it gets included in the build.
