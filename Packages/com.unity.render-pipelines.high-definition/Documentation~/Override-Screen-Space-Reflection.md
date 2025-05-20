# Use the screen space reflection (SSR) override

The **Screen Space Reflection** (SSR) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate reflections. For information about how SSR works in HDRP, see [Understand reflection in HDRP](Override-Screen-Space-Reflection.md).

HDRP implements [ray-traced reflection](Ray-Traced-Reflections.md) on top of this override. This means that the properties visible in the Inspector change depending on whether or not you enable ray tracing.

## Enable screen space reflection

[!include[](snippets/Volume-Override-Enable-Override.md)]

To enable SSR:

1. Open your HDRP Asset in the Inspector.
2. Go to **Lighting** > **Reflections** and enable **Screen Space Reflection**.
3. Go to **Edit** > **Project Settings** > **Graphics** > **Pipeline Specific Settings** > **HDRP** > **Frame Settings (Default Values)** > **Lighting** and enable **Screen Space Reflection**.

## Set up screen space reflection

HDRP uses the [Volume](understand-volumes.md) framework to calculate SSR, so to enable and modify SSR properties, you must add a **Screen Space Reflection** override to a [Volume](understand-volumes.md) in your Scene. To add **Screen Space Reflection** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click **Screen Space Reflection**.
   HDRP now calculates SSR for any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

[!include[](snippets/tracing-modes.md)]

To learn about the Screen Space Reflection override properties, refer to [Screen space reflection (SSR) reference](reference-screen-space-reflection.md).

## Debug screen space reflection speed rejection

HDRP includes a **Fullscreen Debug Mode** called **Screen Space Reflection Speed Rejection** (menu: **Lighting > Fullscreen debug mode > Screen Space Reflection Speed Rejection**) that you can use to visualise the contribution of the following properties:

- **Speed Rejection**
- **Speed Rejection Scaler Factor**
- **Speed From Reflecting Surface**
- **Speed From Reflected Surface**
- **Speed Smooth Rejection**

This fullscreen debug mode uses a color scale from green to red. Green areas indicate the sample is accumulated according to the **Accumulation Factor** and red areas indicate that HDRP rejects this sample. Orange areas indicate a that HDRP accumulates some samples and rejects some samples in this area.

In the following example image, the car GameObject is in the center of the Camera's view. This means the car has no relative motion to the Camera.

This example image uses **Speed From Reflected Surface** to accumulate the samples from the car and partially accumulate the samples from the sky. This makes the car and its reflection appear green, and the surface that reflects the sky appear orange.

![Example: This image uses the **Speed From Reflected Surface** property to accumulate the samples from the car and partially accumulate the samples from the sky. This makes the car and its reflection appear green, and the surface that reflects the sky appear orange.](Images/ScreenSpaceReflectionPBR_SpeedRejectionSmooth.gif)

## Limitations

### Screen-space reflection

To calculate SSR, HDRP reads a color buffer with a blurred mipmap generated during the previous frame.

The color buffer only includes transparent GameObjects that use the **BeforeRefraction** [Rendering Pass](Surface-Type.md). However, HDRP incorrectly reflects a transparent GameObject using the depth of the surface behind it, even if you enable **Depth Write** in the GameObject's Material properties. This is because HDRP calculates SSR before it adds the depth of transparent GameObjects to the depth buffer.

![Example: How opaque, refraction-based transparent, and default transparent materials interact with the depth buffer, affecting their visibility in screen space reflections in a 3D rendering environment.](Images/SSRTransparents.png)

If a transparent material has **Receive SSR Transparent** enabled, HDRP always uses the **Approximation** algorithm to calculate SSR, even you select **PBR Accumulation**.

When a transparent material has rendering pass set to **Low Resolution**, then **Receive SSR Transparent** can't be selected.

### Ray-traced reflection

Currently, ray tracing in HDRP doesn't support [decals](decals.md). This means that, when you use ray-traced reflection, decals don't appear in reflective surfaces.

If a transparent material has **Receive SSR Transparent** enabled, HDRP will evaluate the reflections as smooth.
