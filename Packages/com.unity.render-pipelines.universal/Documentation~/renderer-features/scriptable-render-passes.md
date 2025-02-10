# Scriptable Render Passes

Use the `ScriptableRenderPass` API to write a custom render pass. You can then inject the pass into the Universal Render Pipeline (URP) frame rendering loop using the `RenderPipelineManager` API or a Scriptable Renderer Feature.

|Page|Description|
|-|-|
|[Introduction to Scriptable Render Passes](intro-to-scriptable-render-passes.md)|What a Scriptable Render Pass is, and how you can inject it into a scene.|
|[Write a Scriptable Render Pass](write-a-scriptable-render-pass.md)|An example of a `ScriptableRenderPass` instance that uses `Blit` to create a red tint effect.|
|[Inject a pass via scripting](../customize/inject-render-pass-via-script.md)|Use the `RenderPipelineManager` API to inject a render pass, without using a Scriptable Renderer Feature.|
|[Restrict a render pass to a scene area](../customize/restrict-render-pass-scene-area.md) | Enable a custom rendering effect only if the camera is inside a volume in a scene. |

## Additional resources

- [Inject a pass using a Scriptable Renderer Feature](scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md)