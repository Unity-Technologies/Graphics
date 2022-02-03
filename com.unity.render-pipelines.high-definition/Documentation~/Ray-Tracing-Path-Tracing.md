# Path tracing

Path tracing is a ray tracing algorithm that sends rays from the Camera and, when a ray hits a reflective or refractive surface, recurses the process until it reaches a light source. The series of rays from the Camera to the Light form a "path".

It enables HDRP to compute various effects (such as hard or soft shadows, mirror or glossy reflections and refractions, and indirect illumination) in a single unified process.

A notable downside to path tracing is noise. Noise vanishes as more paths accumulate and converges toward a clean image. For more information about path tracing limitations in HDRP, see [Unsupported features of path tracing](Ray-Tracing-Getting-Started.md#unsupported-features-of-path-tracing).

![](Images/RayTracingPathTracing1.png)

Noisy image with **Maximum Samples** set to 1

![](Images/RayTracingPathTracing2.png)

Clean image with **Maximum Samples** set to 256

The current implementation for path tracing in the High Definition Render Pipeline (HDRP) accumulates paths for every pixel up to a maximum count unless the Camera moves. If the Camera moves, HDRP restarts the path accumulation. Path tracing supports Lit, LayeredLit, Stacklit, AxF, and Unlit materials, and area, point, directional, and environment lights.

## Setting up path tracing

Path tracing shares the general requirements and setup as other ray tracing effects, so for information on hardware requirements and set up, see [getting started with ray tracing](Ray-Tracing-Getting-Started.md). You must carry out this setup before you can add path tracing to your Scene.

## Adding path tracing to a Scene

Path tracing uses the [Volume](Volumes.md) framework, so to enable this feature, and modify its properties, you must add a Path Tracing override to a [Volume](Volumes.md) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, select Add Override > Ray Tracing > Path Tracing.
3. In the Inspector for the Path Tracing Volume Override, check the Enable option. If you do not see the Enable option, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [getting started with ray tracing](Ray-Tracing-Getting-Started.md). This switches HDRP to path-traced rendering and you should initially see a noisy image that converges towards a clean result.
4. If the image does not converge over time, select the drop-down next to the effect toggle and enable Always Refresh.

![](Images/RayTracingPathTracing3.png)

## Properties

| Property                    | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| **Maximum Samples**         | Set the number of frames to accumulate for the final image. There is a progress bar at the bottom of the Scene view which indicates the current accumulation with respect to this value. |
| **Minimum Depth**           | Set the minimum number of light bounces in each path.        |
| **Maximum Depth**           | Set the maximum number of light bounces in each path. You can not set this to be lower than Minimum Depth.<br /> **Note**: You can set this and Minimum Depth to 1 if you only want to direct lighting. You can set them both to 2 if you only want to visualize indirect lighting (which is only visible on the second bounce). |
| **Maximum Intensity**       | Set a value to clamp the intensity of the light value each bounce returns. This avoids very bright, isolated pixels in the final result.<br />**Note**: This property can make the final image dimmer, so if the result looks dark, increase the value of this property. |
| **Sky Importance Sampling** | Set the sky sampling mode. Importance sampling favors the brightest directions, which is beneficial when using a sky model with high contrast and very intense spots (like a sun, or street lights). On the other hand, it can be slightly detrimental when using a smooth, uniform sky. It is active by default for HDRI skies only, but can also be turned On and Off, regardless of the type of sky in use. |

![](Images/RayTracingPathTracing4.png)

**Minimum Depth** set to 1, **Maximum Depth** set to 2: direct and indirect lighting (1 bounce)

![](Images/RayTracingPathTracing5.png)

**Minimum Depth** set to 1, **Maximum Depth** set to 1: direct lighting only

![](Images/RayTracingPathTracing6.png)

**Minimum Depth** set to 2, **Maximum Depth** set to 2: indirect lighting only (1 bounce)

## Setting path tracing parameters for Materials


Path tracing in HDRP makes your scene appear more realistic. To do this, path tracing implements more precise light transport simulations than rasterization. The result is more realistic effects, especially for:


- Light refraction in transmissive objects.
- Light absorption in transmissive objects.
- Subsurface scattering.

Rasterization uses separate methods to approximate lighting effects, which require multiple Material parameters. Path tracing computes all lighting effects and how light interacts with Materials at the same time.

Some parameters have no effect when you use path tracing, and path tracing also changes how a Lit Material’s refraction model behaves. For more information, see [Refraction models](#refraction-models).

## How to enable path tracing on a Material

Path tracing only works on [Lit materials](Lit-Shader.md).

For path tracing to appear on a Lit Material, configure the following settings (pictured below): 

1. Set the [**Surface type]**(Surface-Type.md) property to **Transparent** (A)**.**
2. Set the surface Material to **Double-Sided** (C)**.**

For surface types that represent solid objects like a crystal ball, set the **Normal mode** to **None** (C). This allows the ray to intersect with a front [normal](https://en.wikipedia.org/wiki/Normal_(geometry)) to represent entering the medium (for example, from air into glass), and then back normal to represent leaving it.

![Surface_Options](Images/Surface_Options.png)

<a name="refraction-models"></a>

## How path tracing affects refraction models

Path tracing changes the way refraction models on a Lit Material behave. 

To change the type of refraction model a Lit Material uses, in the **Transparency Inputs** section, select a model from the **Refraction model** dropdown, displayed in the following image:

![Refraction_model](C:\Users\Vic Cooper\Documents\GitHub\Graphics\com.unity.render-pipelines.high-definition\Documentation~\Images\Refraction_model.png)

The following table describes how each refraction model behaves when you enable path tracing:

| **Refraction model**   | **Path tracing behavior**                                    | **Compatible Surface sides**                                 |
| ---------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Box** and **Sphere** | A surface type that represents thick objects like a paperweight or a crystal ball.When you enable path tracing, the **Box** and **Sphere** models behave in the same way. Rays can intersect the GameObjects in the scene and HDRP does not need to approximate transparent surfaces | This refraction model is compatible with a double-sided Material that has its **Normal mode** set to **None**. |
| **Thin**               | A thin surface type with [infinitesimal](<https://en.wikipedia.org/wiki/Infinitesimal>) thickness. Select this for thin, window-like surfaces.  When you enable path tracing, the behavior of the **Thin** refraction model behaves the same as in rasterization. | This refraction model is compatible with a double-sided Material that has its **Normal mode** set to  **Flip** or **Mirror**. |
| **None**               | A thin, refractive surface hardcoded to be smooth to simulate alpha blending. When you enable path tracing, the behavior of the **None** refraction model behaves the same as in rasterization. | This refraction model is compatible with a double-sided Material that has its **Normal mode** set to  **Flip** or **Mirror**. |

### Path tracing and subsurface scattering

To use [subsurface scattering's](Subsurface-Scattering.md) **Transmission** property (A) with path tracing:

1. Open the **Surface Options** window.
2. Enable the **Double-Sided** property (B). 

![Surface_Options_B](Images/Surface_Options_B.png)

The following example images display a sheet of fabric lit from below by a point light. The first image shows a single-sided surface, and the second shows a double-sided surface:

![](Images/Path-traced-SSS-Single-sided.png)

A single-sided surface with no transmission.

![](Images/Path-traced-SSS-Double-sided.png)

A double-sided surface with transmission.

### Hair

Path tracing can easily compute the complex multiple scattering events that occur within a head of hair, crucial for producing the volumetric look of lighter colored hair.

The [Hair Master Stack](master-stack-hair.md) allows the choice between an **Approximate** and **Physical** material mode and parameterization. Currently, it is required for a Hair Material to be configured with the **Physical** mode to participate in path tracing. The reason for this is due to the physically-based parameterization of the model (and the model itself) which produces far more accurate results in a path traced setting. Moreover, the **Approximate** material mode is a non energy-conserving model, better suited for performant rasterization after careful artist tuning.

Representing hair strand geometry is traditionally done via ray-aligned "ribbons", or tubes. Currently, the acceleration structure is not equipped to handle ray-aligned ribbons, so hair must be represented with tube geometry to achieve a good result.

The path traced **Physical** hair mode shares the exact same meaning for its parameters as its rasterized counterpart. However, the underlying model for path tracing differs: it performs a much more rigorous evaluation of the scattering within the fiber, while the rasterized version is only an approximated version of this result. Additionally, path tracing a volume of densely packed hair fibers allows you to compute the complex multiple scattering "for free", whereas in rasterizing we again must approximate this.

You can read more about the parameterization details of the **Physical** hair mode [here](master-stack-hair.md).

## Limitations

This section contains information on the limitations of HDRP's path tracing implementation. Mainly, this is a list of features that HDRP supports in its rasterized render pipeline, but not in its path-traced render pipeline.

### Unsupported features of path tracing

There is no support for path tracing on platforms other than DX12 for now.

HDRP path tracing in Unity 2020.2 has the following limitations:

- If a Mesh in your scene has a Material assigned that does not have the `HDRenderPipeline` tag, the mesh will not appear in your scene. For more information, see [Ray tracing and Meshes](Ray-Tracing-Getting-Started.md#RayTracingMeshes).
- Does not support 3D Text and TextMeshPro.
- Does not support Shader Graph nodes that use derivatives (for example, a normal map that derives from a texture).
- Does not support Shader Graphs that use [Custom Interpolators](../../com.unity.shadergraph/Documentation~/Custom-Interpolators.md).
- Does not support decals.
- Does not support tessellation.
- Does not support Tube and Disc-shaped Area Lights.
- Does not support Translucent Opaque Materials.
- Does not support several of HDRP's Materials. This includes Eye, Hair, and Decal.
- Does not support per-pixel displacement (parallax occlusion mapping, height map, depth offset).
- Does not support MSAA.
- Does not support [Graphics.DrawMesh](https://docs.unity3d.com/ScriptReference/Graphics.DrawMesh.html).
- Does not support [Streaming Virtual Texturing](https://docs.unity3d.com/Documentation/Manual/svt-streaming-virtual-texturing.html).
