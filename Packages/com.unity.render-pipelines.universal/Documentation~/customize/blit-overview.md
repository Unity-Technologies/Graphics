# URP blit best practices

A blit operation is a process of copying a source texture to a destination texture.

This page provides an overview of different ways to perform a blit operation in URP and best practices to follow when writing custom render passes.

## The legacy CommandBuffer.Blit API

Avoid using the [CommandBuffer.Blit](https://docs.unity3d.com/2022.1/Documentation/ScriptReference/Rendering.CommandBuffer.Blit.html) API in URP projects.

The [CommandBuffer.Blit](https://docs.unity3d.com/2022.1/Documentation/ScriptReference/Rendering.CommandBuffer.Blit.html) API is the legacy API. It implicitly runs extra operations related to changing states, binding textures, and setting render targets. Those operations happen under the hood in SRP projects and are not transparent to the user.

The API has compatibility issues with the URP XR integration. Using `cmd.Blit` might implicitly enable or disable XR shader keywords, which breaks XR SPI rendering.

The [CommandBuffer.Blit](https://docs.unity3d.com/2022.1/Documentation/ScriptReference/Rendering.CommandBuffer.Blit.html) API is not compatible with `NativeRenderPass` and `RenderGraph`.

Similar considerations apply to any utilities or wrappers relying on `cmd.Blit` internally, `RenderingUtils.Blit` is one such example.

## SRP Blitter API

Use the [Blitter API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@13.1/api/UnityEngine.Rendering.Blitter.html) in URP projects. This API does not rely on legacy logic, and is compatible with XR, native Render Passes, and other SRP APIs.

## Custom full-screen blit example

The [How to perform a full screen blit in URP](../renderer-features/how-to-fullscreen-blit.md) example shows how to create a custom Renderer Feature that performs a full screen blit. The example works in XR and is compatible with SRP APIs.
