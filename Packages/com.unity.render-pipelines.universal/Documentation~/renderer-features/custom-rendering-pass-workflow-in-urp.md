# Custom render pass workflow in URP

A custom render pass is a way to change how the Universal Render Pipeline (URP) renders a scene or the objects within a scene. A custom render pass contains your own rendering code, which you add to the rendering pipeline at an injection point.

To add a custom render pass, complete the following tasks:

- [Create the code](#create-code) for a custom render pass using the Scriptable Render Pass API.
- [Inject the custom render pass](#inject-pass) using the `RenderPipelineManager` API, or by [creating a Scriptable Renderer Feature](#create-srf) that you add to the URP Renderer.

## <a name="create-code"></a>Create the code for a custom render pass

Use the `ScriptableRenderPass` to create the code for a custom render pass.

Refer to [Write a Scriptable Render Pass](write-a-scriptable-render-pass.md) for more information.

## <a name="inject-pass"></a>Inject the custom render pass using the RenderPipelineManager API

Unity raises a [beginCameraRendering](https://docs.unity3d.com/ScriptReference/Rendering.RenderPipelineManager-beginCameraRendering.html) event before it renders each active Camera in every frame. You can subscribe a method to this event, to execute your custom render pass before Unity renders the Camera.

Refer to [Inject a render pass via scripting](../customize/inject-render-pass-via-script.md) for more information.

## <a name="create-srf"></a>Create a Scriptable Renderer Feature

Scriptable Renderer Features control when and how the Scriptable Render Passes apply to a particular renderer or camera, and can also manage multiple Scriptable Render Passes at once.

To create a Scriptable Renderer Feature, you do the following:

* Create a Scriptable Renderer Feature using the API.
* Add the Scriptable Renderer Feature to the Universal Renderer asset, so it's included in the rendering pipeline.
* Enqueue your custom render pass in the Scriptable Renderer Feature.

Refer to [Inject a pass using a Scriptable Renderer Feature](scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md) for more information.
