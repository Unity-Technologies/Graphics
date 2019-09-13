# Ray traced shadows

Ray traced shadows replaces shadow maps for opaque GameObjects. HDRP ray tracing currently  supports the following [Light](Light-Component.html) types:

- [Directional](#DirectionalLight)
- [Point](#PointLight)
- [Rectangle](#RectangleLight)

## Using ray traced shadows

All ray traced shadows are screen space shadows. This means that HDRP stores them in a screen space buffer which holds the information for every pixel on the screen that is in the depth buffer (which only stores opaque GameObjects).

To use ray traced shadows, enable screen space shadows in your HDRP Project, to do this:

1. Click on your HDRP Asset in the Project window to view it in the Inspector.
2. In the Lighting section, enable Screen Space Shadows.
3. Set the value for Maximum to be the maximum number of screen space shadows you want to evaluate each frame. If there are than this number of Lights in your Scene than HDRP only ray casts the shadows for this number of them, then uses a shadow map for the rest.

Then make sure to enable Screen Space Shadows for your Cameras. To do this:

1. Open the Project Settings window (menu: Edit > Project Settings), then select the HDRP Default Settings tab.
2. Select Camera from the Default Frame Settings For drop-down.
3. In the Lighting section, enable Screen Space Shadows.

Finally, to make HDRP process ray traced shadows for your Directional, Point, or Rectangle Light:

1. Under the Shadow Map foldout in the Shadows section, click the Enable checkbox.
2. Still under the Shadow Map foldout, enable Ray Traced Shadows. For Directional Lights, you need to enable Screen Space Shadows to access this property.
3. You can now edit the properties underneath Ray Traced Shadows to change the behavior of the shadows.

<a name="DirectionalLight"></a>

## Directional Light

Ray traced shadows offers an alternative to the cascade shadow map that Directional Lights use for opaque GameObjects.

![](Images/RayTracedShadows1.png)

**Directional Light cascade shadow map**

![](Images/RayTracedShadows2.png)

**Ray traced Directional Light shadows (Sun Angle = 0)**

![](Images/RayTracedShadows3.png)

**Ray raced Directional Light shadows (Sun Angle = 0.53, the angle of the Sun as seen from Earth)**

### Properties

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Sun Angle**         | The size of the Sun the sky, in degrees. The value for the Sun on Earth is 0.53°. |
| **Sample Count**      | Controls the number of rays that HDRP uses per pixel, per frame. Increasing this values increases execution time linearly. |
| **Denoise**           | Enables the spatio-temporal filter that HDRP uses to remove noise from the ray traced shadows. |
| - **Denoiser Radius** | Controls the radius of the spatio-temporal filter.           |

<a name="PointLight"></a>

## Point Light

Ray traced shadows offers an alternative to the shadow map that Point Lights use for opaque GameObjects. HDRP still evaluates the lighting of a Point Light as coming from a single point in space (the light is [punctual](Glossary.html#PunctualLights)), but it evaluates the shadowing as if the light was coming from the surface of a sphere.

![](Images/RayTracedShadows4.png)

**Point Light shadow map**

![](Images/RayTracedShadows5.png)

**Ray traced Point Light shadows (Radius = 0.001m)**

![](Images/RayTracedShadows6.png)

**Ray traced Point Light shadows (radius = 0.5m)**

### Properties

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Sample Count**      | Controls the number of rays that HDRP uses per pixel, per frame. Increasing this values increases execution time linearly. |
| **Radius**            | Sets the radius of the sphere light that HDRP uses to evaluate the shadows. |
| **Denoise**           | Enables the spatio-temporal filter that HDRP uses to remove noise from the ray traced shadows. |
| - **Denoiser Radius** | Controls the radius of the spatio-temporal filter.           |

<a name="RectangleLight"></a>

## Rectangle Light

Ray traced shadows offers an alternative to the [exponential variance shadow map](Glossary.html#ExponentialVarianceShadowMap) that Rectangle Lights use for opaque GameObjects.

![](Images/RayTracedShadows7.png)

**Rectangle Light shadow map**

![](Images/RayTracedShadows8.png)

**Ray traced Rectangle Light shadows**

### Properties

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Sample Count**      | Controls the number of rays that HDRP uses per pixel, per frame. Increasing this values increases execution time linearly. |
| **Denoise**           | Enables the spatio-temporal filter that HDRP uses to remove noise from the ray traced shadows. |
| - **Denoiser Radius** | Controls the radius of the spatio-temporal filter.           |