# Path Tracing

Path tracing is a ray tracing algorithm that sends rays from the Camera and, when a ray hits a reflective or refractive surface, recurses the process until it reaches a light source. The series of rays from the Camera to the Light forms a "path".

It enables HDRP to compute many different effects (such as: hard/soft shadows, mirror or glossy reflections and refractions, and indirect illumination) using a single, unified, method. However, there is one notable downside to path tracing which is noise. Noise does vanish as more paths accumulate, eventually converging towards a clean image.

![](Images/RayTracingPathTracing1.png)

Noisy image with Maximum Samples set to 1

![](Images/RayTracingPathTracing2.png)

Clean image with Maximum Samples set to 256

## Using path tracing

The current implementation for path tracing in the High Definition Render Pipeline (HDRP) accumulates paths, for every pixel, up to a maximum count, unless the Camera moves in which case HDRP restarts the path accumulation. It currently only supports diffuse and specular reflections, area lights, and environment lights.

Path tracing shares the general requirements and setup as other ray tracing effects, so for information on hardware requirements and set up, see [getting started with ray tracing](Ray-Tracing-Getting-Started.html). The only difference is that you need to set the [Ray Tracing Tier](Ray-Tracing-Getting-Started.html#TierTable) to 3.

After you set up your HDRP Project to support path tracing, you can add path tracing to your Scene.

Path Tracing uses the [Volume](Volumes.html) framework, so to enable this feature, and modify its properties, you must add a Path Tracing override to a [Volume](Volumes.html) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, select Add Override > Path Tracing.
3. In the Inspector for the Path Tracing Volume Override, check the Enable option. If you do not see the Enable option, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [getting started with ray tracing](Ray-Tracing-Getting-Started.html). This switches HDRP to path traced rendering and you should initially see a noisy image that converges towards a clean result.
4. If the image does not converge over time, select the drop-down next to the effect toggle and enable Animated Materials.
   ![](Images/RayTracingPathTracing3.png)

## Properties

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Maximum Samples**   | Set the number of frames to accumulate if the image is left to converge until the end. There is a progress bar at the bottom of the Scene view which indicates the current accumulation with respect to this value. |
| **Minimum Depth**     | Set the minimum number of light bounces in each path.        |
| **Maximum Depth**     | Set the maximum number of light bounces in each path. For instance, set this and Minimum Depth to 1 to see direct lighting only, and set them both to 2 to visualize indirect lighting (2nd bounce only). |
| **Maximum Intensity** | Set a value to clamp the intensity of the light value each bounce returns. This avoids very bright, isolated pixels in the final result. Note: This makes the final image dimmer so, if the result looks dark, increase this value. |

![](Images/RayTracingPathTracing4.png)

Minimum Depth set to 1, Maximum Depth set to 2: direct and indirect lighting (1 bounce)

![](Images/RayTracingPathTracing5.png)

Minimum Depth set to 1, Maximum Depth set to 1: direct lighting only

![](Images/RayTracingPathTracing6.png)

Minimum Depth set to 2, Maximum Depth set to 2: indirect lighting only (1 bounce)