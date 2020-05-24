# Path tracing

Path tracing is a ray tracing algorithm that sends rays from the Camera and, when a ray hits a reflective or refractive surface, recurses the process until it reaches a light source. The series of rays from the Camera to the Light forms a "path".

It enables HDRP to compute many different effects (such as hard or soft shadows, mirror or glossy reflections and refractions, and indirect illumination) in one single unified process.

A notable downside to path tracing is noise. However, noise vanishes as more paths accumulate, and eventually converges toward a clean image.

![](Images/RayTracingPathTracing1.png)

Noisy image with **Maximum Samples** set to 1

![](Images/RayTracingPathTracing2.png)

Clean image with **Maximum Samples** set to 256

The current implementation for path tracing in the High Definition Render Pipeline (HDRP) accumulates paths for every pixel up to a maximum count, unless the Camera moves. If the Camera moves, HDRP restarts the path accumulation. Path tracing supports Lit, LayeredLit and Unlit materials, and area, point, directional and environment lights.

## Set up path tracing

Path tracing shares the general requirements and setup as other ray tracing effects, so for information on hardware requirements and set up, see [getting started with ray tracing](Ray-Tracing-Getting-Started.html). You must carry out this setup before you can add path tracing to your Scene.

## Add path tracing to your Scene

Path Tracing uses the [Volume](Volumes.html) framework, so to enable this feature, and modify its properties, you must add a Path Tracing override to a [Volume](Volumes.html) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, select Add Override > Path Tracing.
3. In the Inspector for the Path Tracing Volume Override, check the Enable option. If you do not see the Enable option, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [getting started with ray tracing](Ray-Tracing-Getting-Started.html). This switches HDRP to path traced rendering and you should initially see a noisy image that converges towards a clean result.
4. If the image does not converge over time, select the drop-down next to the effect toggle and enable Animated Materials.

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