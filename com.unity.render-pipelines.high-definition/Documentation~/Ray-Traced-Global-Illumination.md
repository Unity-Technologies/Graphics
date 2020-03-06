# Ray-Traced Global Illumination

Ray-Traced Global Illumination is a ray tracing feature in the High Definition Render Pipeline (HDRP). It is a more accurate alternative to Light Probes and lightmaps.

![](Images/RayTracedGlobalIllumination1.png)

**Ray-Traced Global Illumination off**

![](Images/RayTracedGlobalIllumination2.png)

**Ray-Traced Global Illumination on**

## Using Ray-Traced Global Illumination

For information about ray tracing in HDRP, and how to set up your HDRP Project to support ray tracing, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.html).

### Camera setup

Cameras use [Frame Settings](Frame-Settings.html) to decide how to render the Scene. To enable ray-traced global illumination for your Cameras by default:

1. Open the Project Settings window (menu: **Edit > Project Settings**), then select the HDRP Default Settings tab.
2. Select Camera from the Default Frame Settings For drop-down.
3. In the Rendering section, enable Ray Tracing.

All Cameras can now process ray-traced global illumination unless they use custom [Frame Settings](Frame-Settings.html). If they do:

1. In the Scene view or Hierarchy, select the Camera's GameObject to open it in the Inspector.
2. In the Custom Frame Settings, navigate to the Rendering section and enable Ray Tracing.

## Volume setup

Ray-Traced Global Illumination uses the [Volume](Volumes.html) framework, so to enable this feature and modify its properties, you need to add a Global Illumination override to a [Volume](Volumes.html) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to Add Override > Ray Tracing and click on Global Illumination.
3. In the Inspector for the Global Illumination Volume Override, enable Ray Tracing. HDRP now uses ray tracing to calculate reflections. If you do not see the Ray Tracing option, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [getting started with ray tracing](Ray-Tracing-Getting-Started.html).

![](Images/RayTracedGlobalIllumination3.png)

## Properties

Alongside the standard properties, Unity makes different properties available depending on the ray tracing mode set for this effect.

### Shared

| Property                       | Description                                                  |
| ------------------------------ | ------------------------------------------------------------ |
| **Ray Tracing**                | Enable this to make HDRP use ray tracing to evaluate indirect diffuse lighting. This makes extra properties available that you can use to adjust the quality of Ray-Traced Global Illumination. |
| **LayerMask**                  | Defines the layers that HDRP processes this ray-traced effect for. |
| **Ray Length**                 | Set a value to control the length of the rays that HDRP uses for ray tracing. If a ray doesn't find an intersection, then the ray returns the color of the sky. |
| **Clamp Value**                | Set a value to control the threshold that HDRP uses to clamp the pre-exposed value. This reduces the range of values and makes the global illumination more stable to denoise, but reduces quality. |
| **Denoise**                    | Enable this to enable the spatio-temporal filter that HDRP uses to remove noise from the Ray-Traced Global Illumination. |
| - **Half Resolution Denoiser** | Enable this feature to evaluate the spatio-temporal filter in half resolution. This decreases the resource intensity of denoising but reduces quality. |
| - **Denoiser Radius**          | Set the radius of the spatio-temporal filter.                |
| - **Second Denoiser Pass**     | Enable this feature to process a second denoiser pass. This helps to remove noise from the effect. |
| - **Second Denoiser Radius**   | Set the radius of the spatio-temporal filter for the second denoiser pass. |

### Performance Mode

| Property            | Description                                                  |
| -----------------   | ------------------------------------------------------------ |
| **Upscale Radius**  | Controls the radius of the up-scaler that HDRP uses to build the GI. The larger the radius, the more neighbors HDRP uses to build the GI, the better the quality. |
| **Full Resolution** | Enable this feature to increase the ray budget to one ray per pixel, per frame. Disable this feature to decrease the ray budget to one ray per four pixels, per frame. |

### Quality Mode

| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| **Sample Count** | Controls the number of rays per pixel per frame. Increasing this value increases execution time linearly. |
| **Bounce Count** | Controls the number of bounces that Global Illumination rays can do. Increasing this value increases execution time exponentially. |