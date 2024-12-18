# Instancing

You can use instancing to batch process multiple effects, if they have the same [Visual Effect Graph Asset](VisualEffectGraphAsset.md). This can lead to a significant improvement in performance.

Instancing is enabled by default, except on Visual Effect Graph Assets that use features that do not support instancing. These include:

- [Output Mesh](Context-OutputMesh.md)
- [Output Event Handlers](OutputEventHandlers.md)

## How instancing works

Visual effects consist of data stored in buffers, and operations performed by shaders. Effects that use the same [Visual Effect Graph Asset](VisualEffectGraphAsset.md) perform the same operations. 

Instancing allocates data for these effects in shared buffers. A group of effects that share buffers is called a **batch**, and the effects are **instances** that belong to that batch.

Every time a new effect is created, the instancing system looks for a slot in an existing batch. If there aren't any empty slots available, the instancing system creates a new full batch, and allocates the effect data there. Batches for each [Visual Effect Graph Asset](VisualEffectGraphAsset.md) have **fixed capacity**, which you can set automatically (by default) or manually.

If instancing is disabled for an effect, the instancing system allocates a batch with a single instance.

During the simulation update, effects that belong to the same batch can run some operations together.

In the same way, during the rendering, effects can be grouped by batch, reducing CPU time.

## Configuration

The instancing feature is enabled by default, and does not require any configuration in most cases. However, to improve performance, you can adjust the batch capacity for each [Visual Effect Graph Asset](VisualEffectGraphAsset.md). You can also disable instancing on specific effects. 

### Adjust batch capacity

You can adjust the batch capacity for each [Visual Effect Graph Asset](VisualEffectGraphAsset.md#visual-effect-asset-inspector). To adjust the capacity, navigate to the VFX asset’s Inspector, and in the Instancing section, select Instancing Mode. 

* **Automatic batch capacity**: Set batch capacity automatically, based on the contents of the effect and the build platform. This is the default mode.
* **Custom batch capacity**: Set the capacity to a custom value. This is useful if you have a good estimation of how many effects should exist at the same time, or if you want to have a better control on how much memory is used. 

When you set a custom capacity, the effect uses the same value on all platforms. You should profile on different scenarios to ensure that there is no regression in performance. 

### Disable instancing

You can disable instancing on a specific [Visual Effect Graph Asset](VisualEffectGraphAsset.md#visual-effect-asset-inspector) or [Visual Effect component](VisualEffectComponent.md#the-visual-effect-inspector), or across your whole project. Disabling instancing sets a batch capacity of 1 on each affected effect. You should only disable instancing for debugging purposes, or if you are confident that you don't need instancing. 

To disable instancing on a specific [Visual Effect Graph Asset](VisualEffectGraphAsset.md#visual-effect-asset-inspector):

1. Navigate to the VFX asset’s Inspector.
1. In the**Instancing** section, select **Instancing Mode**. 
1. Set **Instancing** mode to **Disabled**.

To disable instancing on a specific [Visual Effect component](VisualEffectComponent.md#the-visual-effect-inspector): 

1. Navigate to the Visual Effect component’s Inspector.
1. In the **Instancing** section, deactivate **Allow Instancing**.

To disable instancing across your project:

1. Open the [Visual Effect preferences](VisualEffectPreferences.md) ( Windows: **Edit > Preferences > Visual Effects**. macOS: **Unity > Settings > Visual Effects**).
1. Deactivate **Instancing Enabled**.

## Scripting API

Instancing adds some scripting functionality through [VFX Manager scripting API](https://docs.unity3d.com/ScriptReference/VFX.VFXManager.html).

Using this, you can access [additional information and stats](https://docs.unity3d.com/ScriptReference/VFX.VFXBatchedEffectInfo.html) for each Visual Effect Asset.

