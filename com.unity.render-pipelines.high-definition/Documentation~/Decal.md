# Decal

The High Definition Render Pipeline (HDRP) includes the following ways to create decals in a Scene.

- Use a Decal Mesh and manually position the decal.

- Use the [Decal Projector](Decal-Projector.md) component to project the decal.

To use these methods, you need to create a decal Material. A decal Material is a Material that uses the [Decal Shader](Decal-Shader.md) or [Decal Master Stack](master-stack-decal.md). You can then place or project your decal Material into a Scene.

![](Images/HDRPFeatures-DecalShader.png)

## Decal Layers

HDRP includes Decal Layers that you can use to specify which Materials a decal affects based on which layer you assign it to.

### Enabling Decal Layers

To use Decal Layers, enable them in your Project’s [HDRP Asset](HDRP-Asset.md).  To do this:
1. Navigate to the **Project** window and open your HDRP Asset.
2. In the Inspector, open the **Decals** dropdown.
3. Enable the  **Layers**  checkbox.

You can then enable Decal Layers in your [Frame Settings](Frame-Settings.md) to enable Decal Layers for all Cameras. To do this:

1. Go to **Edit** > **Project Settings** open the **Graphics** section and select [HDRP Global Settings](Default-Settings-Window.md).
2. Go to the **Frame Settings (Default Values**) > **Camera** section.
3. Open the **Rendering** section.
4. Enable the **Decal Layers** checkbox.

To control the Frame Settings and set Decal Layers for a specific Camera:

1. Click on a Camera in the Scene view or Hierarchy window to view its properties in the Inspector.
2. Go to the **Rendering** section and enable the **Custom Frame Settings** checkbox. This opens a new **Frame Settings Overrides** section, which you can use to customize this Camera.
3. In the **Frame Settings Overrides** section open the **Rendering** section.
4. Enable the **Decal Layers** checkbox.

### Using Decal Layers

When you enable Decal Layers, a Decal only affects a Mesh Renderer or Terrain if they both use a matching Decal Layer. You can use Decal Layers to separate Meshes from specific [Decal Projectors](Decal-Projector.md) in your Scene. To do this:

1. Click on a Decal Projector in the Hierarchy or the Scene view to view it in the Inspector.
2. Use the **Decal Layer** property drop-down to select which Decal Layers this Decal Projector affects.
4. Click on a Mesh Renderer or Terrain in the Hierarchy or the Scene view to view it in the Inspector.
5. Use the **Rendering Layer Mask** drop-down (See [MeshRenderer](https://docs.unity3d.com/Manual/class-MeshRenderer.html) for GameObjects or [OtherSettings](https://docs.unity3d.com/Manual/terrain-OtherSettings.html) for Terrain) to select which Decal Layers affect this Mesh Renderer or Terrain.

### Renaming Decal Layers

By default, in the UI for Decal Projectors, Mesh Renderers, or Terrain, Decal Layers are named **Decal Layer 1-7**. You can give each Decal Layer a specific name. To do this:

1. Open the [HDRP Global Settings](Default-Settings-Window.md).
2. Expand the **Decal Layer Names** section.

Here you can set the name of each Decal Layer individually.

### How Decal Layers affect performance

When you enable Decal Layers, it increases the build time of your project. This is because Decal Layers:

* Uses a high amount of memory.
* Increases GPU performance cost.
* Generates more [Shader Variants](https://docs.unity3d.com/Manual/shader-variants.html).

HDRP renders Material depth in a Depth Prepass to apply decals to opaque Materials. This increases resource use on the CPU. Only Materials that have the **Receive Decals** property enabled render in the Depth Prepass, unless you force a full Depth Prepass. To prevent HDRP from rendering a Material which shouldn't receive Decals in the Depth Prepass:

1. Open the Material assigned to a Mesh Renderer or Terrain that you do not want to display decals on.

2. Disable the **Receive Decals** property.

If you use the Decal Layer system to change the **Rendering Layer Mask** of a Mesh Renderer or Terrain to disable decal, it doesn't have an effect on your application's performance.

## Additive Normal Blending

You can use Additive normal blending to blend decal normals with the normals of a specific GameObject.
In the following image examples, the screenshot on the left does not use additive normal blending, and the screenshot on the right uses additive normal blending.:

![](Images/HDRPFeatures-SurfGrad.png)

To use Additive Normal Blending:
1. Open your Project’s [HDRP Asset](HDRP-Asset.md).
2. In the Inspector, go to **Rendering**  >**Decals** and enable the **Additive Normal Blending** checkbox.

### High Precision Normal Buffer

When you use additive normal blending, HDRP constrains the decal displacement to a cone of 45° around the object normal to reduce banding artifacts.
To remove this angle constraint on the normal at the cost of a higher memory usage, enable the High Precision Normal Buffer setting. To do this:

1. Select your Project’s [HDRP Asset](HDRP-Asset.md).
2. In the Inspector, go to **Rendering** > **Decals** and enable the **High Precision Normal Buffer** checkbox.

## Limitations

- A Decal Projector can only affect transparent Materials when you use the [Decal Shader](Decal-Shader.md).

- The Decal Shader does not support emissive on Transparent Materials and does support Decal Layers.

- Decal Meshes can only affect opaque Materials with either a [Decal Shader](Decal-Shader.md) or a [Decal Master Stack](master-stack-decal.md).

- Decal Meshes do not support Decal Layers.

### Migration of data previous to Unity 2020.2

When you convert a project from 2020.2, Mesh renderers and Terrain do not receive any decals by default.

This is because, before Unity 2020.2, the default value for the **Rendering Layer Mask** for new Mesh Renderers and Terrain doesn't include Decal Layer flags.
