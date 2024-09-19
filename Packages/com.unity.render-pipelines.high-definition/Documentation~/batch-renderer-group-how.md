---
uid: um-batch-renderer-group-how
---

# Introduction to the BatchRendererGroup API

BRG is the perfect tool to:

* Render DOTS [Entities](https://docs.unity3d.com/Packages/com.unity.entities@latest). For more information how Entities uses BRG, refer to [Entities Graphics Performance](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.2/manual/entities-graphics-performance.html).
* Render a large number of environment objects where using individual GameObjects would be too resource-intensive. For example, procedurally-placed plants or rocks.
* Render custom terrain patches. You can use different meshes or materials to display different levels of detail.

## Render pipeline compatibility

The following table shows which render pipelines support BRG.

| **Feature name**   | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** | **Custom SRP** | **Built-in Render Pipeline** | 
| ------------------ | ---------------------------- | ----------------------------------- | ------------------------------------------ | -------------- |
| BatchRendererGroup | Yes (1)                             | Yes (1)                                    | Yes (1)        | No                           | 

**Notes**:

1. If the project uses the SRP Batcher.

## Platform compatibility

Unity supports BRG on:

* Windows using DirectX 11
* Windows using DirectX 12
* Windows using Vulkan
* Universal Windows Platform
* Linux using Vulkan
* macOS using Metal
* iOS
* Android (Vulkan and OpenGL ES 3.x)
* PlayStation 4
* PlayStation 5
* Xbox One
* Xbox Series X and Xbox Series S
* Nintendo Switch

## How BatchRendererGroup works

To render to the screen, BatchRendererGroup (BRG) generates [draw commands](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchDrawCommand) which are a BRG-specific concept that contains everything Unity needs to efficiently create optimized, instanced draw calls.

To determine when to render the instances in a draw command, BRG uses [filter settings](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchFilterSettings). Filter settings control when to render instances themselves, but also when to render certain facets of each instance such as its shadows and motion vectors

Because the same filter settings can often apply to a large number of draw commands, BRG uses [draw ranges](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.BatchDrawRange) to apply filter settings to a range of draw commands. A draw range combines a contiguous number of draw commands with an instance of filter settings that apply to them. Draw ranges are especially useful if the filter settings determine that Unity shouldn't render the draw commands, because this makes it possible for Unity to efficiently skip rendering for every draw command in the range.

There is no restriction on which instances are in which draw calls. It's possible to render the same instance, an object with the same instance index and batchID, many times with different meshes and materials. One example where this can be useful is drawing different sub-meshes with different materials, but using the same instance indices to share properties like transform matrices between the draws.

For information on how to create a renderer with BRG, see [Creating a renderer with BatchRendererGroup](batch-renderer-group-creating-a-renderer.md).

## Technical limitations

In most cases, Unity renders a draw command as a single, platform-level, instanced draw call for each compatible [DrawRenderers](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rendering.ScriptableRenderContext.DrawRenderers) call in the Scriptable Render Pipeline. However, that isn't possible when the graphics API has a lower size limit for draw calls than the draw command's `visibleCount`. In these situations, Unity splits the draw command into multiple instanced draw calls.