# Use the recursive rendering volume override

GameObjects that use the recursive rendering mode cast refraction and reflection rays recursively. This means that when a ray hits a surface, it reflects or refracts and carries on to hit other surfaces. You can increase the maximum number of times that a ray bounces. However, a higher number of rays is more resource-intensive. Recursive rendering is a replacement pipeline for rendering Meshes in the High Definition Render Pipeline (HDRP). 

Rays ignore the smoothness of a Material when being reflected or refracted, which makes this rendering mode useful for rendering multi-layered transparent GameObjects. This forces the objects to have smooth reflections as long as their smoothness is above the minimum smoothness specified in the volume override. In the case the smoothness of the surface is below the minimum smoothness it will fallback on the following indirect specular approach.

HDRP might display the sky color instead of a GameObject that has ray tracing applied. This happens when the GameObject is further away from the Camera than the Max Ray Length value set in the volume component. To make the GameObject appear correctly, increase the value of the Max Ray Length property.

![Car gear shift rendered with recursive ray tracing.](Images/RayTracingRecursiveRendering1.png)

**Car gear shift rendered with recursive ray tracing**

To troubleshoot this effect, HDRP provides a Recursive Rendering [Debug Mode](Ray-Tracing-Debug.md) and a Ray Tracing Acceleration Structure [Debug Mode](Ray-Tracing-Debug.md) in Lighting Full Screen Debug Mode.

## Create a recursive rendering volume override

Recursive Rendering uses the [Volume](understand-volumes.md) framework, so to enable this feature and modify its properties, you need to add a Recursive Rendering override to a [Volume](understand-volumes.md) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override > Ray Tracing** and click on **Recursive Rendering**.
3. In the Inspector for the Recursive Rendering Volume Override, set **State** to **Enabled**. If you don't see **State**, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [getting started with ray tracing](Ray-Tracing-Getting-Started.md).

Now that you have recursive rendering set up in your Scene, you must set GameObjects to use the Raytracing rendering pass to make HDRP use recursive rendering for them. To do this:

1. Select the GameObject in the Scene view or Hierarchy to view it in the Inspector.
2. Select the Material attached to the GameObject.
3. In the **Surface Options** foldout, enable **Recursive Rendering (Preview)**.

You can also do this for Shader Graph master nodes:

1. In the Project window, double-click on the Shader to open it in Shader Graph.
2. In the **Graph Settings** tab of the **Graph Inspector**, go to **Surface Options** and enable **Recursive Rendering (Preview)**.

For information about recursive rendering properties, refer to [Recursive rendering override reference](reference-recursive-rendering.md).

It is best practice to use recursive rendering in situations that require multi-bounced reflection and refraction, for example, car headlights. You can use recursive rendering in simple scenarios, like a mirror or a puddle, but for best performance, use [ray-traced reflections](Ray-Traced-Reflections.md).

Since recursive rendering uses an independent render pass, HDRP cannot render any other ray-traced effects on recursively rendered GameObjects. For example, it cannot render effects such as ray-traced subsurface scattering or ray-traced shadows.


