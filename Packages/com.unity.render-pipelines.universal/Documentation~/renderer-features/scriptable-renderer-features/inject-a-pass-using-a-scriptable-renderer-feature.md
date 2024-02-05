# Inject a pass using a Scriptable Renderer Feature

This section describes how to create a [Scriptable Renderer Feature](intro-to-scriptable-renderer-features.md) for a URP Renderer. A Scriptable Renderer Feature enqueues a `ScriptableRenderPass` instance every frame.

You need to [write a Scriptable Render Pass](../write-a-scriptable-render-pass.md) first.

This walkthrough contains the following sections:

* [Create a scriptable Renderer Feature](#scriptable-renderer-feature)
* [Add the Renderer Feature to the the Universal Renderer asset](#add-renderer-feature-to-asset)
* [Enqueue the render pass in the custom renderer feature](#enqueue-the-render-pass-in-the-custom-renderer-feature)
* [Complete code for the scripts in this example](#code-renderer-feature)

## <a name="scriptable-renderer-feature"></a>Create a scriptable Renderer Feature

1. Create a new C# script and name it `MyRendererFeature.cs`.

2. In the script, remove the code that Unity inserted in the `MyRendererFeature` class.

3. Add the following `using` directive:

    ```C#
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;
    ```

3. Create the `MyRendererFeature` class that inherits from the **ScriptableRendererFeature** class.

    ```C#
    public class MyRendererFeature : ScriptableRendererFeature    
    ```

4. In the `MyRendererFeature` class, implement the following methods:

    * `Create`: Unity calls this method on the following events:

        * When the Renderer Feature loads the first time.

        * When you enable or disable the Renderer Feature.

        * When you change a property in the inspector of the Renderer Feature.

    * `AddRenderPasses`: Unity calls this method every frame, once for each camera. This method lets you inject `ScriptableRenderPass` instances into the scriptable Renderer.

Now you have the custom `MyRendererFeature` Renderer Feature with its main methods.

Below is the complete code for this step.

```C#
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class MyRendererFeature : ScriptableRendererFeature
{
    public override void Create()
    {

    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {

    }
}
```

### <a name="add-renderer-feature-to-asset"></a>Add the Renderer Feature to the Universal Renderer asset

Add the Renderer Feature you created to the the Universal Renderer asset. For information on how to do this, refer to the page [How to add a Renderer Feature to a Renderer](../../urp-renderer-feature-how-to-add.md).

## <a name="enqueue-the-render-pass-in-the-custom-renderer-feature"></a>Enqueue a render pass in the custom renderer feature

In this section, you instantiate a render pass in the `Create` method of the `MyRendererFeature` class, and enqueue it in the `AddRenderPasses` method.

This section uses the example `RedTintRenderPass` Scriptable Render Pass from the [Write a Scriptable Render Pass](../write-a-scriptable-render-pass.md) page.

1. Declare the following fields:

    ```C#
    [SerializeField] private Shader shader;
    private Material material;
    private RedTintRenderPass redTintRenderPass;
    ```

1. In the `Create` method, instantiate the `RedTintRenderPass` class.

    In the method, use the `renderPassEvent` field to specify when to execute the render pass.

    ```C#
    public override void Create()
    {
        if (shader == null)
        {
            return;
        }
        material = CoreUtils.CreateEngineMaterial(shader);
        redTintRenderPass = new RedTintRenderPass(material);

        renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }
    ```

2. In the `AddRenderPasses` method, enqueue the render pass with the `EnqueuePass` method.

    ```C#
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(redTintRenderPass);
        }
    }
    ```

## <a name="code-renderer-feature"></a>Custom Renderer Feature code

Below is the complete code for the custom Renderer Feature script.

```C#
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MyRendererFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    private Material material;
    private RedTintRenderPass redTintRenderPass;

    public override void Create()
    {
        if (shader == null)
        {
            return;
        }
        material = CoreUtils.CreateEngineMaterial(shader);
        redTintRenderPass = new RedTintRenderPass(material);
        
        redTintRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(redTintRenderPass);
        }
    }
    public override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(material);
    }
}

```
