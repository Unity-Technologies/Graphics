# Path tracing

Path tracing is a ray tracing algorithm that sends rays from the Camera and, when a ray hits a reflective or refractive surface, recurses the process until it reaches a light source. The series of rays from the Camera to the Light form a "path".

It enables HDRP to compute many effects (such as hard or soft shadows, mirror or glossy reflections and refractions, and indirect illumination) in a single unified process.

A notable downside to path tracing is noise. However, noise vanishes as more paths accumulate, and eventually converges toward a clean image. For more information about path tracing limitations in HDRP, see [Unsupported features of path tracing](Ray-Tracing-Getting-Started.md#unsupported-features-of-path-tracing).

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

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Maximum Samples**   | Set the number of frames to accumulate for the final image. There is a progress bar at the bottom of the Scene view which indicates the current accumulation with respect to this value. |
| **Minimum Depth**     | Set the minimum number of light bounces in each path.        |
| **Maximum Depth**     | Set the maximum number of light bounces in each path. You can not set this to be lower than Minimum Depth.<br /> **Note**: You can set this and Minimum Depth to 1 if you only want to direct lighting. You can set them both to 2 if you only want to visualize indirect lighting (which is only visible on the second bounce). |
| **Maximum Intensity** | Set a value to clamp the intensity of the light value each bounce returns. This avoids very bright, isolated pixels in the final result.<br />**Note**: This property makes the final image dimmer, so if the result looks dark, increase the value of this property. |

![](Images/RayTracingPathTracing4.png)

**Minimum Depth** set to 1, **Maximum Depth** set to 2: direct and indirect lighting (1 bounce)

![](Images/RayTracingPathTracing5.png)

**Minimum Depth** set to 1, **Maximum Depth** set to 1: direct lighting only

![](Images/RayTracingPathTracing6.png)

**Minimum Depth** set to 2, **Maximum Depth** set to 2: indirect lighting only (1 bounce)

## Materials parameterization

Some light phenomena can be expressed more naturally in path tracing than in rasterization, for instance:

- Light refraction in transmissive objects.
- Light absorption in transmissive objects.
- Subsurface scattering.

Rasterization uses various methods to approximate complex lighting effects, with each of those methods relying on dedicated Material parameters. Path tracing computes those same effects all at once, without the need for as many parameters.

For that reason, some parameters have no effect in path tracing, while others bear a slightly different meaning.

### Refraction model

In the Lit family of materials, when the surface type is set to *Transparent*, you can select between *None*, *Box*, *Sphere* or *Thin* refraction models.

For path tracing, the distinction between *Box* or *Sphere* is irrelevant (as rays can intersect the real objects in the scene), and both effectively carry the common meaning of a *thick* mode, to be used on solid objects represented by a closed surface. On the other hand, *Thin* conveys the same idea as its rasterized version, and *None* is a special case of thin refractive surface, simulating alpha blending.

Additionally, transparent surfaces should be *Double-Sided*, so that they get intersected from both sides, and normal mode should be selected appropriately for each situation, as described right below.

| Refraction model  | Path tracing meaning                              | Surface sidedness                                 |
|-------------------|---------------------------------------------------|---------------------------------------------------|
| *Box* or *Sphere* | *Thick* object (e.g magnifying paperweight)       | Double sided, with *None* normal mode             |
| *Thin*            | *Thin* object (e.g soap bubble or window)         | Double sided, with *Flip* or *Mirror* normal mode |
| *None*            | *Thin* object, with smoothness = 1 and no Fresnel | Double sided, with *Flip* or *Mirror* normal mode |

The reason why normal mode should be set to *None* for *thick* objects, is that we want the intersection with a front normal to represent entering the medium (say, from air into glass), but also the back normal to represent leaving it.

### Subsurface scattering

In path tracing, the *Transmission* option of subsurface scattering will only take effect if the surface is also set to be *Double-Sided* (any normal mode will do), in which case it will receive light from both sides.

Here is an example of a sheet of fabric, lit from below by a point light:

![](Images/Path-traced-SSS-Single-sided.png)

Single-sided or no Transmission

![](Images/Path-traced-SSS-Double-sided.png)

Double-sided + Transmission

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
