# Cloud Layer

The **Cloud Layer** [Volume component override](Volume-Components.md) controls settings relevant to rendering a simple representation of clouds in the High Definition Render Pipeline (HDRP). The cloud layer is a 2D texture rendered on top of the sky that can be animated using a flowmap.

This component also exposes an option to project cloud shadows on the ground.

![](Images/HDRPFeatures-CloudLayer.png)

## Using the Cloud Layer

The **Cloud Layer** uses the [Volume](Volumes.md) framework, so to enable and modify **Cloud Layer** properties, you must add a **Cloud Layer** override to a [Volume](Volumes.md) in your Scene. To add **Cloud Layer** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **Cloud Layer**.

After you add a **Cloud Layer** override, you must set the Volume to use **Cloud Layer**. The [Visual Environment](Override-Visual-Environment.md) override controls which type of clouds the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Cloud Type** to **Cloud Layer**. HDRP now renders a **Cloud Layer** for any Camera this Volume affects.
To enable the **Cloud Layer** override, you must assign a cloud map. For information about the cloud map's format or how to find the example cloud map texture, see [about the cloud map](#about-the-cloud-map) section.

The Cloud Layer will bake the cloud map to an intermediate texture, which is recomputed everytime a parameter changes. The resolution of the baked texture is determined by the **Resolution** parameter in the advanced settings of the inspector.
Clouds shadows are also baked to a separate texture whose resolution is set by the **Shadow Resolution** parameter.

[!include[](snippets/volume-override-api.md)]

## About the cloud map

The cloud map is a 2D RGBA texture in LatLong layout (sometimes called Cylindrical or Equirectangular) where each channel contains a cloud opacity. For rendering, HDRP mixes the four channels together using the **Opacity RGBA** parameters of the Volume override. This allows you to change the aspects of the clouds using a single texture and the volume framework.
If you enable **Upper Hemisphere Only**, the map is interpreted as containing only the upper half of a LatLong texture. This means that clouds will only cover the sky above the horizon.

By default, HDRP uses a cloud map named `DefaultCloudMap`. This texture contains cumulus clouds in the red channel, stratus clouds in the green channel, cirrus clouds in the blue channel and wispy clouds in the alpha channel.

**Note**: This cloud map is formatted differently to the cloud map that the [Volumetric Clouds](Override-Volumetric-Clouds.md) feature uses.

## Controlling cloud movement

The Cloud Layer override provides a way to move clouds at runtime, using a flowmap. A flowmap has the same layout as the [cloud map](#about-the-cloud-map), in that it is a LatLong layout 2D texture, and also uses the **Upper Hemisphere Only** property to determine the area it affects.

A flowmap only uses the red and green channels and they represent horizontal and vertical displacement respectively. For each of these channels, a value of `0.5` means no displacement, a value of `0` means a negative displacement and a value of `1` means a positive displacement.

## Properties

![](Images/Override-CloudLayer.png)

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| Property                  | Description                                                  |
| ------------------------- | ------------------------------------------------------------ |
| **Opacity**               | The global opacity of the cloud layer. A value of 0 makes clouds completely transparent. |
| **Upper Hemisphere Only** | Indicates whether the Cloud Layer exclusively renders above the horizon or not. When enabled, HDRP still uses the entire **Cloud Map** texture, but the clouds will be renderer above the horizon. |
| **Layers**                | The number of cloud layers to render. Each layer has its own set of properties. The options are:<br/>&#8226; **1**: Renders a single cloud layer.<br/>&#8226; **2**: Renders two cloud layers. |
| **Resolution**            | The resolution of the texture HDRP uses to bake the clouds.  |

### Per-layer

The Inspector shows the following properties for each cloud layer. The **Layers** property determines the number of cloud layers to render.

| Property               | Description                                                  |
| ---------------------- | ------------------------------------------------------------ |
| **Cloud Map**          | Assign a Texture that HDRP uses to render the cloud layer. For more information about the cloud map, see [about the cloud map](#about-the-cloud-map). |
| - **Opacity R**        | The opacity multiplier to apply to the **Cloud Map**'s red channel. |
| - **Opacity G**        | The opacity multiplier to apply to the **Cloud Map**'s green channel. |
| - **Opacity B**        | The opacity multiplier to apply to the **Cloud Map**'s blue channel. |
| - **Opacity A**        | The opacity multiplier to apply to the **Cloud Map**'s alpha channel. |
| **Rotation**           | The angle to rotate the **Cloud Layer** texture by, in degrees. |
| **Tint**               | The color HDRP uses to tint the clouds.                      |
| **Exposure**           | The amount of light per unit area that HDRP applies to the cloud layer based on the main directional [Light](Light-Component.md) intensity. |
| **Distortion Mode**    | Specifies the distortion mode HDRP uses to simulate cloud movement.<br />&#8226; **None**: No distortion.<br />&#8226; **Procedural**: HDRP distorts the clouds using a uniform wind direction.<br />&#8226; **Flowmap**: HDRP distorts the clouds using the **Flowmap** texture. |
| - **Orientation**      | The orientation of the distortion relative to the X world vector (in degrees).<br />This value can be relative to the **Global Wind Orientation** defined in the **Visual Environment**. |
| - **Speed**            | The speed at which HDRP scrolls the distortion texture.<br />This value can be relative to the **Global Wind Speed** defined in the **Visual Environment**. |
| - **Flowmap**          | The flowmap HDRP uses to distort UVs when rendering the clouds. For more information about the flowmap, see [controlling cloud movement](#controlling-cloud-movement).<br />This property only appears when you select **Flowmap** from the **Distortion** drop-down. |
| **Lighting**           | Indicates whether HDRP calculates lighting for the clouds using the main directional light. When enabled, HDRP uses 2D raymarching on the Cloud Map to approximate self-shadowing from the sun light.<br /> The lighting computations are baked inside a texture and only recomputed when any of the relevant parameter changes. |
| - **Steps**            | The number of raymarching steps HDRP uses to calculate lighting for the clouds. The higher the value, the greater the travelled distance is. |
| - **Thickness**        | The thickness of the clouds. The larger the value, the darker the clouds appear. |
| **Cast Shadows**       | Indicates whether clouds cast shadows for the main directional light.<br/>This calculates the shadow texture and sets it as the light cookie for the main direction Light. |

| Shadows Property      | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Shadow Multiplier** | The opacity of the cloud shadows. The higher the value, the darker the shadows. |
| **Shadow Tint**       | The tint HDRP applies to the cloud shadows.                  |
| **Shadow Resolution** | The resolution of the cloud shadows texture.                 |
| **Shadow Size**       | The size of the projected cloud shadows.                     |
