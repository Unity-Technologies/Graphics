# Decal

The High Definition Render Pipeline (HDRP) includes two ways to create decals in a Scene. You can either use a decal Mesh and manually position the decal or use the [Decal Projector](Decal-Projector.md) component to project the decal. Both of these methods require that you create a decal Material, which is a Material that uses either the [Decal Shader](Decal-Shader.md) or [Decal master node](Master-Node-Decal.md). You can use either to create decal Materials that you can place or project into a Scene.

![](Images/HDRPFeatures-DecalShader.png)

**Limitation and compatibility**

The Decal Projector can affect opaque Materials with either a [Decal Shader](Decal-Shader.md) or a [Decal master node](Master-Node-Decal.md). However, it can only affect transparent Materials with the [Decal Shader](Decal-Shader.md). It does not support emissive on Transparent Materials and does support Decal Layers.
Decal Meshes can only affect opaque Materials with either a [Decal Shader](Decal-Shader.md) or a [Decal master node](Master-Node-Decal.md). They also do not support Decal Layers.

# Decal Layers

## Enabling Decal Layers

To use Decal Layers, first enable them in your Project’s [HDRP Asset](HDRP-Asset.md). You can then enable Decal Layers in your [Frame Settings](Frame-Settings.md) to set your Cameras to process Decal Layers.
1. Select the HDRP Asset in the Project window and, in the Inspector, go to **Decal > Layers** and enable the checkbox.
2. To enable Decal Layers in the default Frame Settings for all Cameras, in your HDRP Asset, go to the **Default Frame Settings For** section, select **Camera** from the drop-down and, in the **Rendering** section, enable the **Decal Layers** checkbox. 

To override the Frame Settings for Cameras and set Decal Layers on an individual basis:

1. Click on a Camera in the Scene view or Hierarchy window to view its properties in the Inspector. 
2. Go to the **General** section and enable the **Custom Frame Settings** checkbox. This exposes the **Frame Settings Overrides,** which you can use to customize this Camera only. 
3. In the **Rendering** section, enable the **Decal Layers** checkbox to make this Camera use Decal Layers.

## Using Decal Layers

After you enable Decal Layers, you can then use them to decouple Meshes from certain Decal Projectors in your Scene. To do this:

1. Click on a Decal Projector in the Hierarchy or the Scene view to view it in the Inspector.
2. Use the **Decal Layer** property drop-down to select which Decal Layers this Decal Projector affects.
4. Click on a Mesh Renderer or Terrain in the Hierarchy or the Scene view to view it in the Inspector.
5. Use the **Rendering Layer Mask** drop-down (See [MeshRenderer](https://docs.unity3d.com/Manual/class-MeshRenderer.html) for GameObjects or [OtherSettings](https://docs.unity3d.com/Manual/terrain-OtherSettings.html) for Terrain) to select which Decal Layers affect this Mesh Renderer or Terrain. When you enable Decal Layers, a Decal only affects a Mesh Renderer or Terrain if they both use a matching Decal Layer.

## Renaming Decal Layers

By default, in the UI for Decal Projectors, Mesh Renderers, or Terrain, Decal Layers are named **Decal Layer 1-7**. To more easily differentiate between them, you can give each Decal Layer a specific name. To do this, open the [Default Settings Windows](Default-Settings-Window.md), and go to **Decal Layer Names**. Here you can set the name of each Decal Layer individually.

## Performance implications

Enabling Decal Layers require increase memory, have a GPU performance cost and generate more Shader Variant (so increase build time).

If you use the Decal Layer system to disable a decal, via the **Rendering Layer Mask** of a Mesh Renderer or Terrain, it doesn't save on any performance. Instead, to save performance, you need to disable the **Receive Decals** property for the Mesh Renderer or Terrain's Material.

Implementation detail: To allow HDRP to apply decals to opaque Materials, it must render depth in a Depth Prepass, which adds to the CPU resource intensity of the operation. Only Materials with **Receive Decals** enabled render in the Depth Prepass, unless you force a full Depth Prepass. If you disable a decal with the Decal Layers system, HDRP still renders it in the Depth Prepass. This is why you need to disable the **Receive Decals** property on Materials to save performance.

## Migration of data previous to Unity 2020.2

Before Unity 2020.2, the default value for the **Rendering Layer Mask** for new Mesh Renderers and Terrain doesn't include any of the Decal Layer flags. This means that, when you enable Decal Layers, these Mesh Renderers and Terrain default to not receive any Decals. Later versions use **Decal Layer Default**  by default.
