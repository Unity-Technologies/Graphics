# Use Screen Space Global Illumination

The **Screen Space Global Illumination** (SSGI) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate diffuse light bounces.

HDRP implements [ray-traced global illumination](Ray-Traced-Global-Illumination.md) (RTGI) on top of this override. This means that the properties visible in the Inspector change depending on whether you enable ray tracing.

SSGI and RTGI replace all [lightmap](https://docs.unity3d.com/Manual/Lightmapping.html) and [Light Probe](https://docs.unity3d.com/Manual/LightProbes.html) data. If you enable this override on a Volume that affects the Camera, Light Probes and the ambient probe stop contributing to lighting for GameObjects.

![](Images/HDRPFeatures-SSGI.png)

## Enable Screen Space Global Illumination
[!include[](Snippets/Volume-Override-Enable-Override.md)]

To enable SSGI:

1. Open your HDRP Asset in the Inspector.
2. Go to **Lighting** and enable **Screen Space Global Illumination**.
3. Go to **Edit** > **Project Settings** > **Graphics** > **Pipeline Specific Settings** > **HDRP** > **Frame Settings (Default Values)** > **Lighting** and enable **Screen Space Global Illumination**.

## Use Screen Space Global Illumination

HDRP uses the [Volume](understand-volumes.md) framework to calculate SSGI, so to enable and modify SSGI properties, you must add a **Screen Space Global Illumination** override to a [Volume](understand-volumes.md) in your Scene. To add **Screen Space Global Illumination** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Lighting** and select **Screen Space Global Illumination**. HDRP now calculates SSGI for any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

[!include[](snippets/tracing-modes.md)]

## Properties

To learn about SSGI properties, refer to [Screen Space Global Illumination (SSGI) reference](reference-screen-space-global-illumination.md).

### Limitations

* SSGI is not compatible with [Reflection Probes](Reflection-Probe.md).
* When you set [Lit Shader mode](Forward-And-Deferred-Rendering.md) to **Deferred** the Ambient Occlusion from Lit Shader will combine with Screen Space Ambient Occlusion and apply to the indirect lighting result where there is no Emissive contribution. This is similar behavior to rendering with Lit Shader mode set to **Forward**. If the Material has an emissive contribution then Ambient Occlusion is set to one.
