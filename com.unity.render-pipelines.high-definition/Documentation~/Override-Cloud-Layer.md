# Cloud Layer

The **Cloud Layer** Volume component override controls settings relevant to rendering a simple representation of the clouds. The cloud layer is a 2D texture rendered on top of the sky that can be animated using a flowmap.
This component also exposes an option to project cloud shadows on the ground.

## Using Cloud Layer

The **Cloud Layer** uses the [Volume](Volumes.md) framework, so to enable and modify **Cloud Layer** properties, you must add a **Cloud Layer** override to a [Volume](Volumes.md) in your Scene. To add **Cloud Layer** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Sky** and click on **Cloud Layer**.

After you add a **Cloud Layer** override, you must set the Volume to use **Cloud Layer**. The [Visual Environment](Override-Visual-Environment.md) override controls which type of clouds the Volume uses. In the **Visual Environment** override, navigate to the **Sky** section and set the **Cloud Type** to **Cloud Layer**. HDRP now renders a **Cloud Layer** for any Camera this Volume affects.
To enable the **Cloud Layer** override, you must assign a Cloud Map. You can refer to the [Cloud Map](#CloudMap) section to learn about the cloud map format or how to find the example cloud map texture.

The Cloud Layer will bake the cloud map to an intermediate texture, which is recomputed everytime a parameter changes. The resolution of the baked texture is determined by the **Resolution** parameter in the advanced settings of the inspector.
Clouds shadows are also baked to a separate texture whose resolution is set by the **Shadows Resolution** parameter.

<a name="CloudMap"></a>

## About the Cloud Map

The Cloud Map is a 2D RGBA texture in LatLong layout (sometimes called Cylindrical or Equirectangular) where each channel contains a cloud opacity. For rendering, the 4 channels are mixed together using the **Opacity RGBA** parameters of the volume override. This allows to change the aspects of the clouds using a single texture and the volume framework.

HDRP includes an example Cloud Map named `DefaultCloudMap`. This texture contains cumulus clouds in the red channel, stratus clouds in the green channel, cirrus clouds in the blue channel and wispy clouds in the alpha channel.

If **Upper Hemisphere Only** is checked, the map is interpreted as being the upper half of a LatLong texture. This means that clouds will only cover the sky above the horizon.

<a name="CustomizingFlowmap"></a>

## Customizing the Flowmap

You can assign a custom Flowmap to the **Cloud Layer** to have control over the cloud movement.
The Flowmap has the same layout as the Cloud Map, and is also subject to the **Upper Hemisphere Only** property.
Only the red and green channels are used and they represent respectively horizontal and vertical displacement. For each of these channels, a value of `0.5` means no displacement, a value of `0` means a negative displacement and a value of `1` means a positive displacement.

## Properties

![](Images/Override-CloudLayer.png)

| Property                      | Description                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| **Opacity**                   | This controls the global opacity of the cloud layer. |
| **Upper Hemisphere Only**     | Check the box to display the cloud layer above the horizon only. |
| **Layers**                    | Control the number of cloud layers (either one or two). Each layer has its own set of parameters described in the table below. |
| **Resolution**                | Controls the resolution of the texture HDRP uses to bake the clouds. |

| Layer Property                | Description                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| **Cloud Map**                 | Assign a Texture that HDRP uses to render the cloud layer. Refer to the section [Cloud Map](#CloudMap) for more details. |
| - **Opacity R**               | Opacity of the red layer. |
| - **Opacity G**               | Opacity of the green layer. |
| - **Opacity B**               | Opacity of the blue layer. |
| - **Opacity A**               | Opacity of the alpha layer. |
| **Rotation**                  | Use the slider to set the angle to rotate the Cloud Layer, in degrees. |
| **Tint**                      | Specifies a color that HDRP uses to tint the Cloud Layer. |
| **Exposure**                  | Set the amount of light per unit area that HDRP applies to the cloud layer. |
| **Distortion Mode**           | Use the dropdown to choose the distortion mode for simulating cloud motion.<br />&#8226; **None**: No distortion.<br />&#8226; **Procedural**: HDRP distorts the clouds using a uniform wind direction.<br />&#8226; **Flowmap**: HDRP distorts the clouds using the provided flowmap. |
| - **Scroll direction**        | Use the slider to set the scrolling direction for the distortion. |
| - **Scroll speed**            | Modify the speed at which HDRP scrolls the distortion texture. |
| - **Flowmap**                 | Assign a flowmap that HDRP uses to distort UVs when rendering the clouds. Refer to the section [Customizing the Flowmap](#CustomizingFlowmap) for more details.<br />This property only appears when you select **Flowmap** from the **Distortion** drop-down. |
| **Lighting**                  | Check the box to enable 2D raymarching on the Cloud Map to compute lighting for the main directional light. |
| - **Steps**                   | Use the slider to set the number of raymarching steps. |
| - **Thickness**               | Set the thickness of the clouds. |
| **Cast Shadows**              | Enable to have the clouds cast shadows for the main directional light.<br />The shadow texture will be set as a cookie on the light. Rotating the light around the Z-axis will rotate the shadow cookie, which may cause discrepancies with the scroll direction. |

| Shadows Property              | Description                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| **Shadow Multiplier**         | Controls the opacity of the cloud shadows. |
| **Shadow Tint**               | Controls the tint of the cloud shadows. |
| **Shadow Resolution**         | Controls the resolution of the cloud shadows texture. |
