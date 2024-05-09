## Compatibility Mode

If you enable **Compatibility Mode (Render Graph Disabled)** in URP graphics settings, you can write a Scriptable Render Pass without using the [render graph API](render-graph.md). The setting is in **Project Settings** > **Graphics** > **Pipeline Specific Settings** > **URP** > **Render Graph**.

> [!NOTE]
> Unity no longer develops or improves the rendering path that doesn't use the [render graph API](render-graph.md). Use the render graph API instead when developing new graphics features.

|Page|Description|
|-|-|
|[Write a Scriptable Render Pass in Compatibility Mode](renderer-features/write-a-scriptable-render-pass.md)|An example of creating a Scriptable Render Pass in Compatibility Mode.|
|[Example of a complete Scriptable Renderer Feature in Compatibility Mode](renderer-features/create-custom-renderer-feature-compatibility-mode.md)|An example of a complete Scriptable Renderer Feature in Compatibility Mode.|
|[Scriptable Render Pass API reference](renderer-features/scriptable-renderer-features/scriptable-render-pass-reference.md)|Reference for the Scriptable Render Pass API.|
|[Perform a full screen blit in URP in Compatibility mode](renderer-features/how-to-fullscreen-blit.md)|An example that describes how to create a custom Renderer Feature that performs a full screen blit.|
