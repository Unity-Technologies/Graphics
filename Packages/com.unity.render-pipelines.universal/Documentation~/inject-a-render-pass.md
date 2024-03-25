# Adding a Scriptable Render Pass to the frame rendering loop

Add the custom render pass to the Universal Render Pipeline (URP) frame rendering loop by creating a Scriptable Renderer Feature, or using the `RenderPipelineManager` API.

|Page|Description|
|-|-|
| [Scriptable Renderer Features](renderer-features/scriptable-renderer-features/scriptable-renderer-features-landing.md) | Write a class that inherits `ScriptableRendererFeature`, and use it to creates an instance of the custom render pass you created, and insert the custom render pass into the rendering pipeline. |
| [Inject a render pass via scripting](customize/inject-render-pass-via-script.md) | Use the `RenderPipelineManager` API to insert a custom render pass into the rendering pipeline. |
| [Injection points reference](customize/custom-pass-injection-points.md) | URP contains multiple injection points that let you inject render passes at different points in the frame rendering loop. |


