# Ray-Traced Contact Shadows

Ray-Traced Contact Shadows is a ray tracing feature in the High Definition Render Pipeline (HDRP). It's an alternative to HDRP's [Contact Shadow](Override-Contact-Shadows.md) technique that uses a more accurate ray-traced solution that can use off-screen data.

![A stone temple scene without contact shadows. Foliage and dark corners have few shadows.](Images/RayTracedContactShadow1.png)<br/>
A stone temple scene without contact shadows. Foliage and dark corners have few shadows.

![A stone temple scene with contact shadows. Foliage and dark corners have more shadows.](Images/RayTracedContactShadow2.png)<br/>
A stone temple scene with contact shadows. Foliage and dark corners have more shadows.

![A stone temple scene with ray-traced contact shadows. Foliage and dark corners have even more shadows.](Images/RayTracedContactShadow3.png)<br/>
A stone temple scene with ray-traced contact shadows. Foliage and dark corners have even more shadows.

For information about ray tracing in HDRP, and how to set up your HDRP Project to support ray tracing, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

To troubleshoot this effect, HDRP provides a Shadows [Debug Mode](Ray-Tracing-Debug.md) and a Ray Tracing Acceleration Structure [Debug Mode](Ray-Tracing-Debug.md) in Lighting Full Screen Debug Mode.

## Using Ray-Traced Contact Shadows

This feature is an alternative to the [Contact Shadows](Override-Contact-Shadows.md) Volume override, so the initial setup is similar. To set up ray-traced contact shadows:

1. First follow the [Enabling Contact Shadows](Override-Contact-Shadows.md#enable-contact-shadows) and [Using Contact Shadows](Override-Contact-Shadows.md#use-contact-shadows) steps to set up the Contact Shadows override.
2. In the Frame Settings for your Cameras, enable **Ray Tracing**.
3. HDRP calculates ray-traced contact shadows on a per-light basis. This means you need to enable it for each light.
4. Select a [Light](Light-Component.md) and, in the Inspector, go to **Shadows > Contact Shadows** and tick the **Enable** checkbox. This exposes the **Ray Tracing** property.
5. Enable the **Ray Tracing** checkbox.
