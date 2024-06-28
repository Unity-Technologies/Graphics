# Scriptable Renderer Feature Reference

When working with Scriptable Renderer Features and Scriptable Render Passes there are predefined methods that you need to implement for URP to call at specific points in the pipeline.
 
The following sections summarize the common methods used to write Scriptable Renderer Features and Scriptable Render Passes:

* [Scriptable Renderer Feature Methods](#scriptable-renderer-feature-methods)
* [Scriptable Render Pass Methods](#scriptable-render-pass-methods)

## Scriptable Renderer Feature Methods

You can use the following methods within a Scriptable Renderer Feature to handle its core functions. For more information on Scriptable Renderer Feature scripting and further details on the methods listed below, refer to [ScriptableRendererFeature](xref:UnityEngine.Rendering.Universal.ScriptableRendererFeature).

| **Method** | **Description** |
| ---------- | --------------- |
| `AddRenderPasses` | Use this method to add one or more Render Passes into the rendering sequence of the renderer with the `EnqueuePass` method.<br/><br/>By default this method applies the render passes to all cameras. To change this, add logic to return early in the method when a specific camera or camera type is detected.<br/><br/>**Note**: URP calls this method once per camera when the renderer is set up, for this reason you should not create or instantiate any resources within this function. |
| `Create` | Use this method to initialize any resources the Scriptable Renderer Feature needs such as Materials and Render Pass instances. |
| `Dispose` | Use this method to clean up the resources allocated to the Scriptable Renderer Feature such as Materials. |
| `SetupRenderPasses` | Use this method to run any setup the Scriptable Render Passes require. For example, you can set the initial values of properties, or run custom setup methods from your Scriptable Render Passes.<br/><br/>If your Scriptable Renderer Feature accesses camera targets to set up its Scriptable Render Passes, do it in this method instead of in the `AddRenderPasses` method. |

## Scriptable Render Pass Methods

You can use the following methods within a Scriptable Renderer Pass to handle its core functions. For further information on Scriptable Render Pass scripting and further details on the methods listed below, refer to [ScriptableRenderPass](xref:UnityEngine.Rendering.Universal.ScriptableRenderPass).

| **Method** | **Description** |
| ---------- | --------------- |
| `Execute` | Use this method to implement the rendering logic for the Scriptable Renderer Feature.<br/><br/>**Note**: You must not call `ScriptableRenderContext.Submit` on a command buffer provided by URP. The render pipeline handles this at specific points in the pipeline. |
| `OnCameraCleanup` | Use this method to clean up any resources that were allocated during the render pass. |
| `OnCameraSetup` | Use this method to configure render targets and their clear state. You can also use it to create temporary render target textures.<br/><br/>**Note**: When this method is empty, the render pass will render to the active camera render target. |

## Additional resources

* [Introduction to Scriptable Renderer Features](./intro-to-scriptable-renderer-features.md)
* [Introduction to Scriptable Render Passes](intro-to-scriptable-renderer-features.md)
* [How to create a Custom Renderer Feature](../create-custom-renderer-feature.md)
