# Inject a render pass using a Scriptable Renderer Feature

Use the `ScriptableRenderFeature` API to insert a [Scriptable Render Pass](../../renderer-features/scriptable-render-passes.md) into the Universal Render Pipeline (URP) frame rendering loop. 

Follow these steps:

1. Create a new C# script.

2. Replace the code with a class that inherits from the `ScriptableRendererFeature` class.

    ```C#
    using UnityEngine;
    using UnityEngine.Rendering.Universal;

    public class MyRendererFeature : ScriptableRendererFeature
    {
    }
    ```

3. In the class, override the `Create` method. For example:

    ```C#
    public override void Create()
    {
    }
    ```

    URP calls the `Create` methods on the following events:

    - When the Scriptable Renderer Feature loads the first time.
    - When you enable or disable the Scriptable Renderer Feature.
    - When you change a property in the **Inspector** window of the Renderer Feature.


4. In the `Create` method, create an instance of your Scriptable Render Pass, and inject it into the renderer.

    For example, if you have a Scriptable Render Pass called `RedTintRenderPass`:

    ```c#
    // Define an instance of the Scriptable Render Pass
    private RedTintRenderPass redTintRenderPass;

    public override void Create()
    {
        // Create an instance of the Scriptable Render Pass
        redTintRenderPass = new RedTintRenderPass();

        // Inject the render pass after rendering the skybox
        redTintRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }
    ```

5. Override the `AddRenderPasses` method.

    ```C#
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
    }
    ```

    URP calls the `AddRenderPasses` method every frame, once for each camera.

6. Use the `EnqueuePass` API to inject the Scriptable Render Pass into the frame rendering loop.

    ```c#
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(redTintRenderPass);
    }
    ```

You can now add the Scriptable Renderer Feature to the active URP asset. Refer to [How to add a Renderer Feature to a Renderer](../../urp-renderer-feature-how-to-add.md) for more information.

## Example

The following is the complete example code of a Scriptable Renderer Feature, using a Scriptable Render Pass called `RedTintRenderPass`.

```C#
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class MyRendererFeature : ScriptableRendererFeature
{
    private RedTintRenderPass redTintRenderPass;

    public override void Create()
    {
        redTintRenderPass = new RedTintRenderPass();
        redTintRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(redTintRenderPass);
    }
}
```
