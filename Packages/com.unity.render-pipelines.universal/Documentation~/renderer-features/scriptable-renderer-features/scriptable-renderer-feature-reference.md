# Scriptable Renderer Feature API reference

You can use the following methods within a Scriptable Renderer Feature to handle its core functions. For more information on Scriptable Renderer Feature scripting and further details on the methods listed below, refer to [ScriptableRendererFeature](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature).

| **Method** | **Description** |
| ---------- | --------------- |
| `AddRenderPasses` | Use this method to add one or more Render Passes into the rendering sequence of the renderer with the `EnqueuePass` method.<br/><br/>By default this method applies the render passes to all cameras. To change this, add logic to return early in the method when a specific camera or camera type is detected.<br/><br/>**Note**: URP calls this method once per camera when the renderer is set up, for this reason you should not create or instantiate any resources within this function. |
| `Create` | Use this method to initialize any resources the Scriptable Renderer Feature needs such as Materials and Render Pass instances. |
| `Dispose` | Use this method to clean up the resources allocated to the Scriptable Renderer Feature such as Materials. |
| `SetupRenderPasses` | Use this method to run any setup the Scriptable Render Passes require. For example, you can set the initial values of properties, or run custom setup methods from your Scriptable Render Passes.<br/><br/>If your Scriptable Renderer Feature accesses camera targets to set up its Scriptable Render Passes, do it in this method instead of in the `AddRenderPasses` method. |

## Additional resources

* [Introduction to Scriptable Renderer Features](./intro-to-scriptable-renderer-features.md)
* [Introduction to Scriptable Render Passes](intro-to-scriptable-renderer-features.md)
* [How to create a Custom Renderer Feature](../create-custom-renderer-feature.md)
