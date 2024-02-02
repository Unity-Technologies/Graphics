# Screen Space Ambient Occlusion (SSAO)

The **Screen Space Ambient Occlusion** override is a real-time, full-screen lighting effect available in the High Definition Render Pipeline (HDRP). This effect approximates [ambient occlusion](https://en.wikipedia.org/wiki/Ambient_occlusion) in the current field of view. It approximates the intensity and position of ambient light on a GameObject’s surface, based on the light in the Scene and the environment around the GameObject. To achieve this, it darkens creases, holes, intersections, and surfaces that are close to one another. In real life, these areas tend to block out, or occlude, ambient light, and so appear darker.

For information on how to use a Texture to specify ambient occlusion caused by details present in a GameObject's Material but not on it's surface geometry, see [Ambient Occlusion](Ambient-Occlusion.md).

HDRP implements [ray-traced ambient occlusion](Ray-Traced-Ambient-Occlusion.md) on top of this override. This means that the properties visible in the Inspector change depending on whether you enable ray tracing.

<a name="enable-screen-space-ambient-occlusion"></md>

## Enable Screen Space Ambient Occlusion

[!include[](snippets/Volume-Override-Enable-Override.md)]

* To enable SSAO in your HDRP Asset go to **Lighting** > **Screen Space Ambient Occlusion**.
* To enable SSAO in your Frame Settings go to **Edit** > **Project Settings** > **Graphics** > **Pipeline Specific Settings** > **HDRP** > **Frame Settings (Default Values)** > **Camera** > **Lighting** > **Screen Space Ambient Occlusion**.

<a name="use-screen-space-ambient-occlusion"></md>

## Use Screen Space Ambient Occlusion

**Screen Space Ambient Occlusion** uses the [Volume](understand-volumes.md) framework, so to enable and modify **Screen Space Ambient Occlusion** properties, you must add an **Screen Space Ambient Occlusion** override to a [Volume](understand-volumes.md) in your Scene. To add **Ambient Occlusion** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override** > **Lighting** and click on **Ambient Occlusion**.
   HDRP now applies **Ambient Occlusion** to any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]


## Limitations

### Screen-space ambient occlusion

A screen-space effect only processes what's on the screen at a given point. This means that objects outside of the field of view can't visually occlude objects in the view. You can sometimes see this on the edges of the screen.
When rendering [Reflection Probes](Reflection-Probe.md) screen space ambient occlusion isn't supported.

