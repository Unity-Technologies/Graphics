---
uid: um-batch-renderer-group-getting-started
---

# Set up your project for the BatchRendererGroup API

Before you use BRG, your project must support it. BRG requires your project to:

* Use the SRP Batcher. To enable the SRP Batcher, see [Using the SRP Batcher](SRPBatcher.md#using-the-srp-batcher).
* Keep BRG [shader variants](https://docs.unity3d.com/6000.0/Documentation/Manual/shader-variants). To do this, select **Edit** > **Project Settings** > **Graphics**, and set **BatchRendererGroup variants** to **Keep all**.
* Allow [unsafe code](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/unsafe-code). To do this, enable the **Allow ‘unsafe’ Code** [Player Setting](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PlayerSettings).

**Note:** The BatchRendererGroup uses [DOTS Instancing shaders](dots-instancing-shaders.md), but it doesn't require any DOTS packages. The name reflects the new data-oriented way to load instance data, and also helps with backward compatibility with existing Hybrid Renderer compatible shaders.

For information on how to use BRG to create a basic renderer, see [Creating a renderer with BatchRendererGroup](batch-renderer-group-creating-a-renderer.md).
