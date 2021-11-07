# How to create a custom Renderer Feature

This section describes how to create a custom Renderer Feature for a URP Renderer.

## Create the scriptable Renderer Feature

This part shows how to create a scriptable Renderer Feature and implement the methods that let you configure and inject `ScriptableRenderPass` instances into the scriptable Renderer.

1. Create a new C# script. Call the script `LensFlareRendererFeature.cs`.

2. Open the script, remove all the code from the `LensFlareRendererFeature` class that Unity created. Add the following `using` directive.

    ```C#
    using UnityEngine.Rendering.Universal;
    ```

3. The `LensFlareRendererFeature` class must inherit from the `ScriptableRendererFeature` class.

    ```C#
    public class LensFlareRendererFeature : ScriptableRendererFeature
    ```

4. The class must implement the following methods:

    * `Create()`: Unity calls this method on the following events:
    
        * When the Renderer Feature loads the first time.

        * When you enable or disable the Renderer Feature.

        * When you change a property in the inspector of the Renderer Feature.

    * `AddRenderPasses()`: Unity calls this method every frame, once for each Camera. This method lets you inject `ScriptableRenderPass` instances into the scriptable Renderer.

Now you have the Renderer Feature with its main methods.

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class LensFlareRendererFeature : ScriptableRendererFeature
{
    public override void Create()
    { }

    public override void AddRenderPasses(ScriptableRenderer renderer,
    ref RenderingData renderingData)
    { }    
}
```

## Create the scriptable Render Pass

This part shows how to create a scriptable Render Pass and and inject its instance into the scriptable Renderer.

1. Inside the `LensFlareRendererFeature` class, declare the `LensFlarePass` class that inherits from `ScriptableRenderPass`.

    ```C#
    class LensFlarePass : ScriptableRenderPass
    { }
    ```

2. In `LensFlarePass`, add the `Execute()` method.

    Unity runs the `Execute()` method every frame. In this method, you can implement your custom rendering functionality.

    ```C#
    public override void Execute(ScriptableRenderContext context,
    ref RenderingData renderingData)
    { }
    ```

## Create the Material and the configuration class for the lens flare effect

This example implements lens flares in the following way: the Renderer Feature draws lens flares as a texture on a Quad.

The implementation requires a Material and a mesh (Quad). The section shows how to create a class that lets you configure the Material and the mesh for the Renderer Feature.

TODO

1. To draw lens flares, this example uses a Material

2. In the `LensFlareRendererFeature` class, declare a private variable to instantiate TODO _lensFlarePass = new LensFlarePass(FlareSettings);
