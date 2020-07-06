# Decal

The High Definition Render Pipeline (HDRP) includes Decal Mesh and the [Decal Projector](Decal-Projector.html) component, which you can use to project specific Materials into your Scene to create realistic-looking decals. These Materials must use the [Decal Shader](Decal-Shader.md) or [Master node Decal](Master-Node-Decal.md). Use those Shader to create decal Materials that you can place, or project, into your Scene.

![](Images/HDRPFeatures-DecalShader.png)

**Limitation and compatibility**

Decal Projector can affect Opaque Material with both a Decal shader and a Master Node Decal. It can affect Transparent Material only with a Decal shader. Emissive isn't supported on Transparent Material. Decal Projector support Decal Layers. The **ReceiveDecals** property of Material don't affect an Emissive Decal. Emissive are always render, only Decal Layers allow a surface to disable them.
Decal Mesh can affect only Opaque Material with both a Decal shader and a Master Node Decal. Decal Mesh don't support Decal Layers.

# Decal Layers

## Enabling Decal Layers

To use Decal Layers, you must enable them in your Project’s [HDRP Asset](HDRP-Asset.html). You can then enable Decal Layers in your default [Frame Settings](Frame-Settings.html) to set your Cameras to process Decal Layers.

1. Select the HDRP Asset in the Project window and, in the Inspector, go to **Decal > Layers** and enable the checkbox.
2. To enable Decal Layers in the default Frame Settings for all Cameras, in your HDRP Asset, go to the **Default Frame Settings For** section, select **Camera** from the drop-down and, in the **Rendering** section, enable the **Decal Layers** checkbox. 

To override the Frame Settings for Cameras and set Decal Layers on an individual basis:

1. Click on a Camera in the Scene view or Hierarchy window to view its properties in the Inspector. 
2. Go to the **General** section and enable the **Custom Frame Settings** checkbox. This exposes the **Frame Settings Overrides,** which you can use to customize this Camera only. 
3. In the **Rendering** section, enable the **Decal Layers** checkbox to make this Camera use Decal Layers.

## Using Decal Layers

After you enable Decal Layers, you can then use them to decouple Meshes from certain Decal Projector in your Scene. To do this:

1. Click on a Decal Projector in the Hierarchy or the Scene view to view it in the Inspector.
2. Use the **Decal Layer** property drop-down to select which Decal Layers this Light affects.
4. Click on a Mesh Renderer or Terrain in the Hierarchy or the Scene view to view it in the Inspector.
5. Use the **Rendering Layer Mask** drop-down (See [MeshRenderer](https://docs.unity3d.com/Manual/class-MeshRenderer.html) for GameObjects or [OtherSettings](https://docs.unity3d.com/Manual/terrain-OtherSettings.html) for Terrain) to select which Decal Layers affect this Mesh Renderer or Terrain. When you enable Decal Layers, a Decal only affects a Mesh Renderer or Terrain if they both use a matching Decal Layer.

## Renaming Decal Layers

By default, in the UI for Decals Projector, Mesh Renderers or Terrain, Decal Layers are named **Decal Layer 1-7**. To more easily differentiate between them, you can give each Decal Layer a specific name. To do this, open the [Default Settings Windows](Default-Settings-Window.md), and go to **Decal Layer Names**. Here you can set the name of each Decal Layer individually.

## Enable/Disable Decal and Performance

Enabling Decal Layers require increase memory, have a GPU performance cost and generate more Shader Variant (so increase build time).

A Decal Shader or a Master Node Decal have a **Receive Decals** property allowing to disable Decal on those Material independently of the Decal Layers system. Disabling Decal with the Decal Layer system via **Rendering Layer Mask** of Mesh Renderer or Terrain don't save any performance. To save performance it is required to disable **Receive Decals** on the Material.

Implementation detail: Decal require to render depth in a Depth Prepass to apply on Opaque Material causing an extra CPU cost. Only Material with **Receive Decals** enable will render in the Depth Prepass unless there is a force of a full Depth Prepass. If Decal is disable with Decal Layers system, it will still render in the Depth Prepass. Only the **Receive Decals** from Material allow to save performance.

## Migration of data previous to Unity 2020.2

Before Unity 2020.2 the default value when creating a Mesh Renderer or Terrain of **Rendering Layer Mask** don't include any of the Decal Layer flags. Consequence, when enabling Decal Layers with those data they default to not receive any Decals. Later version have **Decal Layer Default** enable by default.
