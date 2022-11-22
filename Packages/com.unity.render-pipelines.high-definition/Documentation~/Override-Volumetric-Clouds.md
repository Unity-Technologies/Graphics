# Volumetric Clouds

![](Images/volumetric-clouds-1.png)

The **Volumetric Clouds** [Volume component override](Volume-Components.md) controls settings relevant to rendering volumetric clouds in the High Definition Render Pipeline (HDRP). Volumetric clouds are interactable clouds that can render shadows, and receive fog and volumetric light.

To generate and render volumetric clouds, HDRP uses:

* A cloud lookup table - defines properties such as the altitude, density, and lighting.
* A cloud volume - describes the area in the Scene that HDRP generates the clouds in.
* A cloud map - acts like a top down view of the scene. It defines which areas of the cloud volume have clouds and what kind of cloud they are.

Using these three things, HDRP generates volumetric clouds in a two-step process:

1. **Shaping**: HDRP uses large scale noise to create general cloud shapes.
2. **Erosion**: Using the clouds generated in the shaping stage, HDRP applies a smaller scale noise to them to add local details to their edges.

## Enabling Volumetric Clouds

[!include[](snippets/Volume-Override-Enable-Override.md)]

* In your [HDRP Asset](HDRP Asset) go to **Lighting > Volumetrics > Volumetric Clouds**.

* In your [Frame Settings](Frame-Settings.md) go to **Lighting > Volumetric Clouds**.

## Using Volumetric Clouds

**Volumetric Clouds** uses the [Volume](Volumes.md) framework, so to enable and modify **Volumetric Clouds** properties, you must add a **Volumetric Clouds** override to a [Volume](Volumes.md) in your Scene. To add a **Volumetric Clouds** override to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **Volumetric Clouds**.

**Note**: When editing Volumetric Cloud properties in the Editor, set **Temporal Accumulation Factor** to a lower value. This allows you to see changes instantly, rather than blended over time.

![](Images/volumetric-clouds-2.png)

[!include[](snippets/volume-override-api.md)]

## Cloud map and cloud lookup table

The **Cloud Map** and **Clout LUT** Textures define the shape and look of volumetric clouds. Both of these Textures are [channel-packed](Glossary.md#channel-packing) where each channel contains a separate grayscale Texture with a specific purpose. These two maps are not trivial to author, so it is best to only set **Cloud Control** to **Manual** if your project requires very specific clouds and you can use tool assistance to generate the maps.

For the **Cloud Map**, the color channels represent:

* **Red**: Coverage. Specifies the density of the clouds across the Texture.
    * Values closer to **0** represent an area of the clouds less affected by noise.
    * values closer to **1** represent an area of the clouds more affected by noise.

* **Green**: Rain. Specifies the areas of the clouds that are lighter/darker.
    * Values closer to **0** represent an area of the clouds with less rain that are lighter in color.
    * Values closer to **1** represent an area of the clouds with more rain that are darker in color.
* **Blue**: Type. Maps along the cloud lookup table's horizontal axis to specify cloud properties at the world position the texel Texture represents.

For the **Cloud LUT**, the color channels represent:

* **Red**: Profile Coverage. Determines the density of the cloud based on its height within the cloud volume.
* **Green**: Erosion and shaping. Determines which areas of the cloud volume are more susceptible to erosion and shaping. Values closer to 1 mean the cloud is more susceptible to erosion and shaping.
* **Blue**: Ambient Occlusion. A multiplier that HDRP applies to the ambient probe when it calculates lighting for the volumetric clouds.

When importing these two map Textures, disable **sRGB**. For best results, do not use any compression.

**Note**: This cloud map is formatted differently to the cloud map that the [Cloud Layer](Override-Cloud-Layer.md) feature uses.

![](Images/volumetric-clouds-3.png)

## Properties

### General

| **Property** | **Description**                                       |
| ------------ | ----------------------------------------------------- |
| **State**        | When set to **Enabled**, HDRP renders volumetric clouds. |
| **Local Clouds** | Indicates whether the clouds are part of the scene or rendered into the skybox. When enabled, clouds are part of the scene and you can interact with them. This means, you can move around the clouds, clouds can appear between the Camera and other GameObjects, and the Camera's clipping planes affects the clouds. When disabled, the clouds are part of the skybox. This means the clouds and their shadows appear relative to the Camera and always appear behind geometry. |

### Shape

| **Property**                      | **Description**                                              |
| --------------------------------- | ------------------------------------------------------------ |
| **Cloud Control**                 | Specifies the mode to control volumetric cloud properties. The options are:<br/>&#8226; **Simple**: Uses sliders and input fields to customize volumetric cloud shape properties. This mode generates a cloud map from the various properties and uses HDRP's internal cloud lookup table.<br/>&#8226; **Advanced**: Uses separate Textures to specify each cloud type and their coverage. This mode generates a cloud map from the various Textures and uses HDRP's internal cloud lookup table.<br/>&#8226; **Manual**: Uses the cloud map and lookup table you supply to render the clouds. For more information on the cloud map and cloud lookup table, see [Cloud map and cloud lookup table](#cloud-map-and-cloud-lookup-table). |
| - **Cloud Preset**                | Specifies the preset to apply to the **Simple** mode properties. The options are: <br/>&#8226; **Sparse**: Smaller clouds that are spread apart.<br/>&#8226; **Cloudy**: Medium-sized clouds that partially cover the sky.<br/>&#8226; **Overcast**: A light layer of cloud that covers the entire sky. Some areas are less dense and let more light through, whereas other areas are more dense and appear darker.<br/>&#8226; **Storm Clouds**: Large dark clouds that cover most of the sky.<br/>&#8226; **Custom**: Exposes properties that control the shape of the clouds. <br/><br/>This property only appears if you set **Cloud Control** to **Simple**. |
| - **Density Curve**        |  Controls the density (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume. <br/><br/>This property only appears if you set **Cloud Control** to **Simple**.|
| - **Erosion Curve**        |  Controls the erosion (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume. <br/><br/>This property only appears if you set **Cloud Control** to **Simple**.|
| - **Ambient Occlusion Curve**        |  Controls the ambient occlusion (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume. <br/><br/>This property only appears if you set **Cloud Control** to **Simple**.|
| - **Cumulus Map**                 | Specifies a Texture that defines the distribution of clouds in the lower layer. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced**. |
| - **Cumulus Map Multiplier**      | The multiplier for the clouds specified in the **Cumulus Map**. A value of **0** completely hides the cumulus clouds. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced**. |
| - **Alto Stratus Map**            | Specifies a Texture that defines the distribution of clouds in the higher layer. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced**. |
| - **Alto Stratus Map Multiplier** | The multiplier for the clouds specified in the **Alto Stratus Map**. A value of **0** completely hides the alto stratus clouds. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced**. |
| - **Cumulonimbus Map**            | Specifies a Texture that defines the distribution of anvil shaped cumulonimbus clouds. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced**. |
| - **Cumulonimbus Map Multiplier** | The multiplier for the clouds specified in the **Cumulonimbus Map**. A value of **0** completely hides the cumulonimbus clouds. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced**. |
| - **Rain Map**                    | Specifies a Texture that defines the distribution of rain in the clouds. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced**. |
| - **Cloud Map Resolution**        | Specifies the resolution for the internal Texture HDRP uses for the cloud map. A lower resolution produces better performance, but less precise cloud type transitions. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced**. |
| - **Cloud Map**                   | Specifies the cloud map to use for the volumetric clouds. For information on the format of this Texture, see [Cloud map and cloud lookup table](#cloud-map-and-cloud-lookup-table). <br/><br/>This property only appears if you set **Cloud Control** to **Custom**. |
| - **Cloud LUT**                   | Specifies the lookup table for the clouds. For information on the format of this Texture, see [Cloud map and cloud lookup table](#cloud-map-and-cloud-lookup-table).  <br/><br/>This property only appears if you set **Cloud Control** to **Custom**. |
| - **Cloud Map Tiling**            | The **X** and **Y** UV tile rate for one or more cloud map Textures. HDRP uses the **X** and **Y** values to tile the clouds across the sky.<br/>If **Cloud Control** is set to **Advanced**, this affects **Cumulus Map**, **Alto Stratus Map**, **Cumulonimbus Map**, and **Rain Map**.<br/>If **Cloud Control** is set to **Custom**, this affects the Texture assigned to the **Cloud Map** property.<br/><br/>This property only appears if you set **Cloud Control** to **Advanced** or **Custom**. |
| - **Cloud Map Offset**            | The **X** and **Y** UV offset for one or more cloud map Textures. HDRP uses the **X** and **Y** values to offset the clouds across the sky.<br/>If **Cloud Control** is set to **Advanced**, this affects **Cumulus Map**, **Alto Stratus Map**, **Cumulonimbus Map**, and **Rain Map**.<br/>If **Cloud Control** is set to **Custom**, this affects the Texture assigned to the **Cloud Map** property.<br/><br/>This property only appears if you set **Cloud Control** to **Advanced** or **Custom**, or if you set it to **Simple** and then set **Cloud Preset** to **Custom**. |
| - **Density Multiplier**          | The global density of the volumetric clouds. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced** or **Custom**, or if you set it to **Simple** and then set **Cloud Preset** to **Custom**. |
| - **Shape Factor**                | Controls the amount of shaping to apply to the cloud volume. A higher value produces less cloud coverage and smaller clouds. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced** or **Custom**, or if you set it to **Simple** and then set **Cloud Preset** to **Custom**. |
| - **Shape Scale**                 | Controls the size of the noise HDRP uses in the shaping stage to generate the general cloud shapes. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced** or **Custom**, or if you set it to **Simple** and then set **Cloud Preset** to **Custom**. |
| - **Shape Offset X**              | Controls the offset (world X-axis) applied when evaluating the larger noise passing through the cloud coverage. The values "0", "-1" and "1" will give the same result. |
| - **Shape Offset Y**              | Controls the offset (world Y-axis) applied when evaluating the larger noise passing through the cloud coverage. The values "0", "-1" and "1" will give the same result. |
| - **Shape Offset Z**              | Controls the offset (world Z-axis) applied when evaluating the larger noise passing through the cloud coverage. The values "0", "-1" and "1" will give the same result. |
| - **Erosion Factor**              | Controls the amount of erosion to apply on the edge of the clouds. A higher value erodes clouds more significantly. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced** or **Custom**, or if you set it to **Simple** and then set **Cloud Preset** to **Custom**. |
| - **Erosion Scale**               | Controls the size of the noise HDRP uses in the erosion stage to add local details to the cloud edges. <br/><br/>This property only appears if you set **Cloud Control** to **Advanced** or **Custom**, or if you set it to **Simple** and then set **Cloud Preset** to **Custom**. |
| **Earth Curvature**               | The curvature of the cloud volume. This defines the distance at which the clouds intersect with the horizon. |
| **Bottom Altitude**               | Controls the altitude of the bottom of the volumetric clouds volume in meters.|
| **Altitude Range**                | Controls the size of the volumetric clouds volume in meters. |
| **Fade In Mode**                  | Controls the mode in which the clouds fade in when close to the camera's near plane.|
| **Fade In Start**                 | Controls the minimal distance at which clouds start appearing.|
| **Fade In Distance**              | Controls the distance that it takes for the clouds to reach their complete density.|

### Wind

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Global Horizontal Wind Speed**  | Sets the global horizontal wind speed in kilometers per hour.<br />This value can be relative to the **Global Wind Speed** defined in the **Visual Environment**. |
| - **Orientation**                | Controls the orientation of the wind relative to the world-space direction x-axis.<br />This value can be relative to the **Global Wind Orientation** defined in the **Visual Environment**. |
| - **Cloud Map Speed Multiplier** | The multiplier to apply to the speed of the cloud map.       |
| - **Shape Speed Multiplier**     | The multiplier to apply to the speed of larger cloud shapes. |
| - **Erosion Speed Multiplier**   | The multiplier to apply to the speed of erosion cloud shapes. |
| **Vertical Shape Wind Speed**    | Controls the vertical wind speed of the larger cloud shapes. |
| **Vertical Erosion Wind Speed**  | Controls the vertical wind speed of the erosion cloud shapes. |

### Quality

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Temporal Accumulation Factor** | The amount of temporal accumulation to apply to the clouds. Temporal accumulation increases the visual quality of clouds by decreasing the noise. A higher value produces better quality clouds, but can create [ghosting](Glossary.md#ghosting). |
| **Ghosting Reduction**           | When you enable this property, HDRP removes the ghosting caused by temporal accumulation. This effect might cause a flickering effect when the **Temporal Accumulation Factor** value is low. |
| **Num Primary Steps**            | The number of steps to use to evaluate the clouds' transmittance. Higher values linearly increase the resource intensity of the effect. |
| **Num Light Steps**              | The number of steps to use to evaluate the clouds' lighting. Higher values exponent increase the resource intensity of the effect. |

### Lighting

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Ambient Light Probe Dimmer** | Controls the influence of light probes on the cloud volume. A lower value suppresses the ambient light and produces darker clouds overall. |
| **Sun Light Dimmer**           | Controls the influence of the sun light on the cloud volume. A lower value suppresses the sun light and produces darker clouds overall. |
| **Erosion Occlusion**          | Controls how much Erosion Factor is taken into account when computing ambient occlusion. The Erosion Factor parameter is editable in the custom preset, Advanced and Manual Modes. |
| **Scattering Tint**            | The color to tint the clouds.                                |
| **Powder Effect Intensity**    | Controls the amount of local scattering in the clouds. When clouds have a lot of local details due to erosion, a value of **1** provides a more powdery aspect. |
| **Multi Scattering**           | Controls the amount of multi-scattering inside the cloud. Higher values make lighting look more diffuse within the cloud. |

### Shadows

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Shadows**                      | Indicates whether the volumetric clouds cast shadows. To render the shadows, this property overrides the cookie in the shadow casting directional [Light](Light-Component.md). If [Cloud Layer](Override-Cloud-Layer.md) is active and also is set to cast shadows, volumetric cloud shadows take precedent and override the Cloud Layer shadow cookie. |
| - **Shadow Resolution**          | Specifies the resolution of the volumetric clouds shadow map. |
| - **Shadow Plane Height Offset** | The vertical offset to apply to compute the volumetric clouds shadow. If the Scene geometry is not centered at **0** on the Y-axis, set this offset equal to the y-axis center of your Scene geometry. |
| - **Shadow Distance**            | The size of the area to render volumetric cloud shadows around the camera. |
| - **Shadow Opacity**             | The opacity of the volumetric cloud shadows.                 |
| - **Shadow Opacity Fallback**    | Controls the shadow opacity when outside the area covered by the volumetric clouds shadow. |

## Limitations

* By default, volumetric clouds are disabled on [Planar Reflection Probes](Planar-Reflection-Probe.md) and realtime [Reflection Probes](Reflection-Probe.md) because of the performance cost.
* When enabled for [Reflection Probes](Reflection-Probe.md), the volumetric clouds are rendered at low resolution, without any form of temporal accumulation for performance and stability reasons.
* By default volumetric clouds are enabled on the baked [Reflection Probes](Reflection-Probe.md) if the asset allows it. They are rendered at full resolution without any form of temporal accumulation.
* Volumetric clouds do not appear in ray-traced effects.
* Transmittance is not applied linearly on the camera color to provide a better blending with the sun light (or high intensity pixels). If [Multi-sample anti-aliasing (MSAA)](#MSAA) is enabled on the camera, due to internal limitations, a different blending profile is used that may result in darker cloud edges.
